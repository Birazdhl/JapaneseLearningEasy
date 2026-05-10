using System.Diagnostics.CodeAnalysis;
using JapaneseLearningApp.Data;
using JapaneseLearningApp.Infrastructure;
using JapaneseLearningApp.Models;
using JapaneseLearningApp.Models.Test;
using Microsoft.EntityFrameworkCore;

namespace JapaneseLearningApp.Services;

/// <summary>
/// Session-backed flashcard queues: main shuffle pass, wrong-item review tail, AJAX-friendly helpers.
/// </summary>
public interface IWordTestSessionService
{
    Task<ApiResultDto> RestartAsync(WordTestPromptKind kind);
    Task<ApiResultDto> GetNextAsync(WordTestPromptKind kind);
    Task<ApiResultDto> MarkRightAsync(WordTestPromptKind kind);
    Task<ApiResultDto> MarkWrongAsync(WordTestPromptKind kind);
    TestProgressDto GetProgress(WordTestPromptKind kind);
}

public sealed class WordTestSessionService : IWordTestSessionService
{
    private const string SessionKeyPrefix = "jl.wordTest.";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _db;

    public WordTestSessionService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    private ISession Session =>
        _httpContextAccessor.HttpContext?.Session ?? throw new InvalidOperationException("Session unavailable.");

    private static string SessionKey(WordTestPromptKind kind) => $"{SessionKeyPrefix}{kind}";

    public async Task<ApiResultDto> RestartAsync(WordTestPromptKind kind)
    {
        var ids = await _db.JapaneseWords.AsNoTracking().Select(w => w.Id).ToListAsync();
        if (ids.Count == 0)
        {
            return new ApiResultDto
            {
                Success = false,
                Message = "No vocabulary found. Upload a spreadsheet via Update first.",
                Progress = new TestProgressDto { Completed = 0, Total = 0 }
            };
        }

        var envelope = new TestSessionEnvelope
        {
            MainQueue = Shuffle(ids),
            WrongQueue = new List<int>(),
            CurrentId = null,
            TotalWords = ids.Count
        };

        AdvanceToNextCard(envelope);
        Session.SetJson(SessionKey(kind), envelope);

        WordPromptDto? next = null;
        if (envelope.CurrentId.HasValue)
        {
            next = await BuildPromptAsync(kind, envelope.CurrentId.Value);
        }

        return new ApiResultDto
        {
            Success = true,
            Next = next,
            Progress = BuildProgress(envelope)
        };
    }

    public async Task<ApiResultDto> GetNextAsync(WordTestPromptKind kind)
    {
        if (!TryReadEnvelope(kind, out var envelope))
            return NotInitialized();

        if (IsComplete(envelope))
            return Complete(envelope);

        if (!envelope.CurrentId.HasValue)
            AdvanceToNextCard(envelope);

        if (!envelope.CurrentId.HasValue)
        {
            Save(kind, envelope);
            return Complete(envelope);
        }

        Save(kind, envelope);
        return new ApiResultDto
        {
            Success = true,
            Next = await BuildPromptAsync(kind, envelope.CurrentId.Value),
            Progress = BuildProgress(envelope)
        };
    }

    public async Task<ApiResultDto> MarkRightAsync(WordTestPromptKind kind)
    {
        if (!TryReadEnvelope(kind, out var envelope))
            return NotInitialized();

        if (!envelope.CurrentId.HasValue)
            return Fail("Nothing to score yet — restart the quiz.");

        envelope.CurrentId = null;

        AdvanceToNextCard(envelope);
        Save(kind, envelope);

        if (IsComplete(envelope))
            return Complete(envelope);

        if (!envelope.CurrentId.HasValue)
            return Fail("Quiz state corrupted. Please restart.");

        return new ApiResultDto
        {
            Success = true,
            Next = await BuildPromptAsync(kind, envelope.CurrentId.Value),
            Progress = BuildProgress(envelope)
        };
    }

    public async Task<ApiResultDto> MarkWrongAsync(WordTestPromptKind kind)
    {
        if (!TryReadEnvelope(kind, out var envelope))
            return NotInitialized();

        if (!envelope.CurrentId.HasValue)
            return Fail("Nothing to score yet — restart the quiz.");

        var wrongId = envelope.CurrentId.Value;
        if (!envelope.WrongQueue.Contains(wrongId))
            envelope.WrongQueue.Add(wrongId);

        envelope.CurrentId = null;
        AdvanceToNextCard(envelope);
        Save(kind, envelope);

        if (IsComplete(envelope))
            return Complete(envelope);

        if (!envelope.CurrentId.HasValue)
            return Fail("Quiz state corrupted. Please restart.");

        return new ApiResultDto
        {
            Success = true,
            Next = await BuildPromptAsync(kind, envelope.CurrentId.Value),
            Progress = BuildProgress(envelope)
        };
    }

    public TestProgressDto GetProgress(WordTestPromptKind kind)
    {
        if (!TryReadEnvelope(kind, out var envelope))
            return new TestProgressDto { Completed = 0, Total = 0 };

        return BuildProgress(envelope);
    }

    /// <summary>Pop the next queued item respecting main → wrong ordering.</summary>
    private static void AdvanceToNextCard(TestSessionEnvelope envelope)
    {
        if (IsComplete(envelope))
            return;

        if (envelope.MainQueue.Count > 0)
        {
            var next = envelope.MainQueue[0];
            envelope.MainQueue.RemoveAt(0);
            envelope.CurrentId = next;
            return;
        }

        if (envelope.WrongQueue.Count > 0)
        {
            var next = envelope.WrongQueue[0];
            envelope.WrongQueue.RemoveAt(0);
            envelope.CurrentId = next;
        }
    }

    /// <summary>Finished when queues are empty and nothing is staged.</summary>
    private static bool IsComplete(TestSessionEnvelope envelope)
        => envelope is { CurrentId: null, MainQueue.Count: 0, WrongQueue.Count: 0 };

    private async Task<WordPromptDto?> BuildPromptAsync(WordTestPromptKind kind, int wordId)
    {
        var word = await _db.JapaneseWords.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wordId);
        if (word is null)
            return null;

        return kind switch
        {
            WordTestPromptKind.English => new WordPromptDto
            {
                Id = word.Id,
                Prompt = word.English,
                RomajiReveal = word.Romaji,
                SecondaryReveal = word.Japanese
            },
            WordTestPromptKind.Japanese => new WordPromptDto
            {
                Id = word.Id,
                Prompt = word.Japanese,
                RomajiReveal = null,
                SecondaryReveal = word.English
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static TestProgressDto BuildProgress(TestSessionEnvelope envelope)
    {
        var staged = envelope.CurrentId.HasValue ? 1 : 0;
        var pending = envelope.MainQueue.Count + envelope.WrongQueue.Count + staged;
        var completed = Math.Clamp(envelope.TotalWords - pending, 0, envelope.TotalWords);

        return new TestProgressDto
        {
            Completed = envelope.TotalWords == 0 ? 0 : completed,
            Total = envelope.TotalWords
        };
    }

    private bool TryReadEnvelope(WordTestPromptKind kind, [NotNullWhen(true)] out TestSessionEnvelope? envelope)
    {
        if (Session.TryGetJson<TestSessionEnvelope>(SessionKey(kind), out var stored) &&
            stored is not null)
        {
            envelope = stored;
            return true;
        }

        envelope = null;
        return false;
    }

    private void Save(WordTestPromptKind kind, TestSessionEnvelope envelope)
        => Session.SetJson(SessionKey(kind), envelope);

    private static List<int> Shuffle(IReadOnlyCollection<int> source)
    {
        var bag = source.ToList();
        for (var i = bag.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }
        return bag;
    }

    private static ApiResultDto NotInitialized()
        => new()
        {
            Success = false,
            Message = "Quiz not initialized. Reload the page to restart.",
            Progress = new TestProgressDto { Completed = 0, Total = 0 }
        };

    private static ApiResultDto Fail(string message)
        => new() { Success = false, Message = message };

    private static ApiResultDto Complete(TestSessionEnvelope envelope)
        => new()
        {
            Success = true,
            Progress = BuildProgress(envelope),
            Next = null,
            Message = "Great job! You mastered every word in this deck."
        };
}
