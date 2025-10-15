namespace PdfDownloader.App.Downloads;

internal enum DownloadOutcome
{
    Downloaded,
    SkippedExisting,
    Failed,
    NoUrl,
    TimedOut,
}