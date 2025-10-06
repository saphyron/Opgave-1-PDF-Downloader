namespace PdfDownloader.App;

internal sealed record AppOptions
(
    FileInfo Input,
    DirectoryInfo Output,
    FileInfo? StatusReport,
    string IdColumn,
    string UrlColumn,
    string? FallbackUrlColumn,
    int Limit,
    int MaxConcurrency,
    bool SkipExisting
)
{
    public static string Usage => "Brug: dotnet run -- --input <sti> [--output <mappe>] [--status <fil>] [--id-column <navn>] [--url-column <navn>] [--fallback-url-column <navn>] [--limit <tal>] [--max-concurrency <tal>] [--no-skip-existing]";

    public static AppOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new OptionParsingException("Der mangler argumenter.");
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new OptionParsingException($"Ukendt argument: {arg}");
            }

            var name = arg[2..];
            if (name.Equals("no-skip-existing", StringComparison.OrdinalIgnoreCase))
            {
                flags.Add(name);
                continue;
            }

            if (i + 1 >= args.Length)
            {
                throw new OptionParsingException($"Forventede en værdi efter {arg}");
            }

            var value = args[++i];
            values[name] = value;
        }

        if (!values.TryGetValue("input", out var inputPath))
        {
            throw new OptionParsingException("Optionen --input er påkrævet.");
        }

        var input = new FileInfo(inputPath);
        if (!input.Exists)
        {
            throw new OptionParsingException($"Filen '{input.FullName}' blev ikke fundet.");
        }

        var output = values.TryGetValue("output", out var outputPath)
            ? new DirectoryInfo(outputPath)
            : new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Downloads"));

        var status = values.TryGetValue("status", out var statusPath)
            ? new FileInfo(statusPath)
            : null;

        var idColumn = values.TryGetValue("id-column", out var id) ? id : "BRnum";
        var urlColumn = values.TryGetValue("url-column", out var url) ? url : "Pdf_URL";
        var fallback = values.TryGetValue("fallback-url-column", out var alt) ? alt : "Pdf_URL_Alt";

        var limit = 10;
        if (values.TryGetValue("limit", out var limitString) && int.TryParse(limitString, out var parsedLimit))
        {
            limit = parsedLimit <= 0 ? int.MaxValue : parsedLimit;
        }

        var maxConcurrency = values.TryGetValue("max-concurrency", out var concString) && int.TryParse(concString, out var parsedConcurrency)
            ? Math.Clamp(parsedConcurrency, 1, 32)
            : 4;

        var skipExisting = !flags.Contains("no-skip-existing");

        return new AppOptions(
            input,
            output,
            status,
            idColumn,
            urlColumn,
            string.IsNullOrWhiteSpace(fallback) ? null : fallback,
            limit,
            maxConcurrency,
            skipExisting);
    }
}

internal sealed class OptionParsingException(string message) : Exception(message);
