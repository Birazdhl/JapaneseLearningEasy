using JapaneseLearningApp.Data;
using JapaneseLearningApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JapaneseLearningApp.Controllers;

public class WordsController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    /// <summary>Search-friendly listing for manual verification runs.</summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _db.JapaneseWords.AsNoTracking()
            .OrderBy(w => w.English)
            .ToListAsync(cancellationToken));
}
