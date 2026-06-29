using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AstroCli;

public interface IHorizonsClient
{
    Task<HorizonsStateVector> GetStateVectorAsync(AsteroidTarget target, DateTimeOffset at, CancellationToken cancellationToken = default);
}

public sealed class HorizonsClient : IHorizonsClient
{
    private static readonly Uri Endpoint = new("https://ssd.jpl.nasa.gov/api/horizons.api");
    private readonly HttpClient httpClient;

    public HorizonsClient()
        : this(new HttpClient())
    {
    }

    public HorizonsClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<HorizonsStateVector> GetStateVectorAsync(AsteroidTarget target, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(target, at);
        using var response = await httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return HorizonsStateVectorParser.ParseJson(target.Id, at, json);
    }

    private static Uri BuildUri(AsteroidTarget target, DateTimeOffset at)
    {
        var utc = at.ToUniversalTime();
        var start = FormatHorizonsDateTime(utc);
        var stop = FormatHorizonsDateTime(utc.AddDays(1));
        var parameters = new Dictionary<string, string>
        {
            ["format"] = "json",
            ["MAKE_EPHEM"] = "YES",
            ["EPHEM_TYPE"] = "VECTORS",
            ["COMMAND"] = Quote(target.HorizonsCommand),
            ["CENTER"] = Quote("@10"),
            ["START_TIME"] = Quote(start),
            ["STOP_TIME"] = Quote(stop),
            ["STEP_SIZE"] = Quote("1 d"),
            ["OUT_UNITS"] = "AU-D",
            ["REF_SYSTEM"] = "ICRF",
            ["REF_PLANE"] = "FRAME",
            ["CSV_FORMAT"] = "YES",
            ["VEC_TABLE"] = "2",
            ["OBJ_DATA"] = "NO"
        };

        var query = string.Join(
            "&",
            parameters.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return new UriBuilder(Endpoint) { Query = query }.Uri;
    }

    private static string FormatHorizonsDateTime(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MMM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static string Quote(string value)
    {
        return $"'{value}'";
    }
}

public static class HorizonsStateVectorParser
{
    private static readonly Regex NamedVectorPattern = new(
        @"\b(?<name>X|Y|Z|VX|VY|VZ)\s*=\s*(?<value>[-+]?\d+(?:\.\d*)?(?:[Ee][-+]?\d+)?)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static HorizonsStateVector ParseJson(string targetId, DateTimeOffset requestedAt, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("error", out var error))
        {
            throw new InvalidOperationException($"JPL Horizons error for {targetId}: {error.GetString()}");
        }

        if (!root.TryGetProperty("result", out var resultProperty) || resultProperty.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"JPL Horizons response for {targetId} does not contain a result string.");
        }

        return ParseResult(targetId, requestedAt, resultProperty.GetString() ?? string.Empty);
    }

    public static HorizonsStateVector ParseResult(string targetId, DateTimeOffset requestedAt, string result)
    {
        var table = ExtractTable(targetId, result);
        var named = ParseNamedVector(targetId, requestedAt, table);
        if (named is not null)
        {
            return named;
        }

        return ParseCsvVector(targetId, requestedAt, table);
    }

    private static string ExtractTable(string targetId, string result)
    {
        var start = result.IndexOf("$$SOE", StringComparison.Ordinal);
        var end = result.IndexOf("$$EOE", StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException($"JPL Horizons result for {targetId} does not contain a state vector table.");
        }

        return result[(start + "$$SOE".Length)..end];
    }

    private static HorizonsStateVector? ParseNamedVector(string targetId, DateTimeOffset requestedAt, string table)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in NamedVectorPattern.Matches(table))
        {
            values[match.Groups["name"].Value] = double.Parse(
                match.Groups["value"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
        }

        return values.Count >= 6
            ? new HorizonsStateVector(
                targetId,
                requestedAt,
                values["X"],
                values["Y"],
                values["Z"],
                values["VX"],
                values["VY"],
                values["VZ"])
            : null;
    }

    private static HorizonsStateVector ParseCsvVector(string targetId, DateTimeOffset requestedAt, string table)
    {
        foreach (var rawLine in table.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = rawLine.Split(',', StringSplitOptions.TrimEntries);
            if (fields.Length < 8)
            {
                continue;
            }

            if (TryParseDouble(fields[2], out var x)
                && TryParseDouble(fields[3], out var y)
                && TryParseDouble(fields[4], out var z)
                && TryParseDouble(fields[5], out var vx)
                && TryParseDouble(fields[6], out var vy)
                && TryParseDouble(fields[7], out var vz))
            {
                return new HorizonsStateVector(targetId, requestedAt, x, y, z, vx, vy, vz);
            }
        }

        throw new InvalidOperationException($"JPL Horizons result for {targetId} does not contain parseable X/Y/Z/VX/VY/VZ values.");
    }

    private static bool TryParseDouble(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}

public sealed record HorizonsStateVector(
    string TargetId,
    DateTimeOffset Epoch,
    double X,
    double Y,
    double Z,
    double Vx,
    double Vy,
    double Vz);
