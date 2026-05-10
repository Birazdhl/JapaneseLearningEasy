namespace JapaneseLearningApp.Models.Test;

/// <summary>Minimal payload pushed to flashcard UI.</summary>
public sealed class WordPromptDto
{
    public int Id { get; init; }

    /// <summary>Displayed on the prompt side (either English text or Japanese kana).</summary>
    public required string Prompt { get; init; }

    /// <summary>Reveal line 1 — romaji text (skipped for JP-only reveal mode).</summary>
    public string? RomajiReveal { get; init; }

    /// <summary>Reveal line 2 — English meaning or Japanese kana depending on test mode.</summary>
    public string? SecondaryReveal { get; init; }
}
