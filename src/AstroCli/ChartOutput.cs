using System.Text.Json.Serialization;

namespace AstroCli;

public sealed record ChartOutput(
    string InputDateTime,
    string UtcDateTime,
    string System,
    string Chart,
    LocationOutput Location,
    string HouseSystem,
    HouseCuspsOutput HouseCusps,
    PointsOutput Points,
    SabianSymbolsOutput SabianSymbols,
    IReadOnlyList<DispositorGroupOutput> Dispositors,
    IReadOnlyList<AspectOutput> Aspects,
    CulminatingPlanetOutput CulminatingPlanet);

public sealed record LocationOutput(
    string Latitude,
    string Longitude);

public sealed record HouseCuspsOutput(
    PositionOutput House1,
    PositionOutput House2,
    PositionOutput House3,
    PositionOutput House4,
    PositionOutput House5,
    PositionOutput House6,
    PositionOutput House7,
    PositionOutput House8,
    PositionOutput House9,
    PositionOutput House10,
    PositionOutput House11,
    PositionOutput House12);

public sealed record PointsOutput(
    PointOutput Sun,
    PointOutput Moon,
    PointOutput Mercury,
    PointOutput Venus,
    PointOutput Mars,
    PointOutput Jupiter,
    PointOutput Saturn,
    PointOutput Uranus,
    PointOutput Neptune,
    PointOutput Pluto,
    PointOutput Chiron,
    PointOutput Ceres,
    PointOutput Pallas,
    PointOutput Juno,
    PointOutput Vesta,
    PointOutput Asc,
    PointOutput Ic,
    PointOutput Dsc,
    PointOutput Mc,
    PointOutput NorthNode,
    PointOutput SouthNode,
    PointOutput PartOfFortune,
    PointOutput Vertex,
    PointOutput AntiVertex,
    PointOutput Lilith);

public sealed record SabianSymbolsOutput(
    SabianOutput House1,
    SabianOutput House2,
    SabianOutput House3,
    SabianOutput House4,
    SabianOutput House5,
    SabianOutput House6,
    SabianOutput House7,
    SabianOutput House8,
    SabianOutput House9,
    SabianOutput House10,
    SabianOutput House11,
    SabianOutput House12,
    SabianOutput Sun,
    SabianOutput Moon,
    SabianOutput Mercury,
    SabianOutput Venus,
    SabianOutput Mars,
    SabianOutput Jupiter,
    SabianOutput Saturn,
    SabianOutput Uranus,
    SabianOutput Neptune,
    SabianOutput Pluto,
    SabianOutput Chiron,
    SabianOutput Ceres,
    SabianOutput Pallas,
    SabianOutput Juno,
    SabianOutput Vesta,
    SabianOutput Asc,
    SabianOutput Ic,
    SabianOutput Dsc,
    SabianOutput Mc,
    SabianOutput NorthNode,
    SabianOutput SouthNode,
    SabianOutput PartOfFortune,
    SabianOutput Vertex,
    SabianOutput AntiVertex,
    SabianOutput Lilith);

public sealed record PositionOutput(
    string EclipticLongitude,
    string Sign,
    string DegreeInSign,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? House);

public sealed record PointOutput(
    string Type,
    string EclipticLongitude,
    string Sign,
    string DegreeInSign,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? House);

public sealed record DispositorGroupOutput(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Root,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? Loop,
    IReadOnlyList<DispositorPairOutput> Dispositors);

public sealed record DispositorPairOutput(
    string Point,
    string Dispositor);

public sealed record SabianOutput(
    int Index,
    string Symbol);

public sealed record AspectOutput(
    IReadOnlyList<string> Points,
    string Aspect,
    string Orb);

public sealed record CulminatingPlanetOutput(
    string Planet,
    string DistanceFromMc);
