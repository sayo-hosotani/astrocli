using System.Text.Json;
using CosineKitty;
using SharpAstrology.Enums;
using SharpAstrology.Ephemerides;
using SharpAstrology.Interfaces;

namespace AstroCli.Tests;

public class AppTests
{
    private const string VerificationDateTime = "1989-07-08 05:19:00 +09:00";
    private const string VerificationLocation = "36°24’00″N,139°20’00″E";
    private const double VerificationLatitude = 36.4;
    private const double VerificationLongitude = 139.33333333333334;

    private static readonly BodyCase[] PlanetCases =
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

    private static readonly BodyCase[] ObjectCases =
    [
        new("northNode", Planets.NorthNode, "326°24’19″", "Aquarius", "26°24’19″"),
        new("southNode", Planets.SouthNode, "146°24’19″", "Leo", "26°24’19″")
    ];

    private static readonly AsteroidCase[] AsteroidCases =
    [
        new("chiron", "15°00’00″", "Aries", "15°00’00″"),
        new("ceres", "45°00’10″", "Taurus", "15°00’10″"),
        new("pallas", "75°00’17″", "Gemini", "15°00’17″"),
        new("juno", "105°00’22″", "Cancer", "15°00’22″"),
        new("vesta", "135°00’18″", "Leo", "15°00’18″")
    ];

    [Fact]
    public void Run_WithVerificationDateTime_WritesExpectedJson()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = App.Run(
            [VerificationDateTime, VerificationLocation],
            TextReader.Null,
            stdout,
            stderr,
            new FakeAsteroidHorizonsClient());

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;

        Assert.Equal(VerificationDateTime, root.GetProperty("inputDateTime").GetString());
        Assert.Equal("1989-07-07T20:19:00Z", root.GetProperty("utcDateTime").GetString());
        Assert.Equal("western", root.GetProperty("system").GetString());
        Assert.Equal("natal", root.GetProperty("chart").GetString());

        var location = root.GetProperty("location");
        Assert.Equal("36°24’00″N", location.GetProperty("latitude").GetString());
        Assert.Equal("139°20’00″E", location.GetProperty("longitude").GetString());

        Assert.False(root.TryGetProperty("ascendant", out _));
        Assert.False(root.TryGetProperty("bodies", out _));

        var houses = root.GetProperty("houses");
        Assert.Equal("placidus", houses.GetProperty("system").GetString());
        var cusps = houses.GetProperty("cusps");
        AssertBodySnapshot(cusps.GetProperty("house1"), "114°34’02″", "Cancer", "24°34’02″");
        AssertBodySnapshot(cusps.GetProperty("house2"), "135°37’06″", "Leo", "15°37’06″");
        AssertBodySnapshot(cusps.GetProperty("house3"), "160°15’14″", "Virgo", "10°15’14″");
        AssertBodySnapshot(cusps.GetProperty("house4"), "190°43’33″", "Libra", "10°43’33″");
        AssertBodySnapshot(cusps.GetProperty("house5"), "226°36’51″", "Scorpio", "16°36’51″");
        AssertBodySnapshot(cusps.GetProperty("house6"), "262°48’27″", "Sagittarius", "22°48’27″");
        AssertBodySnapshot(cusps.GetProperty("house7"), "294°34’02″", "Capricorn", "24°34’02″");
        AssertBodySnapshot(cusps.GetProperty("house8"), "315°37’06″", "Aquarius", "15°37’06″");
        AssertBodySnapshot(cusps.GetProperty("house9"), "340°15’14″", "Pisces", "10°15’14″");
        AssertBodySnapshot(cusps.GetProperty("house10"), "10°43’33″", "Aries", "10°43’33″");
        AssertBodySnapshot(cusps.GetProperty("house11"), "46°36’51″", "Taurus", "16°36’51″");
        AssertBodySnapshot(cusps.GetProperty("house12"), "82°48’27″", "Gemini", "22°48’27″");

        var planets = root.GetProperty("planets");
        foreach (var planet in PlanetCases)
        {
            AssertBodySnapshot(
                planets.GetProperty(planet.JsonName),
                planet.EclipticLongitude,
                planet.Sign,
                planet.DegreeInSign);
        }

        var asteroids = root.GetProperty("asteroids");
        foreach (var asteroid in AsteroidCases)
        {
            AssertBodySnapshot(
                asteroids.GetProperty(asteroid.JsonName),
                asteroid.EclipticLongitude,
                asteroid.Sign,
                asteroid.DegreeInSign);
        }

        var angles = root.GetProperty("angles");
        AssertBodySnapshot(angles.GetProperty("asc"), "114°34’02″", "Cancer", "24°34’02″");
        AssertBodySnapshot(angles.GetProperty("ic"), "190°43’33″", "Libra", "10°43’33″");
        AssertBodySnapshot(angles.GetProperty("dsc"), "294°34’02″", "Capricorn", "24°34’02″");
        AssertBodySnapshot(angles.GetProperty("mc"), "10°43’33″", "Aries", "10°43’33″");

        var objects = root.GetProperty("objects");
        foreach (var body in ObjectCases)
        {
            AssertBodySnapshot(
                objects.GetProperty(body.JsonName),
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
        var request = new ChartRequest(input, new GeoLocation(VerificationLatitude, VerificationLongitude), "western", "natal");

        var chart = NatalChartCalculator.Calculate(request, new FakeAsteroidHorizonsClient());

        foreach (var body in PlanetCases.Concat(ObjectCases))
        {
            Assert.Equal(ExpectedLongitude(body.Planet, input), BodyPositionFor(chart, body.JsonName).EclipticLongitude);
        }
    }

    [Fact]
    public void Calculate_UsesSharpAstrologySwissEphMoshierForAngles()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var location = new GeoLocation(VerificationLatitude, VerificationLongitude);
        var request = new ChartRequest(input, location, "western", "natal");

        var chart = NatalChartCalculator.Calculate(request, new FakeAsteroidHorizonsClient());

        Assert.Equal(ExpectedAngle(input, location, Cross.Asc), chart.Angles.Asc.EclipticLongitude);
        Assert.Equal(ExpectedAngle(input, location, Cross.Ic), chart.Angles.Ic.EclipticLongitude);
        Assert.Equal(ExpectedAngle(input, location, Cross.Dc), chart.Angles.Dsc.EclipticLongitude);
        Assert.Equal(ExpectedAngle(input, location, Cross.Mc), chart.Angles.Mc.EclipticLongitude);
    }

    [Fact]
    public void Calculate_UsesPlacidusHouseCusps()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var location = new GeoLocation(VerificationLatitude, VerificationLongitude);
        var request = new ChartRequest(input, location, "western", "natal");

        var chart = NatalChartCalculator.Calculate(request, new FakeAsteroidHorizonsClient());

        Assert.Equal("placidus", chart.Houses.System);
        Assert.Equal(chart.Angles.Asc.EclipticLongitude, chart.Houses.Cusps.House1.EclipticLongitude);
        Assert.Equal(ExpectedHouseCusp(input, location, Houses.House1), chart.Houses.Cusps.House1.EclipticLongitude);
        Assert.Equal(ExpectedHouseCusp(input, location, Houses.House10), chart.Houses.Cusps.House10.EclipticLongitude);
    }

    [Fact]
    public void Calculate_UsesAstronomyEngineAndHorizonsStateVectorsForAsteroids()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var location = new GeoLocation(VerificationLatitude, VerificationLongitude);
        var request = new ChartRequest(input, location, "western", "natal");

        var chart = NatalChartCalculator.Calculate(request, new FakeAsteroidHorizonsClient());

        foreach (var asteroid in AsteroidCases)
        {
            AssertBodySnapshot(
                BodyPositionFor(chart, asteroid.JsonName),
                asteroid.EclipticLongitude,
                asteroid.Sign,
                asteroid.DegreeInSign);
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

    private static void AssertBodySnapshot(BodyPosition body, string longitude, string sign, string degreeInSign)
    {
        Assert.Equal(longitude, body.EclipticLongitude);
        Assert.Equal(sign, body.Sign);
        Assert.Equal(degreeInSign, body.DegreeInSign);
    }

    private static BodyPosition BodyPositionFor(ChartOutput chart, string jsonName)
    {
        return jsonName switch
        {
            "sun" => chart.Planets.Sun,
            "moon" => chart.Planets.Moon,
            "mercury" => chart.Planets.Mercury,
            "venus" => chart.Planets.Venus,
            "mars" => chart.Planets.Mars,
            "jupiter" => chart.Planets.Jupiter,
            "saturn" => chart.Planets.Saturn,
            "uranus" => chart.Planets.Uranus,
            "neptune" => chart.Planets.Neptune,
            "pluto" => chart.Planets.Pluto,
            "northNode" => chart.Objects.NorthNode,
            "southNode" => chart.Objects.SouthNode,
            "chiron" => chart.Asteroids.Chiron,
            "ceres" => chart.Asteroids.Ceres,
            "pallas" => chart.Asteroids.Pallas,
            "juno" => chart.Asteroids.Juno,
            "vesta" => chart.Asteroids.Vesta,
            _ => throw new ArgumentOutOfRangeException(nameof(jsonName), jsonName, "Unknown body.")
        };
    }

    private static string ExpectedLongitude(Planets planet, DateTimeOffset input)
    {
        using IEphemerides ephemerides = new SwissEphemeridesService(ephType: EphType.Moshier).CreateContext();
        var position = ephemerides.PlanetsPosition(planet, input.ToUniversalTime().UtcDateTime);

        return SexagesimalDegreeFormatter.Format(position.Longitude);
    }

    private static string ExpectedAngle(DateTimeOffset input, GeoLocation location, Cross angle)
    {
        using IEphemerides ephemerides = new SwissEphemeridesService(ephType: EphType.Moshier).CreateContext();
        var houses = ephemerides.HouseCuspPositions(
            input.ToUniversalTime().UtcDateTime,
            location.Latitude,
            location.Longitude,
            HouseSystems.Placidus);

        return SexagesimalDegreeFormatter.Format(houses.Cross[angle]);
    }

    private static string ExpectedHouseCusp(DateTimeOffset input, GeoLocation location, Houses house)
    {
        using IEphemerides ephemerides = new SwissEphemeridesService(ephType: EphType.Moshier).CreateContext();
        var houses = ephemerides.HouseCuspPositions(
            input.ToUniversalTime().UtcDateTime,
            location.Latitude,
            location.Longitude,
            HouseSystems.Placidus);

        return SexagesimalDegreeFormatter.Format(houses.HouseCusps[house]);
    }

    private sealed record BodyCase(
        string JsonName,
        Planets Planet,
        string EclipticLongitude,
        string Sign,
        string DegreeInSign);

    private sealed record AsteroidCase(
        string JsonName,
        string EclipticLongitude,
        string Sign,
        string DegreeInSign);

    private sealed class FakeAsteroidHorizonsClient : IHorizonsClient
    {
        private static readonly IReadOnlyDictionary<string, double> Longitudes = new Dictionary<string, double>
        {
            ["chiron"] = 15.0,
            ["ceres"] = 45.0,
            ["pallas"] = 75.0,
            ["juno"] = 105.0,
            ["vesta"] = 135.0
        };

        public Task<HorizonsStateVector> GetStateVectorAsync(
            AsteroidTarget target,
            DateTimeOffset at,
            CancellationToken cancellationToken = default)
        {
            var state = StateVectorForLongitude(at, Longitudes[target.JsonName]);
            var vector = new HorizonsStateVector(
                target.Id,
                at,
                state.x,
                state.y,
                state.z,
                state.vx,
                state.vy,
                state.vz);
            return Task.FromResult(vector);
        }

        private static StateVector StateVectorForLongitude(DateTimeOffset at, double longitude)
        {
            var time = new AstroTime(at.ToUniversalTime().UtcDateTime);
            var simulator = new GravitySimulator(CosineKitty.Body.Sun, time, []);
            var earth = simulator.SolarSystemBodyState(CosineKitty.Body.Earth);
            var geocentricEcliptic = Astronomy.VectorFromSphere(new Spherical(0.0, longitude, 1.0), time);
            var geocentricEqj = Astronomy.RotateVector(Astronomy.Rotation_ECT_EQJ(time), geocentricEcliptic);

            return new StateVector(
                earth.x + geocentricEqj.x,
                earth.y + geocentricEqj.y,
                earth.z + geocentricEqj.z,
                earth.vx,
                earth.vy,
                earth.vz,
                time);
        }
    }
}
