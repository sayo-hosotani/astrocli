using CosineKitty;
using SharpAstrology.DataModels;
using SharpAstrology.Enums;
using SharpAstrology.Ephemerides;
using SharpAstrology.Interfaces;
using SharpAstrology.SwissEphemerides;
using SharpAstrology.SwissEphemerides.Application.Bodies;
using SharpAstrology.SwissEphemerides.Domain.Time;

namespace AstroCli;

public static class NatalChartCalculator
{
    private const double AspectOrbDegrees = 6.0;

    private static readonly BodyDefinition[] PlanetDefinitions =
    [
        new("sun", Planets.Sun),
        new("moon", Planets.Moon),
        new("mercury", Planets.Mercury),
        new("venus", Planets.Venus),
        new("mars", Planets.Mars),
        new("jupiter", Planets.Jupiter),
        new("saturn", Planets.Saturn),
        new("uranus", Planets.Uranus),
        new("neptune", Planets.Neptune),
        new("pluto", Planets.Pluto)
    ];

    private static readonly BodyDefinition[] ObjectDefinitions =
    [
        new("northNode", Planets.NorthNode),
        new("southNode", Planets.SouthNode)
    ];

    private static readonly AspectDefinition[] AspectDefinitions =
    [
        new("conjunction", 0.0),
        new("sextile", 60.0),
        new("square", 90.0),
        new("trine", 120.0),
        new("opposition", 180.0)
    ];

    public static ChartOutput Calculate(ChartRequest request, IHorizonsClient? horizonsClient = null)
    {
        return CalculateAsync(request, horizonsClient).GetAwaiter().GetResult();
    }

    public static async Task<ChartOutput> CalculateAsync(
        ChartRequest request,
        IHorizonsClient? horizonsClient = null,
        CancellationToken cancellationToken = default)
    {
        var utcDateTime = request.InputDateTime.ToUniversalTime();
        using IEphemerides ephemerides = new SwissEphemeridesService(ephType: EphType.Moshier).CreateContext();
        var housePositions = ephemerides.HouseCuspPositions(
            utcDateTime.UtcDateTime,
            request.Location.Latitude,
            request.Location.Longitude,
            HouseSystems.Placidus);
        var houseCusps = HouseCuspsFor(housePositions);

        var planetPoints = CalculatePlanetPoints(ephemerides, utcDateTime.UtcDateTime, houseCusps);
        var asteroidPoints = await new AsteroidBodyPositionCalculator(horizonsClient ?? new HorizonsClient())
            .CalculateAsync(request.InputDateTime, longitude => HouseForLongitude(houseCusps, longitude), cancellationToken)
            .ConfigureAwait(false);
        var anglePoints = CalculateAnglePoints(housePositions);
        var objectPoints = CalculateObjectPoints(
            ephemerides,
            housePositions,
            utcDateTime.UtcDateTime,
            request.Location,
            houseCusps);
        var housePoints = CalculateHousePoints(housePositions);
        var points = planetPoints.Concat(asteroidPoints).Concat(anglePoints).Concat(objectPoints).ToArray();
        var aspectPoints = planetPoints.Concat(asteroidPoints).Concat(objectPoints).ToArray();

        return new ChartOutput(
            FormatInputDateTime(request.InputDateTime),
            FormatUtcDateTime(utcDateTime),
            request.System,
            request.Chart,
            new LocationOutput(request.Location.FormatLatitude(), request.Location.FormatLongitude()),
            CreateHousesOutput(housePoints),
            CreatePointsOutput(points),
            CreateSabianSymbolsOutput(housePoints.Concat(points).ToArray()),
            CalculateAspects(aspectPoints),
            CalculateCulminatingPlanet(planetPoints, anglePoints.Single(point => point.Key == "mc")));
    }

    private static IReadOnlyList<ChartPoint> CalculatePlanetPoints(
        IEphemerides ephemerides,
        DateTime utcDateTime,
        IReadOnlyList<HouseCusp> houseCusps)
    {
        return PlanetDefinitions
            .Select(body => CalculateBodyPoint(ephemerides, body, "planet", utcDateTime, houseCusps))
            .ToArray();
    }

    private static IReadOnlyList<ChartPoint> CalculateObjectPoints(
        IEphemerides ephemerides,
        HousePosition housePositions,
        DateTime utcDateTime,
        GeoLocation location,
        IReadOnlyList<HouseCusp> houseCusps)
    {
        return
        [
            CalculateBodyPoint(ephemerides, ObjectDefinitions[0], "object", utcDateTime, houseCusps),
            CalculateBodyPoint(ephemerides, ObjectDefinitions[1], "object", utcDateTime, houseCusps),
            CalculatePartOfFortune(ephemerides, housePositions, utcDateTime, location, houseCusps),
            CreatePoint("vertex", "object", housePositions.Cross[Cross.Vertex], HouseForLongitude(houseCusps, housePositions.Cross[Cross.Vertex])),
            CreatePoint("antiVertex", "object", housePositions.Cross[Cross.Vertex] + 180.0, HouseForLongitude(houseCusps, housePositions.Cross[Cross.Vertex] + 180.0)),
            CalculateLilith(utcDateTime, houseCusps)
        ];
    }

    private static IReadOnlyList<ChartPoint> CalculateAnglePoints(HousePosition housePositions)
    {
        return
        [
            CreatePoint("asc", "angle", housePositions.Cross[Cross.Asc]),
            CreatePoint("ic", "angle", housePositions.Cross[Cross.Ic]),
            CreatePoint("dsc", "angle", housePositions.Cross[Cross.Dc]),
            CreatePoint("mc", "angle", housePositions.Cross[Cross.Mc])
        ];
    }

    private static IReadOnlyList<ChartPoint> CalculateHousePoints(HousePosition housePositions)
    {
        return
        [
            CreatePoint("house1", "house", housePositions.HouseCusps[Houses.House1]),
            CreatePoint("house2", "house", housePositions.HouseCusps[Houses.House2]),
            CreatePoint("house3", "house", housePositions.HouseCusps[Houses.House3]),
            CreatePoint("house4", "house", housePositions.HouseCusps[Houses.House4]),
            CreatePoint("house5", "house", housePositions.HouseCusps[Houses.House5]),
            CreatePoint("house6", "house", housePositions.HouseCusps[Houses.House6]),
            CreatePoint("house7", "house", housePositions.HouseCusps[Houses.House7]),
            CreatePoint("house8", "house", housePositions.HouseCusps[Houses.House8]),
            CreatePoint("house9", "house", housePositions.HouseCusps[Houses.House9]),
            CreatePoint("house10", "house", housePositions.HouseCusps[Houses.House10]),
            CreatePoint("house11", "house", housePositions.HouseCusps[Houses.House11]),
            CreatePoint("house12", "house", housePositions.HouseCusps[Houses.House12])
        ];
    }

    private static PointsOutput CreatePointsOutput(IReadOnlyList<ChartPoint> points)
    {
        var byKey = points.ToDictionary(point => point.Key);

        return new PointsOutput(
            CreatePointOutput(byKey["sun"]),
            CreatePointOutput(byKey["moon"]),
            CreatePointOutput(byKey["mercury"]),
            CreatePointOutput(byKey["venus"]),
            CreatePointOutput(byKey["mars"]),
            CreatePointOutput(byKey["jupiter"]),
            CreatePointOutput(byKey["saturn"]),
            CreatePointOutput(byKey["uranus"]),
            CreatePointOutput(byKey["neptune"]),
            CreatePointOutput(byKey["pluto"]),
            CreatePointOutput(byKey["chiron"]),
            CreatePointOutput(byKey["ceres"]),
            CreatePointOutput(byKey["pallas"]),
            CreatePointOutput(byKey["juno"]),
            CreatePointOutput(byKey["vesta"]),
            CreatePointOutput(byKey["asc"]),
            CreatePointOutput(byKey["ic"]),
            CreatePointOutput(byKey["dsc"]),
            CreatePointOutput(byKey["mc"]),
            CreatePointOutput(byKey["northNode"]),
            CreatePointOutput(byKey["southNode"]),
            CreatePointOutput(byKey["partOfFortune"]),
            CreatePointOutput(byKey["vertex"]),
            CreatePointOutput(byKey["antiVertex"]),
            CreatePointOutput(byKey["lilith"]));
    }

    private static SabianSymbolsOutput CreateSabianSymbolsOutput(IReadOnlyList<ChartPoint> points)
    {
        var byKey = points.ToDictionary(point => point.Key, point => SabianFor(point.Longitude));

        return new SabianSymbolsOutput(
            byKey["house1"],
            byKey["house2"],
            byKey["house3"],
            byKey["house4"],
            byKey["house5"],
            byKey["house6"],
            byKey["house7"],
            byKey["house8"],
            byKey["house9"],
            byKey["house10"],
            byKey["house11"],
            byKey["house12"],
            byKey["sun"],
            byKey["moon"],
            byKey["mercury"],
            byKey["venus"],
            byKey["mars"],
            byKey["jupiter"],
            byKey["saturn"],
            byKey["uranus"],
            byKey["neptune"],
            byKey["pluto"],
            byKey["chiron"],
            byKey["ceres"],
            byKey["pallas"],
            byKey["juno"],
            byKey["vesta"],
            byKey["asc"],
            byKey["ic"],
            byKey["dsc"],
            byKey["mc"],
            byKey["northNode"],
            byKey["southNode"],
            byKey["partOfFortune"],
            byKey["vertex"],
            byKey["antiVertex"],
            byKey["lilith"]);
    }

    private static HousesOutput CreateHousesOutput(IReadOnlyList<ChartPoint> housePoints)
    {
        var byKey = housePoints.ToDictionary(point => point.Key);

        return new HousesOutput(
            "placidus",
            new HouseCuspsOutput(
                CreatePositionOutput(byKey["house1"]),
                CreatePositionOutput(byKey["house2"]),
                CreatePositionOutput(byKey["house3"]),
                CreatePositionOutput(byKey["house4"]),
                CreatePositionOutput(byKey["house5"]),
                CreatePositionOutput(byKey["house6"]),
                CreatePositionOutput(byKey["house7"]),
                CreatePositionOutput(byKey["house8"]),
                CreatePositionOutput(byKey["house9"]),
                CreatePositionOutput(byKey["house10"]),
                CreatePositionOutput(byKey["house11"]),
                CreatePositionOutput(byKey["house12"])));
    }

    private static ChartPoint CalculateBodyPoint(
        IEphemerides ephemerides,
        BodyDefinition body,
        string type,
        DateTime utcDateTime,
        IReadOnlyList<HouseCusp> houseCusps)
    {
        var position = ephemerides.PlanetsPosition(body.Planet, utcDateTime);
        return CreatePoint(body.Key, type, position.Longitude, HouseForLongitude(houseCusps, position.Longitude));
    }

    private static ChartPoint CalculatePartOfFortune(
        IEphemerides ephemerides,
        HousePosition housePositions,
        DateTime utcDateTime,
        GeoLocation location,
        IReadOnlyList<HouseCusp> houseCusps)
    {
        var asc = NormalizeDegrees(housePositions.Cross[Cross.Asc]);
        var sun = NormalizeDegrees(ephemerides.PlanetsPosition(Planets.Sun, utcDateTime).Longitude);
        var moon = NormalizeDegrees(ephemerides.PlanetsPosition(Planets.Moon, utcDateTime).Longitude);
        var isDayChart = IsSunAboveHorizon(utcDateTime, location);
        var longitude = isDayChart ? asc + moon - sun : asc + sun - moon;

        return CreatePoint("partOfFortune", "object", longitude, HouseForLongitude(houseCusps, longitude));
    }

    private static ChartPoint CalculateLilith(DateTime utcDateTime, IReadOnlyList<HouseCusp> houseCusps)
    {
        using var context = new EphemerisContextBuilder().Build();
        var julianDay = JulianDay.FromUtc(utcDateTime, CalendarSystem.Gregorian);
        var osculatingApogee = context.Bodies.ComputeUt(
            CelestialBody.OsculatingApogee,
            julianDay,
            EphemerisFlags.MoshierEph | EphemerisFlags.Speed);
        var longitude = EclipticLongitude(osculatingApogee.Position.X, osculatingApogee.Position.Y);

        return CreatePoint("lilith", "object", longitude, HouseForLongitude(houseCusps, longitude));
    }

    private static IReadOnlyList<AspectOutput> CalculateAspects(IReadOnlyList<ChartPoint> points)
    {
        var aspects = new List<AspectOutput>();

        for (var left = 0; left < points.Count; left++)
        {
            for (var right = left + 1; right < points.Count; right++)
            {
                var angle = AngularDistance(points[left].Longitude, points[right].Longitude);
                var aspect = AspectDefinitions
                    .Select(definition => new
                    {
                        Definition = definition,
                        Orb = Math.Abs(angle - definition.Angle)
                    })
                    .Where(candidate => candidate.Orb <= AspectOrbDegrees)
                    .OrderBy(candidate => candidate.Orb)
                    .FirstOrDefault();

                if (aspect is null)
                {
                    continue;
                }

                aspects.Add(new AspectOutput(
                    [points[left].Key, points[right].Key],
                    aspect.Definition.Name,
                    SexagesimalDegreeFormatter.Format(aspect.Orb)));
            }
        }

        return aspects;
    }

    private static CulminatingPlanetOutput CalculateCulminatingPlanet(
        IReadOnlyList<ChartPoint> planetPoints,
        ChartPoint mc)
    {
        var culminating = planetPoints
            .Select(point => new
            {
                Point = point,
                Distance = NormalizeDegrees(point.Longitude - mc.Longitude)
            })
            .OrderBy(candidate => candidate.Distance)
            .First();

        return new CulminatingPlanetOutput(
            culminating.Point.Key,
            SexagesimalDegreeFormatter.Format(culminating.Distance));
    }

    private static PositionOutput CreatePositionOutput(ChartPoint point)
    {
        var longitude = NormalizeDegrees(point.Longitude);
        var sign = Zodiac.SignForLongitude(longitude);

        return new PositionOutput(
            SexagesimalDegreeFormatter.Format(longitude),
            sign.Name,
            SexagesimalDegreeFormatter.Format(sign.DegreeInSign),
            point.House,
            new DispositorOutput(DispositorFor(sign.Name)));
    }

    private static PointOutput CreatePointOutput(ChartPoint point)
    {
        var longitude = NormalizeDegrees(point.Longitude);
        var sign = Zodiac.SignForLongitude(longitude);

        return new PointOutput(
            point.Type,
            SexagesimalDegreeFormatter.Format(longitude),
            sign.Name,
            SexagesimalDegreeFormatter.Format(sign.DegreeInSign),
            point.House,
            new DispositorOutput(DispositorFor(sign.Name)));
    }

    private static SabianOutput SabianFor(double longitude)
    {
        longitude = NormalizeDegrees(longitude);
        var roundedSeconds = (int)Math.Round(longitude * 3600.0, MidpointRounding.AwayFromZero) % (360 * 3600);
        var roundedLongitude = roundedSeconds / 3600.0;
        var sign = Zodiac.SignForLongitude(roundedLongitude);
        var signIndex = Math.Min(roundedSeconds / (30 * 3600), 11);
        var degree = ((roundedSeconds - (signIndex * 30 * 3600)) / 3600) + 1;

        return new SabianOutput(
            (signIndex * 30) + degree,
            SabianSymbols.SymbolForIndex((signIndex * 30) + degree));
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

    private static bool IsSunAboveHorizon(DateTime utcDateTime, GeoLocation location)
    {
        var time = new AstroTime(utcDateTime);
        var observer = new Observer(location.Latitude, location.Longitude, 0.0);
        var sun = Astronomy.Equator(
            CosineKitty.Body.Sun,
            time,
            observer,
            EquatorEpoch.OfDate,
            Aberration.Corrected);
        var horizon = Astronomy.Horizon(time, observer, sun.ra, sun.dec, Refraction.None);

        return horizon.altitude >= 0.0;
    }

    private static double EclipticLongitude(double x, double y)
    {
        return NormalizeDegrees(Math.Atan2(y, x) * 180.0 / Math.PI);
    }

    private static ChartPoint CreatePoint(string key, string type, double longitude, string? house = null)
    {
        longitude = NormalizeDegrees(longitude);

        return new ChartPoint(key, type, longitude, house);
    }

    private static IReadOnlyList<HouseCusp> HouseCuspsFor(HousePosition housePositions)
    {
        return
        [
            new("house1", housePositions.HouseCusps[Houses.House1]),
            new("house2", housePositions.HouseCusps[Houses.House2]),
            new("house3", housePositions.HouseCusps[Houses.House3]),
            new("house4", housePositions.HouseCusps[Houses.House4]),
            new("house5", housePositions.HouseCusps[Houses.House5]),
            new("house6", housePositions.HouseCusps[Houses.House6]),
            new("house7", housePositions.HouseCusps[Houses.House7]),
            new("house8", housePositions.HouseCusps[Houses.House8]),
            new("house9", housePositions.HouseCusps[Houses.House9]),
            new("house10", housePositions.HouseCusps[Houses.House10]),
            new("house11", housePositions.HouseCusps[Houses.House11]),
            new("house12", housePositions.HouseCusps[Houses.House12])
        ];
    }

    private static string HouseForLongitude(IReadOnlyList<HouseCusp> houseCusps, double longitude)
    {
        longitude = NormalizeDegrees(longitude);

        for (var index = 0; index < houseCusps.Count; index++)
        {
            var current = NormalizeDegrees(houseCusps[index].Longitude);
            var next = NormalizeDegrees(houseCusps[(index + 1) % houseCusps.Count].Longitude);
            var distanceToLongitude = NormalizeDegrees(longitude - current);
            var distanceToNextCusp = NormalizeDegrees(next - current);

            if (distanceToLongitude < distanceToNextCusp)
            {
                return houseCusps[index].Name;
            }
        }

        return houseCusps[^1].Name;
    }

    private static double AngularDistance(double left, double right)
    {
        var distance = Math.Abs(NormalizeDegrees(left) - NormalizeDegrees(right));
        return distance > 180.0 ? 360.0 - distance : distance;
    }

    private static string FormatInputDateTime(DateTimeOffset dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss zzz");
    }

    private static string FormatUtcDateTime(DateTimeOffset dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private sealed record BodyDefinition(string Key, Planets Planet);

    private sealed record HouseCusp(string Name, double Longitude);

    private sealed record AspectDefinition(string Name, double Angle);
}
