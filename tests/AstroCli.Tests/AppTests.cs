using System.Text.Json;
using SharpAstrology.Enums;
using SharpAstrology.Ephemerides;
using SharpAstrology.Interfaces;

namespace AstroCli.Tests;

public class AppTests
{
    private const string VerificationDateTime = "1989-07-08 05:19:00 +09:00";
    private const string VerificationLocation = "35°41’22″N,139°41’30″E";

    private static readonly BodyCase[] BodyCases =
    [
        new("sun", Planets.Sun, "105°40’31″", "Cancer", "15°40’31″"),
        new("moon", Planets.Moon, "160°52’03″", "Virgo", "10°52’03″"),
        new("mercury", Planets.Mercury, "93°32’41″", "Cancer", "3°32’41″"),
        new("venus", Planets.Venus, "130°22’42″", "Leo", "10°22’42″"),
        new("mars", Planets.Mars, "133°14’34″", "Leo", "13°14’34″"),
        new("jupiter", Planets.Jupiter, "85°01’36″", "Gemini", "25°01’36″"),
        new("saturn", Planets.Saturn, "280°13’48″", "Capricorn", "10°13’48″"),
        new("uranus", Planets.Uranus, "272°49’23″", "Capricorn", "2°49’23″"),
        new("neptune", Planets.Neptune, "280°52’10″", "Capricorn", "10°52’10″"),
        new("pluto", Planets.Pluto, "222°25’53″", "Scorpio", "12°25’53″")
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
            "114°29’59″",
            "Cancer",
            "24°29’59″");
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
    public void Calculate_UsesSharpAstrologySwissEphMoshierForBodyLongitudes()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var request = new ChartRequest(input, new GeoLocation(35.68944444444445, 139.69166666666666), "western", "natal");

        var chart = NatalChartCalculator.Calculate(request);

        foreach (var body in BodyCases)
        {
            Assert.Equal(ExpectedLongitude(body.Planet, input), BodyPositionFor(chart, body.JsonName).EclipticLongitude);
        }
    }

    [Fact]
    public void Calculate_UsesSharpAstrologySwissEphMoshierForAscendant()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var location = new GeoLocation(35.68944444444445, 139.69166666666666);
        var request = new ChartRequest(input, location, "western", "natal");

        var chart = NatalChartCalculator.Calculate(request);

        Assert.Equal(ExpectedAscendant(input, location), chart.Ascendant.EclipticLongitude);
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

    private static string ExpectedLongitude(Planets planet, DateTimeOffset input)
    {
        using IEphemerides ephemerides = new SwissEphemeridesService(ephType: EphType.Moshier).CreateContext();
        var position = ephemerides.PlanetsPosition(planet, input.ToUniversalTime().UtcDateTime);

        return SexagesimalDegreeFormatter.Format(position.Longitude);
    }

    private static string ExpectedAscendant(DateTimeOffset input, GeoLocation location)
    {
        using IEphemerides ephemerides = new SwissEphemeridesService(ephType: EphType.Moshier).CreateContext();
        var houses = ephemerides.HouseCuspPositions(
            input.ToUniversalTime().UtcDateTime,
            location.Latitude,
            location.Longitude,
            HouseSystems.Placidus);

        return SexagesimalDegreeFormatter.Format(houses.Cross[Cross.Asc]);
    }

    private sealed record BodyCase(
        string JsonName,
        Planets Planet,
        string EclipticLongitude,
        string Sign,
        string DegreeInSign);
}
