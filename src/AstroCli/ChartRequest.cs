namespace AstroCli;

public sealed record ChartRequest(
    DateTimeOffset InputDateTime,
    string System,
    string Chart);
