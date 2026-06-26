namespace AstroCli;

public sealed record ChartRequest(
    DateTimeOffset InputDateTime,
    GeoLocation Location,
    string System,
    string Chart);
