using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

namespace PdfDownloader.App.Middleware;

internal sealed class MetadataLoader
{
    public Task<IReadOnlyList<MetadataRecord>> LoadAsync(
        FileInfo file,
        string idColumn,
        string urlColumn,
        string? fallbackUrlColumn,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(file.Extension.ToLowerInvariant() switch
        {
            ".xlsx" or ".xls" => LoadFromExcel(file, idColumn, urlColumn, fallbackUrlColumn, cancellationToken),
            ".csv" => LoadFromCsv(file, idColumn, urlColumn, fallbackUrlColumn, cancellationToken),
            _ => throw new OptionParsingException($"Filtypen '{file.Extension}' understøttes ikke. Benyt .xlsx eller .csv."),
        });
    }

    private static IReadOnlyList<MetadataRecord> LoadFromExcel(
        FileInfo file,
        string idColumn,
        string urlColumn,
        string? fallbackUrlColumn,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(file.FullName);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.FirstRowUsed();
        if (headerRow is null)
        {
            return Array.Empty<MetadataRecord>();
        }

        var headers = headerRow.CellsUsed().ToDictionary(
            cell => cell.GetString(),
            cell => cell.Address.ColumnNumber,
            StringComparer.OrdinalIgnoreCase);

        if (!headers.TryGetValue(idColumn, out var idColumnIndex))
        {
            throw new OptionParsingException($"Kolonnen '{idColumn}' blev ikke fundet i regnearket.");
        }

        if (!headers.TryGetValue(urlColumn, out var urlColumnIndex))
        {
            throw new OptionParsingException($"Kolonnen '{urlColumn}' blev ikke fundet i regnearket.");
        }

        var fallbackColumnIndex = 0;
        if (!string.IsNullOrWhiteSpace(fallbackUrlColumn) && headers.TryGetValue(fallbackUrlColumn, out var index))
        {
            fallbackColumnIndex = index;
        }

        var result = new List<MetadataRecord>();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = row.Cell(idColumnIndex).GetValue<string>().Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var url = row.Cell(urlColumnIndex).GetValue<string>().Trim();
            string? fallback = null;
            if (fallbackColumnIndex > 0)
            {
                fallback = row.Cell(fallbackColumnIndex).GetValue<string>().Trim();
            }

            result.Add(new MetadataRecord(id, string.IsNullOrWhiteSpace(url) ? null : url, string.IsNullOrWhiteSpace(fallback) ? null : fallback));
        }

        return result;
    }

    private static IReadOnlyList<MetadataRecord> LoadFromCsv(
        FileInfo file,
        string idColumn,
        string urlColumn,
        string? fallbackUrlColumn,
        CancellationToken cancellationToken)
    {
        using var stream = file.OpenRead();
        using var reader = new StreamReader(stream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
        };

        using var csv = new CsvReader(reader, config);
        if (!csv.Read())
        {
            return Array.Empty<MetadataRecord>();
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord;

        if (headers is null || headers.Length == 0)
        {
            throw new OptionParsingException("CSV-filen mangler en gyldig header-raekke.");
        }

        if (!headers.Any(h => string.Equals(h, idColumn, StringComparison.OrdinalIgnoreCase)))
        {
            throw new OptionParsingException($"Kolonnen '{idColumn}' blev ikke fundet i CSV-filen.");
        }

        if (!headers.Any(h => string.Equals(h, urlColumn, StringComparison.OrdinalIgnoreCase)))
        {
            throw new OptionParsingException($"Kolonnen '{urlColumn}' blev ikke fundet i CSV-filen.");
        }

        var records = new List<MetadataRecord>();

        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = csv.GetField(idColumn)?.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var url = csv.TryGetField(urlColumn, out string? primary) ? primary?.Trim() : null;
            string? fallback = null;
            if (!string.IsNullOrWhiteSpace(fallbackUrlColumn) && csv.TryGetField(fallbackUrlColumn, out string? alt))
            {
                fallback = alt?.Trim();
            }

            records.Add(new MetadataRecord(id, string.IsNullOrWhiteSpace(url) ? null : url, string.IsNullOrWhiteSpace(fallback) ? null : fallback));
        }

        return records;
    }
}






