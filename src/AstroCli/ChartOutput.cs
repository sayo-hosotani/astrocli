using System.Text.Json.Serialization;

namespace AstroCli;

public sealed record ChartOutput(
    string InputDateTime,
    string UtcDateTime,
    string System,
    string Chart,
    LocationOutput Location,
    HousesOutput Houses,
    PlanetsOutput Planets,
    AsteroidsOutput Asteroids,
    AnglesOutput Angles,
    ObjectsOutput Objects);

public sealed record LocationOutput(
    string Latitude,
    string Longitude);

public sealed record HousesOutput(
    string System,
    HouseCuspsOutput Cusps);

public sealed record HouseCuspsOutput(
    BodyPosition House1,
    BodyPosition House2,
    BodyPosition House3,
    BodyPosition House4,
    BodyPosition House5,
    BodyPosition House6,
    BodyPosition House7,
    BodyPosition House8,
    BodyPosition House9,
    BodyPosition House10,
    BodyPosition House11,
    BodyPosition House12);

public sealed record PlanetsOutput(
    BodyPosition Sun,
    BodyPosition Moon,
    BodyPosition Mercury,
    BodyPosition Venus,
    BodyPosition Mars,
    BodyPosition Jupiter,
    BodyPosition Saturn,
    BodyPosition Uranus,
    BodyPosition Neptune,
    BodyPosition Pluto);

public sealed record AsteroidsOutput(
    BodyPosition Chiron,
    BodyPosition Ceres,
    BodyPosition Pallas,
    BodyPosition Juno,
    BodyPosition Vesta);

public sealed record AnglesOutput(
    BodyPosition Asc,
    BodyPosition Ic,
    BodyPosition Dsc,
    BodyPosition Mc);

public sealed record ObjectsOutput(
    BodyPosition NorthNode,
    BodyPosition SouthNode,
    BodyPosition PartOfFortune,
    BodyPosition Vertex,
    BodyPosition AntiVertex,
    BodyPosition Lilith);

public sealed record BodyPosition(
    string Name,
    string EclipticLongitude,
    string Sign,
    string DegreeInSign,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? House = null);
