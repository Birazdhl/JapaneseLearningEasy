namespace JapaneseLearningApp.Configuration;

/// <summary>
/// Consolidated workbook + HTTP envelope limits referenced from controllers and host configuration.
/// </summary>
public static class ExcelUploadLimits
{
    public const long MaxWorkbookBytes = 5L * 1024 * 1024;

    /// <summary>
    /// Multipart encoding adds MIME boundaries/metadata on top of the raw file payload.
    /// Keep the ASP.NET/Kestrel ceiling slightly larger than <see cref="MaxWorkbookBytes"/>.
    /// </summary>
    public const long MultipartEnvelopeBytes = MaxWorkbookBytes + 1024L * 1024;
}
