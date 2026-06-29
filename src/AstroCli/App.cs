using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AstroCli;

public static class App
{
    private const string ExpectedDateTimeFormat = "yyyy-MM-dd HH:mm:ss zzz";

    public static int Run(string[] args, TextWriter standardOutput, TextWriter standardError)
    {
        return Run(args, Console.In, standardOutput, standardError);
    }

    public static int Run(
        string[] args,
        TextReader standardInput,
        TextWriter standardOutput,
        TextWriter standardError,
        IHorizonsClient? horizonsClient = null)
    {
        if (args.Length > 0 && string.Equals(args[0], "asteroid", StringComparison.OrdinalIgnoreCase))
        {
            return AsteroidCommand.Run(args[1..], standardInput, standardOutput, standardError);
        }

        if (args.Length != 2)
        {
            WriteUsageError(standardError);
            return 1;
        }

        if (!DateTimeOffset.TryParseExact(
                args[0],
                ExpectedDateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var inputDateTime))
        {
            WriteUsageError(standardError);
            return 1;
        }

        if (!GeoLocation.TryParse(args[1], out var location))
        {
            WriteUsageError(standardError);
            return 1;
        }

        try
        {
            var request = new ChartRequest(inputDateTime, location!, "western", "natal");
            var chart = NatalChartCalculator.Calculate(request, horizonsClient);
            var json = JsonSerializer.Serialize(chart, JsonOptions.Default);
            standardOutput.WriteLine(json);
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or HttpRequestException or IOException or JsonException)
        {
            standardError.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void WriteUsageError(TextWriter standardError)
    {
        standardError.WriteLine("Invalid arguments.");
        standardError.WriteLine("Usage: astrocli \"yyyy-MM-dd HH:mm:ss zzz\" \"35°41’22″N,139°41’30″E\"");
        standardError.WriteLine("Usage: astrocli asteroid --at \"yyyy-MM-dd HH:mm:ss zzz\" [--output result.json|-]");
    }
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };
}
