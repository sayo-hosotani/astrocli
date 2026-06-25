using System.Text.Json;
using CosineKitty;

namespace AstroCli.Tests;

public class AppTests
{
    private const string VerificationDateTime = "1989-07-08 05:19:00 +09:00";

    [Fact]
    public void Run_WithVerificationDateTime_WritesExpectedJson()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = App.Run([VerificationDateTime], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;

        Assert.Equal(VerificationDateTime, root.GetProperty("inputDateTime").GetString());
        Assert.Equal("1989-07-07T20:19:00Z", root.GetProperty("utcDateTime").GetString());
        Assert.Equal("western", root.GetProperty("system").GetString());
        Assert.Equal("natal", root.GetProperty("chart").GetString());

        AssertBodySnapshot(
            root.GetProperty("bodies").GetProperty("sun"),
            105.675329,
            "Cancer",
            15.675329);
        AssertBodySnapshot(
            root.GetProperty("bodies").GetProperty("moon"),
            160.867755,
            "Virgo",
            10.867755);
    }

    [Fact]
    public void Calculate_UsesAstronomyEngineForSunAndMoonLongitude()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var request = new ChartRequest(input, "western", "natal");

        var chart = NatalChartCalculator.Calculate(request);

        Assert.Equal(ExpectedLongitude(Body.Sun, input), chart.Bodies.Sun.EclipticLongitude, 6);
        Assert.Equal(ExpectedLongitude(Body.Moon, input), chart.Bodies.Moon.EclipticLongitude, 6);
    }

    [Fact]
    public void Run_WithInvalidDateTime_WritesErrorToStandardErrorAndReturnsNonZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = App.Run(["1989-07-08T05:19:00+09:00"], stdout, stderr);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Invalid arguments", stderr.ToString());
    }

    private static void AssertBodySnapshot(JsonElement body, double longitude, string sign, double degreeInSign)
    {
        Assert.Equal(JsonValueKind.Number, body.GetProperty("eclipticLongitude").ValueKind);
        Assert.Equal(JsonValueKind.String, body.GetProperty("sign").ValueKind);
        Assert.Equal(JsonValueKind.Number, body.GetProperty("degreeInSign").ValueKind);
        Assert.Equal(longitude, body.GetProperty("eclipticLongitude").GetDouble(), 6);
        Assert.Equal(sign, body.GetProperty("sign").GetString());
        Assert.Equal(degreeInSign, body.GetProperty("degreeInSign").GetDouble(), 6);
    }

    private static double ExpectedLongitude(Body body, DateTimeOffset input)
    {
        var time = new AstroTime(input.ToUniversalTime().UtcDateTime);
        var geoVector = Astronomy.GeoVector(body, time, Aberration.Corrected);
        var ecliptic = Astronomy.EquatorialToEcliptic(geoVector);
        var longitude = ecliptic.elon % 360.0;
        if (longitude < 0)
        {
            longitude += 360.0;
        }

        return Math.Round(longitude, 6, MidpointRounding.AwayFromZero);
    }
}
