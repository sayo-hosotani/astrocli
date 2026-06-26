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

        var request = new ChartRequest(inputDateTime, location!, "western", "natal");
        var chart = NatalChartCalculator.Calculate(request);
        var json = JsonSerializer.Serialize(chart, JsonOptions.Default);
        standardOutput.WriteLine(json);
        return 0;
    }

    private static void WriteUsageError(TextWriter standardError)
    {
        standardError.WriteLine("Invalid arguments. Usage: astrocli \"yyyy-MM-dd HH:mm:ss zzz\" \"35°41’22″N,139°41’30″E\"");
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
