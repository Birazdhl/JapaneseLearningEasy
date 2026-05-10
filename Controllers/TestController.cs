using JapaneseLearningApp.Models;
using JapaneseLearningApp.Models.Test;
using JapaneseLearningApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace JapaneseLearningApp.Controllers;

/// <summary>Flashcard quizzes + AJAX companion endpoints.</summary>
[Route("Test")]
public class TestController(IWordTestSessionService testSessions) : Controller
{
    private readonly IWordTestSessionService _testSessions = testSessions;

    [HttpGet]
    [Route("English")]
    public IActionResult English() => View();

    [HttpGet]
    [Route("Japanese")]
    public IActionResult Japanese() => View();

    // --- AJAX: English ------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(RestartEnglishTest))]
    public async Task<JsonResult> RestartEnglishTest(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var dto = await _testSessions.RestartAsync(WordTestPromptKind.English);
        return Json(dto);
    }

    /// <inheritdoc cref="JapaneseLearningApp.Controllers.TestController.GetNextEnglishWord" />
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(GetNextEnglishWord))]
    public async Task<JsonResult> GetNextEnglishWord(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Json(await _testSessions.GetNextAsync(WordTestPromptKind.English));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(MarkAsRightEnglish))]
    public Task<JsonResult> MarkAsRightEnglish(CancellationToken cancellationToken)
        => InvokeMarkRight(WordTestPromptKind.English, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(MarkAsWrongEnglish))]
    public Task<JsonResult> MarkAsWrongEnglish(CancellationToken cancellationToken)
        => InvokeMarkWrong(WordTestPromptKind.English, cancellationToken);

    [HttpGet]
    [Route(nameof(GetTestProgressEnglish))]
    public JsonResult GetTestProgressEnglish(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Json(_testSessions.GetProgress(WordTestPromptKind.English));
    }

    // --- AJAX: Japanese -----------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(RestartJapaneseTest))]
    public async Task<JsonResult> RestartJapaneseTest(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var dto = await _testSessions.RestartAsync(WordTestPromptKind.Japanese);
        return Json(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(GetNextJapaneseWord))]
    public async Task<JsonResult> GetNextJapaneseWord(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Json(await _testSessions.GetNextAsync(WordTestPromptKind.Japanese));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(MarkAsRightJapanese))]
    public Task<JsonResult> MarkAsRightJapanese(CancellationToken cancellationToken)
        => InvokeMarkRight(WordTestPromptKind.Japanese, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(nameof(MarkAsWrongJapanese))]
    public Task<JsonResult> MarkAsWrongJapanese(CancellationToken cancellationToken)
        => InvokeMarkWrong(WordTestPromptKind.Japanese, cancellationToken);

    [HttpGet]
    [Route(nameof(GetTestProgressJapanese))]
    public JsonResult GetTestProgressJapanese(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Json(_testSessions.GetProgress(WordTestPromptKind.Japanese));
    }

    private async Task<JsonResult> InvokeMarkRight(WordTestPromptKind kind, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Json(await _testSessions.MarkRightAsync(kind));
    }

    private async Task<JsonResult> InvokeMarkWrong(WordTestPromptKind kind, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Json(await _testSessions.MarkWrongAsync(kind));
    }
}
