using System.Text.Json.Serialization;

namespace AstroCli;

public sealed record ChartOutput(
    string InputDateTime,
    string UtcDateTime,
    string System,
    string Chart,
    LocationOutput Location,
    string HouseSystem,
    ChartRulerOutput ChartRuler,
    CulminateOutput Culminate,
    HouseCuspsOutput HouseCusps,
    PointsOutput Points,
    SabianSymbolsOutput SabianSymbols,
    IReadOnlyList<DispositorGroupOutput> Dispositors,
    IReadOnlyList<AspectOutput> Aspects,
    IReadOnlyList<StelliumOutput> Stelliums,
    IReadOnlyList<ComplexAspectOutput> ComplexAspects);

public sealed record LocationOutput(
    string Latitude,
    string Longitude);

public sealed record HouseCuspsOutput(
    [property: JsonPropertyName("1")]
    PositionOutput House1,
    [property: JsonPropertyName("2")]
    PositionOutput House2,
    [property: JsonPropertyName("3")]
    PositionOutput House3,
    [property: JsonPropertyName("4")]
    PositionOutput House4,
    [property: JsonPropertyName("5")]
    PositionOutput House5,
    [property: JsonPropertyName("6")]
    PositionOutput House6,
    [property: JsonPropertyName("7")]
    PositionOutput House7,
    [property: JsonPropertyName("8")]
    PositionOutput House8,
    [property: JsonPropertyName("9")]
    PositionOutput House9,
    [property: JsonPropertyName("10")]
    PositionOutput House10,
    [property: JsonPropertyName("11")]
    PositionOutput House11,
    [property: JsonPropertyName("12")]
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
    int? House);

public sealed record PointOutput(
    string Type,
    string EclipticLongitude,
    string Sign,
    string DegreeInSign,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? House);

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

public sealed record ChartRulerOutput(
    string Sign,
    string Ruler);

public sealed record StelliumOutput(
    string Kind,
    string Name,
    IReadOnlyList<string> Points);

public sealed record ComplexAspectOutput(
    string Pattern,
    IReadOnlyList<string> Points,
    IReadOnlyList<AspectOutput> Aspects);

public sealed record CulminateOutput(
    string Planet,
    string DistanceFromMc);
