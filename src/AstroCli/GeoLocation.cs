using System.Globalization;
using System.Text.RegularExpressions;

namespace AstroCli;

public sealed record GeoLocation(double Latitude, double Longitude)
{
    private static readonly Regex CoordinatePattern = new(
        @"^(?<degrees>\d+)°(?<minutes>\d{1,2})[’'](?<seconds>\d{1,2}(?:\.\d+)?)[″""](?<direction>[NSEW])$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool TryParse(string value, out GeoLocation? location)
    {
        location = null;

        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseCoordinate(parts[0], 'N', 'S', 90.0, out var latitude))
        {
            return false;
        }

        if (!TryParseCoordinate(parts[1], 'E', 'W', 180.0, out var longitude))
        {
            return false;
        }

        location = new GeoLocation(latitude, longitude);
        return true;
    }

    private static bool TryParseCoordinate(
        string value,
        char positiveDirection,
        char negativeDirection,
        double maxAbsoluteValue,
        out double coordinate)
    {
        coordinate = 0;

        var match = CoordinatePattern.Match(value);
        if (!match.Success)
        {
            return false;
        }

        var direction = char.ToUpperInvariant(match.Groups["direction"].Value[0]);
        if (direction != positiveDirection && direction != negativeDirection)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["degrees"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var degrees))
        {
            return false;
        }

        if (!int.TryParse(match.Groups["minutes"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes))
        {
            return false;
        }

        if (!double.TryParse(match.Groups["seconds"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds))
        {
            return false;
        }

        if (minutes is < 0 or >= 60 || seconds is < 0 or >= 60)
        {
            return false;
        }

        var absoluteValue = degrees + (minutes / 60.0) + (seconds / 3600.0);
        if (absoluteValue > maxAbsoluteValue)
        {
            return false;
        }

        coordinate = direction == positiveDirection ? absoluteValue : -absoluteValue;
        return true;
    }

    public string FormatLatitude()
    {
        return FormatCoordinate(Latitude, 'N', 'S');
    }

    public string FormatLongitude()
    {
        return FormatCoordinate(Longitude, 'E', 'W');
    }

    private static string FormatCoordinate(double coordinate, char positiveDirection, char negativeDirection)
    {
        var direction = coordinate < 0 ? negativeDirection : positiveDirection;
        return $"{SexagesimalDegreeFormatter.Format(Math.Abs(coordinate))}{direction}";
    }
}
