namespace JapaneseLearningApp.Models;

/// <summary>
/// Represents a vocabulary row synced from Excel and used in quizzes.
/// </summary>
public class JapaneseWord
{
    public int Id { get; set; }
    public string English { get; set; } = string.Empty;
    public string Romaji { get; set; } = string.Empty;
    /// <summary>Hiragana/Katakana as imported from spreadsheet.</summary>
    public string Japanese { get; set; } = string.Empty;
}
