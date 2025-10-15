namespace PdfDownloader.App;

internal sealed record AppOptions
(
    FileInfo Input,                 // skal eksistere
    DirectoryInfo Output,           // skal eksistere
    FileInfo? StatusReport,         // kan være null
    string IdColumn,                // ID-kolonne
    string UrlColumn,               // primær URL-kolonne
    string? FallbackUrlColumn,      // kan være null
    int Limit,                      // 0 = ingen grænse
    int MaxConcurrency,             // maks antal samtidige downloads
    bool SkipExisting,              // hvis true: spring over hvis fil findes
    FileInfo? ResumeFromStatus,     // hvis angivet: genoptag fra status-fil
    bool AppendStatus,              // hvis true: tilføj til eksisterende status-fil (hvis findes)
    bool OverwriteStatus,           // hvis true: skriv ny status-fil (overskriv eksisterende)
    int? First,                     // tager de første N rækker
    int? Skip,                      // skip N, tag resten (evt. afgrænset af Take)
    int? Take,                      // tag N (efter Skip)
    int? FromIndex,                 // 1-baseret startposition
    int? ToIndex,                   // 1-baseret slutposition (inklusiv)
    bool OverwriteDownloads,        // hvis true: hent igen selvom fil findes
    bool DetectChanges,             // hvis true: sammenlign med eksisterende (SHA-256)
    bool KeepOldOnChange,           // hvis true: omdøb gammel til *.updated ved forskel
    // timeouts
    TimeSpan DownloadTimeout,       // total pr. fil (0 = slået fra)
    TimeSpan IdleTimeout,           // ingen bytes i så lang tid => afbryd (0 = slået fra)
    bool NoTimeout,                 // overstyr alt og kør “så længe som muligt”
    TimeSpan ConnectTimeout         // TCP/TLS opkobling
)
{
    public static string Usage => """
Brug: dotnet run -- --input <sti> --output <dir> [--status <sti>] 
                   --id-column <navn> --url-column <navn> [--fallback-url-column <navn>]
                   [--limit <tal>] [--max-concurrency <tal>] [--no-skip-existing]
                   [--resume-from-status <sti>] [--append-status] [--overwrite-status]
                   [--first <N>] [--skip <N>] [--take <N>] [--from <i>] [--to <j>]
                   [--overwrite-downloads] [--detect-changes] [--keep-old-on-change]
                   [--download-timeout hh:mm:ss] [--idle-timeout hh:mm:ss]
                   [--no-timeout] [--connect-timeout hh:mm:ss]
""";

    public static AppOptions Parse(string[] args)
    {
        // helpers
        string? Get(string name)
        {
            var i = Array.FindIndex(args, a => string.Equals(a, $"--{name}", StringComparison.OrdinalIgnoreCase));
            return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
        }
        bool Has(string name) => args.Any(a => string.Equals(a, $"--{name}", StringComparison.OrdinalIgnoreCase));
        static TimeSpan GetTsOr(string? s, TimeSpan fallback)
            => TimeSpan.TryParse(s, out var ts) ? ts : fallback;

        var inputStr   = Get("input") ?? throw new OptionParsingException("Mangler --input");
        var outputStr  = Get("output") ?? throw new OptionParsingException("Mangler --output");

        var idColumn   = Get("id-column")   ?? "BRnum";
        var urlColumn  = Get("url-column")  ?? "Pdf_URL";
        var fallback   = Get("fallback-url-column");

        var statusStr  = Get("status");
        var limit      = int.TryParse(Get("limit"), out var lim) ? lim : 0;
        var maxConc    = int.TryParse(Get("max-concurrency"), out var mc) ? mc : 10;
        var skipExisting = !Has("no-skip-existing");

        var resumeStr  = Get("resume-from-status");
        var append     = Has("append-status");
        var overwriteS = Has("overwrite-status");
        if (append && overwriteS) throw new OptionParsingException("Vælg enten --append-status eller --overwrite-status (ikke begge).");
        if (!append && !overwriteS) append = true;

        int? first     = int.TryParse(Get("first"), out var f) ? f : null;
        int? skip      = int.TryParse(Get("skip"),  out var s) ? s : null;
        int? take      = int.TryParse(Get("take"),  out var t) ? t : null;
        int? fromIdx   = int.TryParse(Get("from"),  out var fi) ? fi : null;
        int? toIdx     = int.TryParse(Get("to"),    out var ti) ? ti : null;

        var overwriteDownloads = Has("overwrite-downloads");
        var detectChanges      = Has("detect-changes");
        var keepOldOnChange    = Has("keep-old-on-change") || overwriteDownloads;

        // NEW — parse timeouts (standard: 02:00 total, 00:15 idle, 00:10 connect)
        var dlTimeout      = GetTsOr(Get("download-timeout"), TimeSpan.FromMinutes(2));
        var idleTimeout    = GetTsOr(Get("idle-timeout"),     TimeSpan.FromSeconds(15));
        var noTimeout      = Has("no-timeout");
        var connectTimeout = GetTsOr(Get("connect-timeout"),  TimeSpan.FromSeconds(10));

        var input   = new FileInfo(inputStr);
        var output  = new DirectoryInfo(outputStr);
        var status  = string.IsNullOrWhiteSpace(statusStr) ? null : new FileInfo(statusStr);
        var resume  = string.IsNullOrWhiteSpace(resumeStr) ? null : new FileInfo(resumeStr);

        return new AppOptions(
            input,
            output,
            status,
            idColumn,
            urlColumn,
            string.IsNullOrWhiteSpace(fallback) ? null : fallback,
            limit,
            maxConc,
            skipExisting,
            resume,
            append,
            overwriteS,
            first,
            skip,
            take,
            fromIdx,
            toIdx,
            overwriteDownloads,
            detectChanges,
            keepOldOnChange,
            dlTimeout,
            idleTimeout,
            noTimeout,
            connectTimeout
        );
    }
}

internal sealed class OptionParsingException(string message) : Exception(message);