namespace JapaneseLearningApp.Models.Test;

public sealed class TestProgressDto
{
    /// <summary>Words answered correctly.</summary>
    public int Completed { get; init; }

    /// <summary>Deck size captured when the quiz started.</summary>
    public int Total { get; init; }

    public bool Finished => Total > 0 && Completed >= Total;
}
