namespace AstroCli;

public sealed record ChartOutput(
    string InputDateTime,
    string UtcDateTime,
    string System,
    string Chart,
    LocationOutput Location,
    BodyPosition Ascendant,
    BodiesOutput Bodies);

public sealed record LocationOutput(
    string Latitude,
    string Longitude);

public sealed record BodiesOutput(
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

public sealed record BodyPosition(
    string Name,
    string EclipticLongitude,
    string Sign,
    string DegreeInSign);
