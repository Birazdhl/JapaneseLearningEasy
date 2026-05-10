namespace JapaneseLearningApp.Models;

/// <summary>
/// Singleton row for app-wide timestamps (tracks last successful vocabulary import).
/// </summary>
public class AppMetadata
{
    /// <summary>Always <c>1</c> — single singleton row.</summary>
    public int Id { get; set; } = 1;
    public DateTimeOffset? LastDatabaseImportUtc { get; set; }
}
