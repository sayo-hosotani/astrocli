namespace AstroCli;

public sealed record ChartPoint(
    string Key,
    string Type,
    double Longitude,
    int? House = null);
