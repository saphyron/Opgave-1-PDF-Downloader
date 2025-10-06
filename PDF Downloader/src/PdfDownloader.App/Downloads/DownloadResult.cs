namespace PdfDownloader.App.Downloads;

internal sealed record DownloadResult(
    string Id,
    DownloadOutcome Outcome,
    string? Message,
    Uri? SourceUrl,
    FileInfo? SavedFile);