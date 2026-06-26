namespace AstroCli;

public static class SexagesimalDegreeFormatter
{
    public static string Format(double degrees)
    {
        var totalSeconds = (int)Math.Round(degrees * 3600.0, MidpointRounding.AwayFromZero);
        var degreePart = totalSeconds / 3600;
        var remainingSeconds = totalSeconds % 3600;
        var minutePart = remainingSeconds / 60;
        var secondPart = remainingSeconds % 60;

        return $"{degreePart}°{minutePart:00}’{secondPart:00}″";
    }
}
