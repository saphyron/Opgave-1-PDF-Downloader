namespace PdfDownloader.App.Downloads;

internal sealed record DownloadRequest(string Id, IReadOnlyList<string> Urls);