using CosineKitty;

namespace AstroCli;

public static class NatalChartCalculator
{
    public static ChartOutput Calculate(ChartRequest request)
    {
        var utcDateTime = request.InputDateTime.ToUniversalTime();
        var astroTime = new AstroTime(utcDateTime.UtcDateTime);

        return new ChartOutput(
            FormatInputDateTime(request.InputDateTime),
            FormatUtcDateTime(utcDateTime),
            request.System,
            request.Chart,
            new BodiesOutput(
                CalculateBody("sun", Body.Sun, astroTime),
                CalculateBody("moon", Body.Moon, astroTime)));
    }

    private static BodyPosition CalculateBody(string name, Body body, AstroTime time)
    {
        var geoVector = Astronomy.GeoVector(body, time, Aberration.Corrected);
        var ecliptic = Astronomy.EquatorialToEcliptic(geoVector);
        var longitude = NormalizeDegrees(ecliptic.elon);
        var sign = Zodiac.SignForLongitude(longitude);

        return new BodyPosition(
            name,
            RoundDegrees(longitude),
            sign.Name,
            RoundDegrees(sign.DegreeInSign));
    }

    private static string FormatInputDateTime(DateTimeOffset dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss zzz");
    }

    private static string FormatUtcDateTime(DateTimeOffset dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private static double RoundDegrees(double value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
