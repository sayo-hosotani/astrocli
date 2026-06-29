namespace AstroCli;

public static class KnownAsteroids
{
    public static readonly IReadOnlyList<AsteroidTarget> FixedTargets =
    [
        new("キロン", "chiron", "2060;"),
        new("セレス", "ceres", "1;"),
        new("パラス", "pallas", "2;"),
        new("ジュノー", "juno", "3;"),
        new("ベスタ", "vesta", "4;")
    ];
}
