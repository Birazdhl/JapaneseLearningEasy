namespace JapaneseLearningApp.Models.Test;

/// <summary>Standard envelope for AJAX calls from quiz pages.</summary>
public sealed class ApiResultDto
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    /// <summary>Next card payload when available.</summary>
    public WordPromptDto? Next { get; init; }

    public TestProgressDto? Progress { get; init; }
}
