namespace AstroCli;

public static class Zodiac
{
    private static readonly string[] Signs =
    [
        "Aries",
        "Taurus",
        "Gemini",
        "Cancer",
        "Leo",
        "Virgo",
        "Libra",
        "Scorpio",
        "Sagittarius",
        "Capricorn",
        "Aquarius",
        "Pisces"
    ];

    public static ZodiacPosition SignForLongitude(double eclipticLongitude)
    {
        if (eclipticLongitude < 0 || eclipticLongitude >= 360)
        {
            throw new ArgumentOutOfRangeException(nameof(eclipticLongitude), "Longitude must be in the range [0, 360).");
        }

        var signIndex = Math.Min((int)(eclipticLongitude / 30.0), Signs.Length - 1);
        return new ZodiacPosition(Signs[signIndex], eclipticLongitude - (signIndex * 30.0));
    }
}

public sealed record ZodiacPosition(string Name, double DegreeInSign);
