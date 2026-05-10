using System.Diagnostics;
using JapaneseLearningApp.Data;
using JapaneseLearningApp.Models;
using JapaneseLearningApp.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JapaneseLearningApp.Controllers;

public class HomeController(
    ApplicationDbContext db,
    ILogger<HomeController> logger,
    IWebHostEnvironment environment) : Controller
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<HomeController> _logger = logger;
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<IActionResult> Index()
    {
        var total = await _db.JapaneseWords.AsNoTracking().CountAsync();
        var metaUtc = await _db.AppMetadata.AsNoTracking()
            .Where(m => m.Id == 1)
            .Select(m => m.LastDatabaseImportUtc)
            .FirstOrDefaultAsync();

        var vm = new HomeIndexViewModel
        {
            TotalWords = total,
            LastDatabaseImportUtc = metaUtc
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        string? diagnostics = null;

        if (_environment.IsDevelopment())
        {
            var fault = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (fault is not null)
            {
                diagnostics = $"{fault.GetType().FullName}: {fault.Message}";
            }
        }

        return View(new ErrorViewModel { RequestId = requestId, Diagnostics = diagnostics });
    }

    /// <summary>Built-in scaffold route removed from navigation — kept only for tooling compatibility.</summary>
    public IActionResult Privacy() => RedirectToAction(nameof(Index));
}
