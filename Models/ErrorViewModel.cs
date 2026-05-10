namespace JapaneseLearningApp.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>Populated locally when richer diagnostics can be shown safely.</summary>
    public string? Diagnostics { get; set; }

    public bool ShowDiagnostics => !string.IsNullOrWhiteSpace(Diagnostics);
}
