using System.Text.Json;
using CosineKitty;
using SharpAstrology.Enums;
using SharpAstrology.Ephemerides;
using SharpAstrology.Interfaces;
using SharpAstrology.SwissEphemerides;
using SharpAstrology.SwissEphemerides.Application.Bodies;
using SharpAstrology.SwissEphemerides.Domain.Time;

namespace AstroCli.Tests;

public class AppTests
{
    private const string VerificationDateTime = "1989-07-08 05:19:00 +09:00";
    private const string NightChartDateTime = "1991-06-06 04:05:00 +09:00";
    private const string VerificationLocation = "36°24’00″N,139°20’00″E";
    private const double VerificationLatitude = 36.4;
    private const double VerificationLongitude = 139.33333333333334;

    private static readonly BodyCase[] PlanetCases =
    [
        new("sun", Planets.Sun, "105°40’31″", "Cancer", "15°40’31″", 12),
        new("moon", Planets.Moon, "160°52’03″", "Virgo", "10°52’03″", 3),
        new("mercury", Planets.Mercury, "93°32’41″", "Cancer", "3°32’41″", 12),
        new("venus", Planets.Venus, "130°22’42″", "Leo", "10°22’42″", 1),
        new("mars", Planets.Mars, "133°14’34″", "Leo", "13°14’34″", 1),
        new("jupiter", Planets.Jupiter, "85°01’36″", "Gemini", "25°01’36″", 12),
        new("saturn", Planets.Saturn, "280°13’48″", "Capricorn", "10°13’48″", 6),
        new("uranus", Planets.Uranus, "272°49’23″", "Capricorn", "2°49’23″", 6),
        new("neptune", Planets.Neptune, "280°52’10″", "Capricorn", "10°52’10″", 6),
        new("pluto", Planets.Pluto, "222°25’53″", "Scorpio", "12°25’53″", 4)
    ];

    private static readonly BodyCase[] ObjectCases =
    [
        new("northNode", Planets.NorthNode, "326°24’19″", "Aquarius", "26°24’19″", 8),
        new("southNode", Planets.SouthNode, "146°24’19″", "Leo", "26°24’19″", 2)
    ];

    private static readonly AsteroidCase[] AdditionalObjectCases =
    [
        new("partOfFortune", "169°45’34″", "Virgo", "19°45’34″", 3),
        new("vertex", "248°46’52″", "Sagittarius", "8°46’52″", 5),
        new("antiVertex", "68°46’52″", "Gemini", "8°46’52″", 11),
        new("lilith", "201°45’48″", "Libra", "21°45’48″", 4)
    ];

    private static readonly AsteroidCase[] AsteroidCases =
    [
        new("chiron", "15°00’00″", "Aries", "15°00’00″", 10),
        new("ceres", "45°00’10″", "Taurus", "15°00’10″", 10),
        new("pallas", "75°00’17″", "Gemini", "15°00’17″", 11),
        new("juno", "105°00’22″", "Cancer", "15°00’22″", 12),
        new("vesta", "135°00’18″", "Leo", "15°00’18″", 1)
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
        Assert.False(root.TryGetProperty("planets", out _));
        Assert.False(root.TryGetProperty("asteroids", out _));
        Assert.False(root.TryGetProperty("angles", out _));
        Assert.False(root.TryGetProperty("objects", out _));

        var sabianSymbols = root.GetProperty("sabianSymbols");

        Assert.False(root.TryGetProperty("houses", out _));
        Assert.Equal("placidus", root.GetProperty("houseSystem").GetString());
        Assert.True(root.TryGetProperty("culminate", out var culminate));
        Assert.True(root.TryGetProperty("houseCusps", out var cusps));
        Assert.True(GetPropertyIndex(root, "culminate") < GetPropertyIndex(root, "houseCusps"));
        Assert.False(root.TryGetProperty("culminatingPlanet", out _));
        Assert.Equal("jupiter", culminate.GetProperty("planet").GetString());
        Assert.Equal("74°18’03″", culminate.GetProperty("distanceFromMc").GetString());
        AssertPositionSnapshot(cusps.GetProperty("1"), "114°34’02″", "Cancer", "24°34’02″", sabianSymbols.GetProperty("house1"));
        AssertPositionSnapshot(cusps.GetProperty("2"), "135°37’06″", "Leo", "15°37’06″", sabianSymbols.GetProperty("house2"));
        AssertPositionSnapshot(cusps.GetProperty("3"), "160°15’14″", "Virgo", "10°15’14″", sabianSymbols.GetProperty("house3"));
        AssertPositionSnapshot(cusps.GetProperty("4"), "190°43’33″", "Libra", "10°43’33″", sabianSymbols.GetProperty("house4"));
        AssertPositionSnapshot(cusps.GetProperty("5"), "226°36’51″", "Scorpio", "16°36’51″", sabianSymbols.GetProperty("house5"));
        AssertPositionSnapshot(cusps.GetProperty("6"), "262°48’27″", "Sagittarius", "22°48’27″", sabianSymbols.GetProperty("house6"));
        AssertPositionSnapshot(cusps.GetProperty("7"), "294°34’02″", "Capricorn", "24°34’02″", sabianSymbols.GetProperty("house7"));
        AssertPositionSnapshot(cusps.GetProperty("8"), "315°37’06″", "Aquarius", "15°37’06″", sabianSymbols.GetProperty("house8"));
        AssertPositionSnapshot(cusps.GetProperty("9"), "340°15’14″", "Pisces", "10°15’14″", sabianSymbols.GetProperty("house9"));
        AssertPositionSnapshot(cusps.GetProperty("10"), "10°43’33″", "Aries", "10°43’33″", sabianSymbols.GetProperty("house10"));
        AssertPositionSnapshot(cusps.GetProperty("11"), "46°36’51″", "Taurus", "16°36’51″", sabianSymbols.GetProperty("house11"));
        AssertPositionSnapshot(cusps.GetProperty("12"), "82°48’27″", "Gemini", "22°48’27″", sabianSymbols.GetProperty("house12"));

        var points = root.GetProperty("points");
        foreach (var planet in PlanetCases)
        {
            AssertPointSnapshot(
                points.GetProperty(planet.JsonName),
                "planet",
                planet.EclipticLongitude,
                planet.Sign,
                planet.DegreeInSign,
                sabianSymbols.GetProperty(planet.JsonName),
                planet.House);
        }

        foreach (var asteroid in AsteroidCases)
        {
            AssertPointSnapshot(
                points.GetProperty(asteroid.JsonName),
                "asteroid",
                asteroid.EclipticLongitude,
                asteroid.Sign,
                asteroid.DegreeInSign,
                sabianSymbols.GetProperty(asteroid.JsonName),
                asteroid.House);
        }

        AssertPointSnapshot(points.GetProperty("asc"), "angle", "114°34’02″", "Cancer", "24°34’02″", sabianSymbols.GetProperty("asc"));
        AssertPointSnapshot(points.GetProperty("ic"), "angle", "190°43’33″", "Libra", "10°43’33″", sabianSymbols.GetProperty("ic"));
        AssertPointSnapshot(points.GetProperty("dsc"), "angle", "294°34’02″", "Capricorn", "24°34’02″", sabianSymbols.GetProperty("dsc"));
        AssertPointSnapshot(points.GetProperty("mc"), "angle", "10°43’33″", "Aries", "10°43’33″", sabianSymbols.GetProperty("mc"));

        foreach (var body in ObjectCases)
        {
            AssertPointSnapshot(
                points.GetProperty(body.JsonName),
                "object",
                body.EclipticLongitude,
                body.Sign,
                body.DegreeInSign,
                sabianSymbols.GetProperty(body.JsonName),
                body.House);
        }

        foreach (var body in AdditionalObjectCases)
        {
            AssertPointSnapshot(
                points.GetProperty(body.JsonName),
                "object",
                body.EclipticLongitude,
                body.Sign,
                body.DegreeInSign,
                sabianSymbols.GetProperty(body.JsonName),
                body.House);
        }

        var dispositors = root.GetProperty("dispositors").EnumerateArray().ToArray();
        AssertDispositorLoop(
            dispositors,
            ["moon", "mercury"],
            [
                ("sun", "moon"),
                ("moon", "mercury"),
                ("mercury", "moon"),
                ("venus", "sun"),
                ("mars", "sun"),
                ("jupiter", "mercury")
            ]);
        AssertDispositorRoot(dispositors, "saturn", [("saturn", "saturn"), ("uranus", "saturn"), ("neptune", "saturn")]);
        AssertDispositorRoot(dispositors, "pluto", [("pluto", "pluto")]);
        AssertDispositorPairsOnlyUsePlanets(dispositors);

        var aspects = root.GetProperty("aspects").EnumerateArray().ToArray();
        Assert.NotEmpty(aspects);
        Assert.DoesNotContain(aspects, aspect => AspectPointsEqual(aspect, ["northNode", "southNode"]));
        Assert.DoesNotContain(aspects, aspect => AspectPointsEqual(aspect, ["vertex", "antiVertex"]));
        var sunMoon = aspects.Single(aspect =>
            AspectPointsEqual(aspect, ["sun", "moon"]));
        Assert.Equal("sextile", sunMoon.GetProperty("aspect").GetString());
        Assert.False(sunMoon.TryGetProperty("angle", out _));
        Assert.Equal("4°48’28″", sunMoon.GetProperty("orb").GetString());

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

        Assert.Equal(ExpectedAngle(input, location, Cross.Asc), chart.Points.Asc.EclipticLongitude);
        Assert.Equal(ExpectedAngle(input, location, Cross.Ic), chart.Points.Ic.EclipticLongitude);
        Assert.Equal(ExpectedAngle(input, location, Cross.Dc), chart.Points.Dsc.EclipticLongitude);
        Assert.Equal(ExpectedAngle(input, location, Cross.Mc), chart.Points.Mc.EclipticLongitude);
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

        Assert.Equal("placidus", chart.HouseSystem);
        Assert.Equal(chart.Points.Asc.EclipticLongitude, chart.HouseCusps.House1.EclipticLongitude);
        Assert.Equal(ExpectedHouseCusp(input, location, Houses.House1), chart.HouseCusps.House1.EclipticLongitude);
        Assert.Equal(ExpectedHouseCusp(input, location, Houses.House10), chart.HouseCusps.House10.EclipticLongitude);
    }

    [Fact]
    public void Calculate_UsesNightFormulaForPartOfFortuneWhenSunIsBelowHorizon()
    {
        var input = DateTimeOffset.ParseExact(
            NightChartDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var location = new GeoLocation(VerificationLatitude, VerificationLongitude);
        var request = new ChartRequest(input, location, "western", "natal");

        var chart = NatalChartCalculator.Calculate(request, new FakeAsteroidHorizonsClient());

        AssertPointSnapshot(chart.Points.PartOfFortune, "object", "156°19’33″", "Virgo", "6°19’33″", 4);
        Assert.Equal(ExpectedNightPartOfFortune(input, location), chart.Points.PartOfFortune.EclipticLongitude);
    }

    [Fact]
    public void Calculate_UsesOsculatingApogeeForLilith()
    {
        var input = DateTimeOffset.ParseExact(
            VerificationDateTime,
            "yyyy-MM-dd HH:mm:ss zzz",
            System.Globalization.CultureInfo.InvariantCulture);
        var location = new GeoLocation(VerificationLatitude, VerificationLongitude);
        var request = new ChartRequest(input, location, "western", "natal");

        var chart = NatalChartCalculator.Calculate(request, new FakeAsteroidHorizonsClient());

        Assert.Equal(ExpectedLilith(input), chart.Points.Lilith.EclipticLongitude);
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
            AssertPointSnapshot(
                BodyPositionFor(chart, asteroid.JsonName),
                "asteroid",
                asteroid.EclipticLongitude,
                asteroid.Sign,
                asteroid.DegreeInSign,
                asteroid.House);
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

    private static void AssertPositionSnapshot(
        JsonElement body,
        string longitude,
        string sign,
        string degreeInSign,
        JsonElement sabian,
        int? house = null)
    {
        AssertPositionSnapshot(body, longitude, sign, degreeInSign, sabian, allowsType: false, house);
    }

    private static void AssertPositionSnapshot(
        JsonElement body,
        string longitude,
        string sign,
        string degreeInSign,
        JsonElement sabian,
        bool allowsType,
        int? house = null)
    {
        Assert.Equal(JsonValueKind.String, body.GetProperty("eclipticLongitude").ValueKind);
        Assert.Equal(JsonValueKind.String, body.GetProperty("sign").ValueKind);
        Assert.Equal(JsonValueKind.String, body.GetProperty("degreeInSign").ValueKind);
        Assert.False(body.TryGetProperty("name", out _));
        if (!allowsType)
        {
            Assert.False(body.TryGetProperty("type", out _));
        }

        Assert.False(body.TryGetProperty("sabian", out _));
        Assert.False(body.TryGetProperty("dispositor", out _));
        Assert.Equal(longitude, body.GetProperty("eclipticLongitude").GetString());
        Assert.Equal(sign, body.GetProperty("sign").GetString());
        Assert.Equal(degreeInSign, body.GetProperty("degreeInSign").GetString());

        var sabianDegree = SabianDegreeFor(degreeInSign);
        Assert.Equal(SabianIndexFor(sign, sabianDegree), sabian.GetProperty("index").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(sabian.GetProperty("symbol").GetString()));
        Assert.False(sabian.TryGetProperty("sign", out _));
        Assert.False(sabian.TryGetProperty("degree", out _));
        Assert.False(sabian.TryGetProperty("degreeName", out _));

        if (house is null)
        {
            Assert.False(body.TryGetProperty("house", out _));
        }
        else
        {
            Assert.Equal(JsonValueKind.Number, body.GetProperty("house").ValueKind);
            Assert.Equal(house, body.GetProperty("house").GetInt32());
        }
    }

    private static void AssertPointSnapshot(
        JsonElement body,
        string type,
        string longitude,
        string sign,
        string degreeInSign,
        JsonElement sabian,
        int? house = null)
    {
        Assert.Equal(type, body.GetProperty("type").GetString());
        AssertPositionSnapshot(body, longitude, sign, degreeInSign, sabian, allowsType: true, house);
    }

    private static void AssertPointSnapshot(
        PointOutput body,
        string type,
        string longitude,
        string sign,
        string degreeInSign,
        int? house = null)
    {
        Assert.Equal(type, body.Type);
        Assert.Equal(longitude, body.EclipticLongitude);
        Assert.Equal(sign, body.Sign);
        Assert.Equal(degreeInSign, body.DegreeInSign);
        Assert.Equal(house, body.House);
    }

    private static int GetPropertyIndex(JsonElement element, string propertyName)
    {
        var index = 0;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name == propertyName)
            {
                return index;
            }

            index++;
        }

        throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "JSON property was not found.");
    }

    private static void AssertDispositorRoot(
        IReadOnlyList<JsonElement> groups,
        string root,
        IReadOnlyList<(string Point, string Dispositor)> expectedPairs)
    {
        var group = groups.Single(candidate =>
            candidate.TryGetProperty("root", out var rootProperty)
            && rootProperty.GetString() == root);

        Assert.False(group.TryGetProperty("loop", out _));
        AssertDispositorPairs(group, expectedPairs);
    }

    private static void AssertDispositorLoop(
        IReadOnlyList<JsonElement> groups,
        IReadOnlyList<string> loop,
        IReadOnlyList<(string Point, string Dispositor)> expectedPairs)
    {
        var group = groups.Single(candidate =>
            candidate.TryGetProperty("loop", out var loopProperty)
            && loopProperty.EnumerateArray().Select(item => item.GetString()).SequenceEqual(loop));

        Assert.False(group.TryGetProperty("root", out _));
        AssertDispositorPairs(group, expectedPairs);
    }

    private static void AssertDispositorPairs(
        JsonElement group,
        IReadOnlyList<(string Point, string Dispositor)> expectedPairs)
    {
        var pairs = group.GetProperty("dispositors")
            .EnumerateArray()
            .Select(pair => (Point: pair.GetProperty("point").GetString(), Dispositor: pair.GetProperty("dispositor").GetString()))
            .ToArray();

        foreach (var expected in expectedPairs)
        {
            Assert.Contains((expected.Point, expected.Dispositor), pairs);
        }
    }

    private static void AssertDispositorPairsOnlyUsePlanets(IReadOnlyList<JsonElement> groups)
    {
        var planets = PlanetCases.Select(planet => planet.JsonName).ToHashSet(StringComparer.Ordinal);
        foreach (var pair in groups.SelectMany(group => group.GetProperty("dispositors").EnumerateArray()))
        {
            Assert.Contains(pair.GetProperty("point").GetString()!, planets);
            Assert.Contains(pair.GetProperty("dispositor").GetString()!, planets);
        }
    }

    private static bool AspectPointsEqual(JsonElement aspect, IReadOnlyList<string> points)
    {
        return aspect.GetProperty("points")
            .EnumerateArray()
            .Select(point => point.GetString())
            .SequenceEqual(points);
    }

    [Fact]
    public void SabianSymbols_UsesJonesVersionEnglishSymbolNames()
    {
        Assert.Equal(
            "A woman rises out of the water, a seal rises and embraces her",
            SabianSymbols.SymbolForIndex(1));
        Assert.Equal("A public market.", SabianSymbols.SymbolForIndex(331));
        Assert.Equal("The Great Stone Face", SabianSymbols.SymbolForIndex(360));
    }

    private static PointOutput BodyPositionFor(ChartOutput chart, string jsonName)
    {
        return jsonName switch
        {
            "sun" => chart.Points.Sun,
            "moon" => chart.Points.Moon,
            "mercury" => chart.Points.Mercury,
            "venus" => chart.Points.Venus,
            "mars" => chart.Points.Mars,
            "jupiter" => chart.Points.Jupiter,
            "saturn" => chart.Points.Saturn,
            "uranus" => chart.Points.Uranus,
            "neptune" => chart.Points.Neptune,
            "pluto" => chart.Points.Pluto,
            "northNode" => chart.Points.NorthNode,
            "southNode" => chart.Points.SouthNode,
            "partOfFortune" => chart.Points.PartOfFortune,
            "vertex" => chart.Points.Vertex,
            "antiVertex" => chart.Points.AntiVertex,
            "lilith" => chart.Points.Lilith,
            "chiron" => chart.Points.Chiron,
            "ceres" => chart.Points.Ceres,
            "pallas" => chart.Points.Pallas,
            "juno" => chart.Points.Juno,
            "vesta" => chart.Points.Vesta,
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

    private static string ExpectedNightPartOfFortune(DateTimeOffset input, GeoLocation location)
    {
        using IEphemerides ephemerides = new SwissEphemeridesService(ephType: EphType.Moshier).CreateContext();
        var utcDateTime = input.ToUniversalTime().UtcDateTime;
        var houses = ephemerides.HouseCuspPositions(
            utcDateTime,
            location.Latitude,
            location.Longitude,
            HouseSystems.Placidus);
        var asc = houses.Cross[Cross.Asc];
        var sun = ephemerides.PlanetsPosition(Planets.Sun, utcDateTime).Longitude;
        var moon = ephemerides.PlanetsPosition(Planets.Moon, utcDateTime).Longitude;

        return SexagesimalDegreeFormatter.Format(NormalizeDegrees(asc + sun - moon));
    }

    private static string ExpectedLilith(DateTimeOffset input)
    {
        using var context = new EphemerisContextBuilder().Build();
        var julianDay = JulianDay.FromUtc(input.ToUniversalTime().UtcDateTime, CalendarSystem.Gregorian);
        var osculatingApogee = context.Bodies.ComputeUt(
            CelestialBody.OsculatingApogee,
            julianDay,
            EphemerisFlags.MoshierEph | EphemerisFlags.Speed);

        return SexagesimalDegreeFormatter.Format(
            EclipticLongitude(osculatingApogee.Position.X, osculatingApogee.Position.Y));
    }

    private static double EclipticLongitude(double x, double y)
    {
        return NormalizeDegrees(Math.Atan2(y, x) * 180.0 / Math.PI);
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private static int SabianDegreeFor(string degreeInSign)
    {
        return int.Parse(degreeInSign[..degreeInSign.IndexOf('°')]) + 1;
    }

    private static int SabianIndexFor(string sign, int degree)
    {
        var signIndex = sign switch
        {
            "Aries" => 0,
            "Taurus" => 1,
            "Gemini" => 2,
            "Cancer" => 3,
            "Leo" => 4,
            "Virgo" => 5,
            "Libra" => 6,
            "Scorpio" => 7,
            "Sagittarius" => 8,
            "Capricorn" => 9,
            "Aquarius" => 10,
            "Pisces" => 11,
            _ => throw new ArgumentOutOfRangeException(nameof(sign), sign, "Unknown zodiac sign.")
        };

        return (signIndex * 30) + degree;
    }

    private static string DispositorFor(string sign)
    {
        return sign switch
        {
            "Aries" => "mars",
            "Taurus" => "venus",
            "Gemini" => "mercury",
            "Cancer" => "moon",
            "Leo" => "sun",
            "Virgo" => "mercury",
            "Libra" => "venus",
            "Scorpio" => "pluto",
            "Sagittarius" => "jupiter",
            "Capricorn" => "saturn",
            "Aquarius" => "uranus",
            "Pisces" => "neptune",
            _ => throw new ArgumentOutOfRangeException(nameof(sign), sign, "Unknown zodiac sign.")
        };
    }

    private sealed record BodyCase(
        string JsonName,
        Planets Planet,
        string EclipticLongitude,
        string Sign,
        string DegreeInSign,
        int House);

    private sealed record AsteroidCase(
        string JsonName,
        string EclipticLongitude,
        string Sign,
        string DegreeInSign,
        int House);

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
