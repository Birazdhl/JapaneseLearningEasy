namespace JapaneseLearningApp.Services;

/// <summary>Serialized word-test queue state stored in Session.</summary>
public sealed class TestSessionEnvelope
{
    public List<int> MainQueue { get; set; } = new();
    public List<int> WrongQueue { get; set; } = new();
    /// <summary>Word currently displayed to the user (null means call advance).</summary>
    public int? CurrentId { get; set; }

    /// <summary>Counts original batch size when the round started.</summary>
    public int TotalWords { get; set; }
}
