namespace JapaneseLearningApp.ViewModels;

public sealed class HomeIndexViewModel
{
    public int TotalWords { get; init; }
    public DateTimeOffset? LastDatabaseImportUtc { get; init; }
}
