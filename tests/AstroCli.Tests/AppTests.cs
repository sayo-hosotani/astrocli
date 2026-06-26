using System.Text.Json;
using CosineKitty;

namespace AstroCli.Tests;

public class AppTests
{
    private const string VerificationDateTime = "1989-07-08 05:19:00 +09:00";
    private const string VerificationLocation = "35°41’22″N,139°41’30″E";

    private static readonly BodyCase[] BodyCases =
    [
        new("sun", Body.Sun, "105°40’31″", "Cancer", "15°40’31″"),
        new("moon", Body.Moon, "160°52’04″", "Virgo", "10°52’04″"),
        new("mercury", Body.Mercury, "93°32’43″", "Cancer", "3°32’43″"),
        new("venus", Body.Venus, "130°22’43″", "Leo", "10°22’43″"),
        new("mars", Body.Mars, "133°14’35″", "Leo", "13°14’35″"),
        new("jupiter", Body.Jupiter, "85°01’36″", "Gemini", "25°01’36″"),
        new("saturn", Body.Saturn, "280°13’48″", "Capricorn", "10°13’48″"),
        new("uranus", Body.Uranus, "272°49’18″", "Capricorn", "2°49’18″"),
        new("neptune", Body.Neptune, "280°52’08″", "Capricorn", "10°52’08″"),
        new("pluto", Body.Pluto, "222°25’55″", "Scorpio", "12°25’55″")
    ];

    [Fact]
    public void Run_WithVerificationDateTime_WritesExpectedJson()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = App.Run([VerificationDateTime, VerificationLocation], stdout, stderr);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;

        Assert.Equal(VerificationDateTime, root.GetProperty("inputDateTime").GetString());
        Assert.Equal("1989-07-07T20:19:00Z", root.GetProperty("utcDateTime").GetString());
        Assert.Equal("western", root.GetProperty("system").GetString());
        Assert.Equal("natal", root.GetProperty("chart").GetString());

        var location = root.GetProperty("location");
        Assert.Equal("35°41’22″N", location.GetProperty("latitude").GetString());
        Assert.Equal("139°41’30″E", location.GetProperty("longitude").GetString());

        AssertBodySnapshot(
            root.GetProperty("ascendant"),
            "294°29’53″",
            "Capricorn",
            "24°29’53″");
        var bodies = root.GetProperty("bodies");
        foreach (var body in BodyCases)
        {
            AssertBodySnapshot(
                bodies.GetProperty(body.JsonName),
                body.EclipticLongitude,
                body.Sign,
                body.DegreeInSign);
        }
    }

    [Fact]
    public void Calculate_UsesAstronomyEngineForBodyLongitudes()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var request = new ChartRequest(input, new GeoLocation(35.68944444444445, 139.69166666666666), "western", "natal");

        var chart = NatalChartCalculator.Calculate(request);

        foreach (var body in BodyCases)
        {
            Assert.Equal(ExpectedLongitude(body.Body, input), BodyPositionFor(chart, body.JsonName).EclipticLongitude);
        }
    }

    [Fact]
    public void Run_WithInvalidDateTime_WritesErrorToStandardErrorAndReturnsNonZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = App.Run(["1989-07-08T05:19:00+09:00", VerificationLocation], stdout, stderr);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Invalid arguments", stderr.ToString());
    }

    [Fact]
    public void Run_WithInvalidLocation_WritesErrorToStandardErrorAndReturnsNonZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = App.Run([VerificationDateTime, "35.6895N,139.6917E"], stdout, stderr);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Invalid arguments", stderr.ToString());
    }

    [Theory]
    [InlineData("35°41’22″N,139°41’30″E", 35.68944444444445, 139.69166666666666)]
    [InlineData("33°52’08″S,151°12’33″E", -33.86888888888889, 151.20916666666668)]
    [InlineData("40°42’46″N,74°00’22″W", 40.71277777777778, -74.00611111111111)]
    public void GeoLocation_TryParse_ConvertsDirectionsToSignedDecimalDegrees(
        string value,
        double expectedLatitude,
        double expectedLongitude)
    {
        var success = GeoLocation.TryParse(value, out var location);

        Assert.True(success);
        Assert.NotNull(location);
        Assert.Equal(expectedLatitude, location.Latitude, 12);
        Assert.Equal(expectedLongitude, location.Longitude, 12);
    }

    [Theory]
    [InlineData("35.6895N,139.6917E")]
    [InlineData("91°00’01″N,139°41’30″E")]
    [InlineData("35°41’22″N,181°00’00″E")]
    [InlineData("35°41’22″E,139°41’30″N")]
    [InlineData("35°60’00″N,139°41’30″E")]
    [InlineData("35°41’60″N,139°41’30″E")]
    public void GeoLocation_TryParse_RejectsInvalidLocations(string value)
    {
        var success = GeoLocation.TryParse(value, out var location);

        Assert.False(success);
        Assert.Null(location);
    }

    private static void AssertBodySnapshot(JsonElement body, string longitude, string sign, string degreeInSign)
    {
        Assert.Equal(JsonValueKind.String, body.GetProperty("eclipticLongitude").ValueKind);
        Assert.Equal(JsonValueKind.String, body.GetProperty("sign").ValueKind);
        Assert.Equal(JsonValueKind.String, body.GetProperty("degreeInSign").ValueKind);
        Assert.Equal(longitude, body.GetProperty("eclipticLongitude").GetString());
        Assert.Equal(sign, body.GetProperty("sign").GetString());
        Assert.Equal(degreeInSign, body.GetProperty("degreeInSign").GetString());
    }

    private static BodyPosition BodyPositionFor(ChartOutput chart, string jsonName)
    {
        return jsonName switch
        {
            "sun" => chart.Bodies.Sun,
            "moon" => chart.Bodies.Moon,
            "mercury" => chart.Bodies.Mercury,
            "venus" => chart.Bodies.Venus,
            "mars" => chart.Bodies.Mars,
            "jupiter" => chart.Bodies.Jupiter,
            "saturn" => chart.Bodies.Saturn,
            "uranus" => chart.Bodies.Uranus,
            "neptune" => chart.Bodies.Neptune,
            "pluto" => chart.Bodies.Pluto,
            _ => throw new ArgumentOutOfRangeException(nameof(jsonName), jsonName, "Unknown body.")
        };
    }

    private static string ExpectedLongitude(Body body, DateTimeOffset input)
    {
        var time = new AstroTime(input.ToUniversalTime().UtcDateTime);
        var geoVector = Astronomy.GeoVector(body, time, Aberration.Corrected);
        var ecliptic = Astronomy.EquatorialToEcliptic(geoVector);
        var longitude = ecliptic.elon % 360.0;
        if (longitude < 0)
        {
            longitude += 360.0;
        }

        return SexagesimalDegreeFormatter.Format(longitude);
    }

    private sealed record BodyCase(
        string JsonName,
        Body Body,
        string EclipticLongitude,
        string Sign,
        string DegreeInSign);
}
