using System.Data.Common;
using JapaneseLearningApp.Configuration;
using JapaneseLearningApp.Data;
using JapaneseLearningApp.Models;
using JapaneseLearningApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace JapaneseLearningApp.Controllers;

/// <summary>Excel ingestion entry point (/Update).</summary>
[Route("Update")]
public class UpdateController(ApplicationDbContext db, ILogger<UpdateController> logger) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<UpdateController> _logger = logger;

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        var empty = !await _db.JapaneseWords.AsNoTracking().AnyAsync();
        return View(new UpdateDatabaseViewModel { IsDatabaseEmpty = empty });
    }

    [HttpPost]
    [Route("")]
    [Route("Index")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = ExcelUploadLimits.MultipartEnvelopeBytes)]
    [RequestSizeLimit(ExcelUploadLimits.MultipartEnvelopeBytes)]
    public async Task<IActionResult> Index(UpdateDatabaseViewModel model, CancellationToken cancellationToken)
    {
        model.IsDatabaseEmpty = !await _db.JapaneseWords.AsNoTracking().AnyAsync(cancellationToken);

        if (model.Spreadsheet is null || model.Spreadsheet.Length == 0)
        {
            ModelState.AddModelError(nameof(model.Spreadsheet),
                $"Please choose an .xlsx file (max {ExcelUploadLimits.MaxWorkbookBytes / (1024 * 1024)} MB).");
        }
        else if (model.Spreadsheet.Length > ExcelUploadLimits.MaxWorkbookBytes)
        {
            ModelState.AddModelError(nameof(model.Spreadsheet),
                $"File size cannot exceed {ExcelUploadLimits.MaxWorkbookBytes / (1024 * 1024)} MB.");
        }
        else if (!Path.GetExtension(model.Spreadsheet.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.Spreadsheet), "Only Excel .xlsx workbooks are allowed.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var uploadedSpreadsheet = model.Spreadsheet!;

        await using var ms = new MemoryStream();
        await uploadedSpreadsheet.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        List<JapaneseWord> extracted;
        try
        {
            extracted = ExtractWords(ms);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rejected spreadsheet upload.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        try
        {
            await ImportIntoDatabaseAsync(extracted, cancellationToken);
        }
        catch (DbUpdateException dbEx)
        {
            MapDatabaseFailure(dbEx.InnerException ?? dbEx);
            return View(model);
        }
        catch (DbException dbEx)
        {
            MapDatabaseFailure(dbEx);
            return View(model);
        }
        catch (InvalidOperationException setupEx)
        {
            ModelState.AddModelError(string.Empty, setupEx.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled failure while importing Excel workbook.");
            ModelState.AddModelError(string.Empty,
                "Something unexpected crashed while importing. Check Visual Studio Output / ASP.NET logs, " +
                "set ASPNETCORE_ENVIRONMENT to Development temporarily, then retry.");
            return View(model);
        }

        var toast =
            $"Database updated successfully! {extracted.Count} words imported.";
        TempData["ToastSuccess"] = toast;
        _logger.LogInformation("Imported {Count} vocabulary rows.", extracted.Count);

        return RedirectToAction(nameof(Index));
    }

    private async Task ImportIntoDatabaseAsync(IReadOnlyCollection<JapaneseWord> extracted,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE [JapaneseTable];",
            cancellationToken);

        await _db.JapaneseWords.AddRangeAsync(extracted, cancellationToken);
        await SaveImportTimestampAsync(cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private void MapDatabaseFailure(Exception ex)
    {
        _logger.LogError(ex, "Database replace failed during Excel import.");

        var message =
            $"Database replace failed ({ex.GetType().Name}). See logs for technical details.";
        switch (ex)
        {
            case Microsoft.Data.SqlClient.SqlException sx:
                switch (sx.Number)
                {
                    case 208: // Invalid object name
                        message =
                            $"SQL Server could not find a required table ({sx.Message.Trim()}). " +
                            "Apply EF migrations first: Package Manager Console `Update-Database` — or CLI `dotnet ef database update`.";
                        break;
                    case 4060:
                    case 18456:
                        message =
                            "Cannot authenticate or open the database. Verify SQL Server is running and " +
                            "that your connection string targets the intended catalog (for example JapanDB).";
                        break;
                    case 547:
                        message =
                            "Import blocked by SQL Server constraint (foreign key). Remove dependent rows or recreate " +
                            "the vocabulary tables cleanly.";
                        break;
                    default:
                        message =
                            $"SQL Server returned error #{sx.Number}, state {sx.State}: {sx.Message.Trim()}";
                        break;
                }

                break;
            default:
                if (Contains(ex.Message, "JapaneseTable"))
                {
                    message =
                        "The Japanese vocabulary table is missing from the connected database. " +
                        "Run EF migrations (`Update-Database` or `dotnet ef database update`), then try again.";
                }
                else if (Contains(ex.Message, "AppMetadata"))
                {
                    message =
                        "The AppMetadata lookup row is missing. Run EF migrations, then retry the import.";
                }

                break;
        }

        ModelState.AddModelError(string.Empty, message);
    }

    private static bool Contains(string? haystack, string needle) =>
        !string.IsNullOrEmpty(haystack) &&
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private async Task SaveImportTimestampAsync(CancellationToken cancellationToken)
    {
        var meta = await _db.AppMetadata
            .SingleOrDefaultAsync(m => m.Id == 1, cancellationToken);

        if (meta is null)
        {
            throw new InvalidOperationException(
                "Missing AppMetadata row Id=1. Run `dotnet ef database update` against this solution " +
                "so the seeded metadata row exists, then upload again.");
        }

        meta.LastDatabaseImportUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reads the workbook. Row 1 is treated as headers: English | Romaji | Japanese.
    /// </summary>
    private static List<JapaneseWord> ExtractWords(Stream workbookStream)
    {
        ExcelPackage pkg;
        try
        {
            pkg = new ExcelPackage(workbookStream);
        }
        catch
        {
            throw new InvalidOperationException(
                "Unreadable workbook — verify the upload is an .xlsx file saved by Excel.");
        }

        using (pkg)
        {
            var sheet = pkg.Workbook.Worksheets.FirstOrDefault()
                        ?? throw new InvalidOperationException(
                            "The workbook needs at least one worksheet.");

            if (sheet.Dimension is null)
                throw new InvalidOperationException("The worksheet appears empty.");

            var headers = Enumerable.Range(1, sheet.Dimension.Columns)
                .Select(col => sheet.Cells[1, col].Text?.Trim())
                .ToArray();

            if (headers.Length < 3)
                throw new InvalidOperationException("The Excel file must expose three columns.");

            MapHeaders(headers, out var englishIdx, out var romajiIdx, out var japaneseIdx);

            var words = new List<JapaneseWord>();
            for (var row = 2; row <= sheet.Dimension.Rows; row++)
            {
                var english = sheet.Cells[row, englishIdx].Text?.Trim();
                var romaji = sheet.Cells[row, romajiIdx].Text?.Trim();
                var japanese = sheet.Cells[row, japaneseIdx].Text?.Trim();

                if (string.IsNullOrWhiteSpace(english) &&
                    string.IsNullOrWhiteSpace(romaji) &&
                    string.IsNullOrWhiteSpace(japanese))
                {
                    continue;
                }

                words.Add(new JapaneseWord
                {
                    English = english ?? string.Empty,
                    Romaji = romaji ?? string.Empty,
                    Japanese = japanese ?? string.Empty
                });
            }

            if (words.Count == 0)
                throw new InvalidOperationException(
                    "No data rows detected below the headers.");

            return words;
        }
    }

    private static void MapHeaders(IReadOnlyList<string?> headers, out int englishCol,
        out int romajiCol, out int japaneseCol)
    {
        englishCol = romajiCol = japaneseCol = -1;
        for (var i = 0; i < headers.Count; i++)
        {
            var text = headers[i]?.Trim();
            if (text is null) continue;

            if (text.Equals("English", StringComparison.OrdinalIgnoreCase))
                englishCol = i + 1;
            else if (text.Equals("Romaji", StringComparison.OrdinalIgnoreCase))
                romajiCol = i + 1;
            else if (text.Equals("Japanese", StringComparison.OrdinalIgnoreCase))
                japaneseCol = i + 1;
        }

        if (englishCol < 1 || romajiCol < 1 || japaneseCol < 1)
            throw new InvalidOperationException(
                "Headers must be named exactly English, Romaji, and Japanese (column order flexible).");

        if (new[] { englishCol, romajiCol, japaneseCol }.Distinct().Count() != 3)
            throw new InvalidOperationException(
                "Each required header (English, Romaji, Japanese) must occupy its own distinct column.");
    }
}
