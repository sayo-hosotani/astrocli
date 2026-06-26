using CosineKitty;

namespace AstroCli;

public static class NatalChartCalculator
{
    private const double EclipticObliquityDegrees = 23.4392911;

    public static ChartOutput Calculate(ChartRequest request)
    {
        var utcDateTime = request.InputDateTime.ToUniversalTime();
        var astroTime = new AstroTime(utcDateTime.UtcDateTime);
        var ascendant = CalculateAscendant(request.Location, astroTime);

        return new ChartOutput(
            FormatInputDateTime(request.InputDateTime),
            FormatUtcDateTime(utcDateTime),
            request.System,
            request.Chart,
            new LocationOutput(request.Location.FormatLatitude(), request.Location.FormatLongitude()),
            ascendant,
            new BodiesOutput(
                CalculateBody("sun", Body.Sun, astroTime),
                CalculateBody("moon", Body.Moon, astroTime),
                CalculateBody("mercury", Body.Mercury, astroTime),
                CalculateBody("venus", Body.Venus, astroTime),
                CalculateBody("mars", Body.Mars, astroTime),
                CalculateBody("jupiter", Body.Jupiter, astroTime),
                CalculateBody("saturn", Body.Saturn, astroTime),
                CalculateBody("uranus", Body.Uranus, astroTime),
                CalculateBody("neptune", Body.Neptune, astroTime),
                CalculateBody("pluto", Body.Pluto, astroTime)));
    }

    private static BodyPosition CalculateBody(string name, Body body, AstroTime time)
    {
        var geoVector = Astronomy.GeoVector(body, time, Aberration.Corrected);
        var ecliptic = Astronomy.EquatorialToEcliptic(geoVector);
        var longitude = NormalizeDegrees(ecliptic.elon);
        var sign = Zodiac.SignForLongitude(longitude);

        return new BodyPosition(
            name,
            SexagesimalDegreeFormatter.Format(longitude),
            sign.Name,
            SexagesimalDegreeFormatter.Format(sign.DegreeInSign));
    }

    private static BodyPosition CalculateAscendant(GeoLocation location, AstroTime time)
    {
        var greenwichSiderealDegrees = Astronomy.SiderealTime(time) * 15.0;
        var localSiderealDegrees = NormalizeDegrees(greenwichSiderealDegrees + location.Longitude);
        var localSiderealRadians = DegreesToRadians(localSiderealDegrees);
        var latitudeRadians = DegreesToRadians(location.Latitude);
        var obliquityRadians = DegreesToRadians(EclipticObliquityDegrees);

        var y = -Math.Cos(localSiderealRadians);
        var x = (Math.Sin(localSiderealRadians) * Math.Cos(obliquityRadians))
            + (Math.Tan(latitudeRadians) * Math.Sin(obliquityRadians));
        var longitude = NormalizeDegrees(RadiansToDegrees(Math.Atan2(y, x)));
        var sign = Zodiac.SignForLongitude(longitude);

        return new BodyPosition(
            "ascendant",
            SexagesimalDegreeFormatter.Format(longitude),
            sign.Name,
            SexagesimalDegreeFormatter.Format(sign.DegreeInSign));
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

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }
}
