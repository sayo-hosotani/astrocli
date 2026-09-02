using System.Text.Json;

namespace AstroCli.Tests;

public class DashaCommandTests
{
    [Fact]
    public void Run_UsesChartFileAndWritesDefaultTwoLevelTree()
    {
        using var fixture = new DashaFixture();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = DashaCommand.Run([fixture.RequestPath], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Equal("Processed: 1 | Success: 1\n", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
        using var json = JsonDocument.Parse(File.ReadAllText(fixture.OutputPath));
        var root = json.RootElement;
        Assert.Equal("Vimshottari", root.GetProperty("meta").GetProperty("dashaSystem").GetString());
        Assert.Equal(2, root.GetProperty("meta").GetProperty("depth").GetInt32());
        Assert.Equal("1989-07-08T05:19:00.000+09:00", root.GetProperty("birth").GetProperty("dateTime").GetString());
        Assert.Equal(11, root.GetProperty("birth").GetProperty("moonNakshatra").GetProperty("number").GetInt32());
        Assert.Equal("Pūrva Phalgunī", root.GetProperty("birth").GetProperty("moonNakshatra").GetProperty("name").GetString());
        Assert.Equal("Venus", root.GetProperty("birth").GetProperty("moonNakshatra").GetProperty("lord").GetString());
        Assert.True(root.GetProperty("dashas").GetArrayLength() >= 6);
        var first = root.GetProperty("dashas")[0];
        Assert.Equal("Venus", first.GetProperty("lord").GetString());
        Assert.True(DateTimeOffset.Parse(first.GetProperty("startDate").GetString()!) < DateTimeOffset.Parse(root.GetProperty("birth").GetProperty("dateTime").GetString()!));
        Assert.True(DateTimeOffset.Parse(first.GetProperty("endDate").GetString()!) > DateTimeOffset.Parse(root.GetProperty("birth").GetProperty("dateTime").GetString()!));
        var completeParent = root.GetProperty("dashas").EnumerateArray().First(parent => parent.GetProperty("children").GetArrayLength() == 9);
        Assert.Equal(completeParent.GetProperty("children")[0].GetProperty("endDate").GetString(), completeParent.GetProperty("children")[1].GetProperty("startDate").GetString());
        Assert.Equal(completeParent.GetProperty("endDate").GetString(), completeParent.GetProperty("children")[8].GetProperty("endDate").GetString());
        Assert.False(root.TryGetProperty("initialMahadasha", out _));
    }

    [Fact]
    public void Calculate_DefaultDepthThreeUsesCurrentMahadashaPeriodAndReferenceDate()
    {
        using var fixture = new DashaFixture("""
            {
              "source": { "chartFile": "chart.json" },
              "output": { "depth": 3, "referenceDateTime": "1990-01-01T00:00:00Z" }
            }
            """);

        var result = DashaCalculator.Calculate(fixture.RequestPath);

        Assert.Equal("1990-01-01T09:00:00.000+09:00", result.Meta.ReferenceDateTime);
        Assert.Single(result.Dashas);
        Assert.Equal(3, result.Dashas[0].Children![0].Children![0].LevelNumber);
        Assert.All(result.Dashas[0].Children![0].Children!, child => Assert.NotNull(child));
    }

    [Fact]
    public void Calculate_EmitsLegacyWarningsWithoutUsingLegacyDashaValues()
    {
        using var fixture = new DashaFixture("""
            {
              "source": { "chartFile": "chart.json" },
              "output": { "depth": 1, "period": { "start": "1989-07-08T00:00:00Z", "end": "1989-07-09T00:00:00Z" } }
            }
            """, chart: """
            {
              "input": { "utcOffsetHours": 9 },
              "vedic_sidereal_lahiri": {
                "utcIso": "1989-07-07T20:19:00Z",
                "siderealLons": { "Moon": 137.15712411385647 },
                "ayanamshaDeg": 24.0,
                "nakshatras": { "Moon": { "nakIdx": 10, "nakName": "wrong", "pada": 2, "ruler": "Moon" } },
                "dasha": { "Mahadasha": "not an input" }
              },
              "western_tropical_placidus": { "lons": { "Moon": 161.28052411385647 } }
            }
            """);

        var result = DashaCalculator.Calculate(fixture.RequestPath);

        Assert.Equal(3, result.Warnings!.Count);
        Assert.Contains(result.Warnings, warning => warning.Field.EndsWith("nakName", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Field.EndsWith("ruler", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Code == "LEGACY_AYANAMSHA_MISMATCH");
        Assert.Equal("Venus", result.Dashas[0].Lord);
    }

    [Fact]
    public void Run_ErrorRemovesOldOutputAndContinuesBatch()
    {
        using var fixture = new DashaFixture();
        var badRequest = Path.Combine(fixture.Directory, "bad.json");
        File.WriteAllText(badRequest, "{ \"source\": {} }");
        File.WriteAllText(Path.Combine(fixture.Directory, "bad_dasha.json"), "old");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = DashaCommand.Run([badRequest, fixture.RequestPath], stdout, stderr);

        Assert.Equal(1, exitCode);
        Assert.Contains("Processed: 2 | Success: 1 | Error: 1", stdout.ToString());
        Assert.False(File.Exists(Path.Combine(fixture.Directory, "bad_dasha.json")));
        Assert.True(File.Exists(fixture.OutputPath));
    }

    private sealed class DashaFixture : IDisposable
    {
        public string Directory { get; } = System.IO.Directory.CreateTempSubdirectory("astrocli-dasha-").FullName;
        public string RequestPath { get; }
        public string OutputPath => Path.Combine(Directory, "request_dasha.json");

        public DashaFixture(string? request = null, string? chart = null)
        {
            File.WriteAllText(Path.Combine(Directory, "chart.json"), chart ?? DefaultChart);
            RequestPath = Path.Combine(Directory, "request.json");
            File.WriteAllText(RequestPath, request ?? "{ \"source\": { \"chartFile\": \"chart.json\" } }");
        }

        public void Dispose() => System.IO.Directory.Delete(Directory, true);

        private const string DefaultChart = """
            {
              "input": { "utcOffsetHours": 9 },
              "vedic_sidereal_lahiri": {
                "utcIso": "1989-07-07T20:19:00Z",
                "siderealLons": { "Moon": 137.15712411385647 },
                "ayanamshaDeg": 24.1234,
                "nakshatras": { "Moon": { "nakIdx": 10, "nakName": "プールヴァファルグニー", "pada": 2, "ruler": "Venus" } }
              },
              "western_tropical_placidus": { "lons": { "Moon": 161.28052411385647 } }
            }
            """;
    }
}
