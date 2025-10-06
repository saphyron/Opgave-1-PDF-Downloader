namespace PdfDownloader.App.Middleware;

internal sealed record MetadataRecord(string Id, string? PrimaryUrl, string? FallbackUrl)
{
    public bool HasAnyUrl => !string.IsNullOrWhiteSpace(PrimaryUrl) || !string.IsNullOrWhiteSpace(FallbackUrl);

    public IReadOnlyList<string> GetOrderedUrls()
    {
        var urls = new List<string>(capacity: 2);
        if (!string.IsNullOrWhiteSpace(PrimaryUrl))
        {
            urls.Add(PrimaryUrl!);
        }

        if (!string.IsNullOrWhiteSpace(FallbackUrl))
        {
            urls.Add(FallbackUrl!);
        }

        return urls;
    }
}