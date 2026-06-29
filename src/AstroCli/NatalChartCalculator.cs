using SharpAstrology.DataModels;
using SharpAstrology.Enums;
using SharpAstrology.Ephemerides;
using SharpAstrology.Interfaces;

namespace AstroCli;

public static class NatalChartCalculator
{
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
        var asteroidPositions = await new AsteroidBodyPositionCalculator(horizonsClient ?? new HorizonsClient())
            .CalculateAsync(request.InputDateTime, cancellationToken)
            .ConfigureAwait(false);

        return new ChartOutput(
            FormatInputDateTime(request.InputDateTime),
            FormatUtcDateTime(utcDateTime),
            request.System,
            request.Chart,
            new LocationOutput(request.Location.FormatLatitude(), request.Location.FormatLongitude()),
            CalculateHouses(housePositions),
            new PlanetsOutput(
                CalculateBody(ephemerides, PlanetDefinitions[0], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[1], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[2], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[3], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[4], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[5], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[6], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[7], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[8], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, PlanetDefinitions[9], utcDateTime.UtcDateTime)),
            new AsteroidsOutput(
                asteroidPositions[0],
                asteroidPositions[1],
                asteroidPositions[2],
                asteroidPositions[3],
                asteroidPositions[4]),
            CalculateAngles(housePositions),
            new ObjectsOutput(
                CalculateBody(ephemerides, ObjectDefinitions[0], utcDateTime.UtcDateTime),
                CalculateBody(ephemerides, ObjectDefinitions[1], utcDateTime.UtcDateTime)));
    }

    private static AnglesOutput CalculateAngles(HousePosition housePositions)
    {
        return new AnglesOutput(
            CreatePosition("asc", housePositions.Cross[Cross.Asc]),
            CreatePosition("ic", housePositions.Cross[Cross.Ic]),
            CreatePosition("dsc", housePositions.Cross[Cross.Dc]),
            CreatePosition("mc", housePositions.Cross[Cross.Mc]));
    }

    private static HousesOutput CalculateHouses(HousePosition housePositions)
    {
        return new HousesOutput(
            "placidus",
            new HouseCuspsOutput(
                CreatePosition("house1", housePositions.HouseCusps[Houses.House1]),
                CreatePosition("house2", housePositions.HouseCusps[Houses.House2]),
                CreatePosition("house3", housePositions.HouseCusps[Houses.House3]),
                CreatePosition("house4", housePositions.HouseCusps[Houses.House4]),
                CreatePosition("house5", housePositions.HouseCusps[Houses.House5]),
                CreatePosition("house6", housePositions.HouseCusps[Houses.House6]),
                CreatePosition("house7", housePositions.HouseCusps[Houses.House7]),
                CreatePosition("house8", housePositions.HouseCusps[Houses.House8]),
                CreatePosition("house9", housePositions.HouseCusps[Houses.House9]),
                CreatePosition("house10", housePositions.HouseCusps[Houses.House10]),
                CreatePosition("house11", housePositions.HouseCusps[Houses.House11]),
                CreatePosition("house12", housePositions.HouseCusps[Houses.House12])));
    }

    private static BodyPosition CalculateBody(IEphemerides ephemerides, BodyDefinition body, DateTime utcDateTime)
    {
        var position = ephemerides.PlanetsPosition(body.Planet, utcDateTime);
        return CreatePosition(body.Name, position.Longitude);
    }

    private static BodyPosition CreatePosition(string name, double longitude)
    {
        longitude = NormalizeDegrees(longitude);
        var sign = Zodiac.SignForLongitude(longitude);

        return new BodyPosition(
            name,
            SexagesimalDegreeFormatter.Format(longitude),
            sign.Name,
            SexagesimalDegreeFormatter.Format(sign.DegreeInSign));
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

    private sealed record BodyDefinition(string Name, Planets Planet);
}
