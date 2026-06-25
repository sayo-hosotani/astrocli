namespace AstroCli;

public sealed record ChartOutput(
    string InputDateTime,
    string UtcDateTime,
    string System,
    string Chart,
    BodiesOutput Bodies);

public sealed record BodiesOutput(
    BodyPosition Sun,
    BodyPosition Moon);

public sealed record BodyPosition(
    string Name,
    double EclipticLongitude,
    string Sign,
    double DegreeInSign);
