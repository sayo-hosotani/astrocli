using System.Globalization;
using System.Text.Json;

namespace AstroCli;

public static class AsteroidCommand
{
    private const string ExpectedDateTimeFormat = "yyyy-MM-dd HH:mm:ss zzz";

    public static int Run(
        string[] args,
        TextReader standardInput,
        TextWriter standardOutput,
        TextWriter standardError,
        IHorizonsClient? horizonsClient = null)
    {
        try
        {
            var options = ParseOptions(args);
            if (options is null)
            {
                WriteUsageError(standardError);
                return 1;
            }

            var calculator = new AsteroidPositionCalculator(horizonsClient ?? new HorizonsClient());
            var output = calculator.CalculateAsync(new AsteroidPositionRequest(options.At, KnownAsteroids.FixedTargets))
                .GetAwaiter()
                .GetResult();
            var json = JsonSerializer.Serialize(output, JsonOptions.Default);

            if (string.IsNullOrWhiteSpace(options.OutputPath) || options.OutputPath == "-")
            {
                standardOutput.WriteLine(json);
            }
            else
            {
                File.WriteAllText(options.OutputPath, json + Environment.NewLine);
            }

            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or HttpRequestException or IOException or JsonException)
        {
            standardError.WriteLine(ex.Message);
            return 1;
        }
    }

    private static AsteroidCommandOptions? ParseOptions(string[] args)
    {
        DateTimeOffset? at = null;
        string? outputPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (option is "--at" or "--output")
            {
                if (index + 1 >= args.Length)
                {
                    return null;
                }

                var value = args[++index];
                switch (option)
                {
                    case "--at":
                        if (!DateTimeOffset.TryParseExact(
                                value,
                                ExpectedDateTimeFormat,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var parsedAt))
                        {
                            return null;
                        }

                        at = parsedAt;
                        break;

                    case "--output":
                        outputPath = value;
                        break;
                }
            }
            else
            {
                return null;
            }
        }

        if (at is null)
        {
            return null;
        }

        return new AsteroidCommandOptions(at.Value, outputPath);
    }

    private static void WriteUsageError(TextWriter standardError)
    {
        standardError.WriteLine("Invalid arguments.");
        standardError.WriteLine("Usage: astrocli asteroid --at \"yyyy-MM-dd HH:mm:ss zzz\" [--output result.json|-]");
    }

    private sealed record AsteroidCommandOptions(
        DateTimeOffset At,
        string? OutputPath);
}
