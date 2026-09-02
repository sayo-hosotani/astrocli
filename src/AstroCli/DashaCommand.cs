using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AstroCli;

public static class DashaCommand
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            error.WriteLine("Invalid arguments.");
            error.WriteLine("Usage: astrocli dasha <input-or-glob> [<input-or-glob> ...]");
            return 1;
        }

        var files = new SortedSet<string>(StringComparer.Ordinal);
        var missingFiles = new List<string>();
        foreach (var argument in args)
        {
            if (argument.Contains('*') || argument.Contains('?'))
            {
                foreach (var file in Expand(argument)) files.Add(file);
            }
            else if (File.Exists(argument))
            {
                files.Add(Path.GetFullPath(argument));
            }
            else
            {
                missingFiles.Add(argument);
            }
        }

        files.RemoveWhere(file => file.EndsWith("_dasha.json", StringComparison.OrdinalIgnoreCase));
        var inputFiles = files.OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal).ThenBy(file => file, StringComparer.Ordinal).ToList();
        var successes = 0;
        var warningFiles = 0;
        var errorFiles = missingFiles.Count;
        foreach (var missingFile in missingFiles)
            error.WriteLine($"ERROR: {missingFile}: file not found");
        foreach (var file in inputFiles)
        {
            var destination = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + "_dasha.json");
            try
            {
                if (File.Exists(destination)) File.Delete(destination);
                var result = DashaCalculator.Calculate(file);
                File.WriteAllText(destination, JsonSerializer.Serialize(result, DashaJsonOptions.Default) + Environment.NewLine);
                if (result.Warnings is null) successes++; else warningFiles++;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidDataException or JsonException or IOException or InvalidOperationException)
            {
                errorFiles++;
                error.WriteLine($"ERROR: {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        output.Write($"Processed: {inputFiles.Count + missingFiles.Count}");
        if (successes > 0) output.Write($" | Success: {successes}");
        if (warningFiles > 0) output.Write($" | Warning: {warningFiles}");
        if (errorFiles > 0) output.Write($" | Error: {errorFiles}");
        output.WriteLine();
        return errorFiles == 0 ? 0 : 1;
    }

    private static IEnumerable<string> Expand(string pattern)
    {
        var directoryPart = Path.GetDirectoryName(pattern);
        var directory = string.IsNullOrEmpty(directoryPart) ? Directory.GetCurrentDirectory() : Path.GetFullPath(directoryPart);
        if (!Directory.Exists(directory)) yield break;
        foreach (var file in Directory.GetFiles(directory, Path.GetFileName(pattern), SearchOption.TopDirectoryOnly))
            yield return Path.GetFullPath(file);
    }
}

public static class DashaCalculator
{
    private const double DashaYearDays = 365.25;
    private const double CycleDays = 43830;
    private const double NakshatraSize = 360.0 / 27.0;
    private static readonly string[] Lords = ["Ketu", "Venus", "Sun", "Moon", "Mars", "Rahu", "Jupiter", "Saturn", "Mercury"];
    private static readonly double[] LordYears = [7, 20, 6, 10, 7, 18, 16, 19, 17];
    private static readonly string[] NakshatraNames = ["Ashvinī", "Bharanī", "Kṛttikā", "Rohinī", "Mrigashīra", "Ārdrā", "Punarvasu", "Pushya", "Ashlesha", "Maghā", "Pūrva Phalgunī", "Uttara Phalgunī", "Hasta", "Chitrā", "Svātī", "Vishākhā", "Anurādhā", "Jyeshtha", "Mūla", "Pūrva Ashādhā", "Uttara Ashādhā", "Shravana", "Dhanistha", "Shatabhisha", "Pūrva Bhādrapadā", "Uttara Bhādrapadā", "Revatī"];

    public static DashaResult Calculate(string requestPath)
    {
        using var requestDocument = JsonDocument.Parse(File.ReadAllText(requestPath));
        var request = requestDocument.RootElement;
        if (request.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Request root must be an object");
        var source = RequiredObject(request, "source");
        var chartFile = RequiredString(source, "chartFile", "source.chartFile");
        if (chartFile.StartsWith('~') || (Uri.TryCreate(chartFile, UriKind.Absolute, out var uri) && !uri.IsFile))
            throw new InvalidDataException("source.chartFile must be a local JSON file");

        var requestFullPath = Path.GetFullPath(requestPath);
        var chartPath = Path.IsPathRooted(chartFile) ? chartFile : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(requestFullPath)!, chartFile));
        using var chartDocument = JsonDocument.Parse(File.ReadAllText(chartPath));
        var chart = chartDocument.RootElement;
        if (chart.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Chart root must be an object");
        var input = RequiredObject(chart, "input");
        var offset = ParseOffset(RequiredNumber(input, "utcOffsetHours", "input.utcOffsetHours"));
        var vedic = RequiredObject(chart, "vedic_sidereal_lahiri");
        var birth = ParseDate(RequiredString(vedic, "utcIso", "vedic_sidereal_lahiri.utcIso"));
        var moon = RequiredNumber(RequiredObject(vedic, "siderealLons"), "Moon", "vedic_sidereal_lahiri.siderealLons.Moon");
        if (double.IsNaN(moon) || double.IsInfinity(moon)) throw new InvalidDataException("Moon longitude must be finite");

        var nakshatra = CalculateNakshatra(moon);
        var warnings = LegacyChartValidator.Validate(chart, nakshatra, moon);
        var output = request.TryGetProperty("output", out var outputElement) ? outputElement : default;
        if (output.ValueKind != JsonValueKind.Undefined && output.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("output must be an object");
        var depth = ReadDepth(output);
        var cycleEnd = birth.AddDays(CycleDays);
        DateTimeOffset outputStart;
        DateTimeOffset outputEnd;
        DateTimeOffset? usedReference = null;
        JsonElement period = default;
        var hasPeriod = output.ValueKind == JsonValueKind.Object && output.TryGetProperty("period", out period);
        if (hasPeriod && period.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("output.period must be an object");
        DateTimeOffset? suppliedReference = null;
        if (output.ValueKind == JsonValueKind.Object && output.TryGetProperty("referenceDateTime", out var reference))
        {
            suppliedReference = ParseDateValue(reference, "output.referenceDateTime");
            ValidateSupported(suppliedReference.Value, birth, cycleEnd, "referenceDateTime", false);
        }

        if (hasPeriod)
        {
            outputStart = ParseDateValue(Required(period, "start"), "output.period.start");
            outputEnd = ParseDateValue(Required(period, "end"), "output.period.end");
        }
        else if (depth <= 2)
        {
            outputStart = birth;
            outputEnd = cycleEnd;
        }
        else
        {
            usedReference = suppliedReference ?? DateTimeOffset.UtcNow;
            ValidateSupported(usedReference.Value, birth, cycleEnd, "referenceDateTime", false);
            var current = FindDashaChain(birth, nakshatra, usedReference.Value, depth - 2, cycleEnd);
            outputStart = current.Start < birth ? birth : current.Start;
            outputEnd = current.End > cycleEnd ? cycleEnd : current.End;
        }

        ValidatePeriod(outputStart, outputEnd, birth, cycleEnd);
        var dashas = BuildTree(birth, nakshatra, depth, outputStart, outputEnd, offset, cycleEnd);
        return new DashaResult(
            new DashaMeta("Vimshottari", depth, DashaYearDays, usedReference is null ? null : FormatDate(usedReference.Value, offset), new OutputPeriod(FormatDate(outputStart, offset), FormatDate(outputEnd, offset))),
            new Birth(FormatDate(birth, offset), moon, nakshatra with { FractionCompleted = TruncateFraction(nakshatra.FractionCompleted) }),
            dashas,
            warnings.Count == 0 ? null : warnings);
    }

    private static int ReadDepth(JsonElement output)
    {
        if (output.ValueKind != JsonValueKind.Object || !output.TryGetProperty("depth", out var value)) return 2;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var depth) || depth is < 1 or > 5)
            throw new InvalidDataException("output.depth must be an integer from 1 to 5");
        return depth;
    }

    private static MoonNakshatra CalculateNakshatra(double longitude)
    {
        var normalized = Normalize360(longitude);
        var index = Math.Min(26, (int)Math.Floor(normalized / NakshatraSize));
        var fraction = (normalized - index * NakshatraSize) / NakshatraSize;
        return new MoonNakshatra(index + 1, NakshatraNames[index], (int)Math.Floor(fraction * 4) + 1, Lords[index % 9], fraction);
    }

    private static List<DashaNode> BuildTree(DateTimeOffset birth, MoonNakshatra nakshatra, int depth, DateTimeOffset filterStart, DateTimeOffset filterEnd, TimeSpan offset, DateTimeOffset cycleEnd)
    {
        return TopLevelPeriods(birth, nakshatra, cycleEnd)
            .Where(period => Overlaps(period.Start, period.End, filterStart, filterEnd))
            .Select(period => ToNode(period, depth, filterStart, filterEnd, offset))
            .ToList();
    }

    private static List<DashaPeriod> TopLevelPeriods(DateTimeOffset birth, MoonNakshatra nakshatra, DateTimeOffset cycleEnd)
    {
        var index = Array.IndexOf(Lords, nakshatra.Lord);
        var start = birth.AddDays(-LordYears[index] * DashaYearDays * nakshatra.FractionCompleted);
        var result = new List<DashaPeriod>();
        while (start < cycleEnd)
        {
            var end = start.AddDays(LordYears[index] * DashaYearDays);
            result.Add(new DashaPeriod(1, Lords[index], start, end));
            start = end;
            index = (index + 1) % Lords.Length;
        }
        return result;
    }

    private static DashaPeriod FindDashaChain(DateTimeOffset birth, MoonNakshatra nakshatra, DateTimeOffset reference, int targetLevel, DateTimeOffset cycleEnd)
    {
        var current = TopLevelPeriods(birth, nakshatra, cycleEnd).Single(period => Contains(period, reference));
        for (var level = 1; level < targetLevel; level++) current = Children(current).Single(period => Contains(period, reference));
        return current;
    }

    private static DashaNode ToNode(DashaPeriod period, int depth, DateTimeOffset filterStart, DateTimeOffset filterEnd, TimeSpan offset)
    {
        List<DashaNode>? children = null;
        if (period.Level < depth)
        {
            children = Children(period).Where(child => Overlaps(child.Start, child.End, filterStart, filterEnd)).Select(child => ToNode(child, depth, filterStart, filterEnd, offset)).ToList();
            if (children.Count == 0) children = null;
        }
        var levelName = period.Level switch { 1 => "Mahadasha", 2 => "Antardasha", 3 => "Pratyantardasha", 4 => "Sookshma Dasha", _ => "Prana Dasha" };
        return new DashaNode(period.Level, levelName, period.Lord, FormatDate(period.Start, offset), FormatDate(period.End, offset), children);
    }

    private static List<DashaPeriod> Children(DashaPeriod parent)
    {
        var parentIndex = Array.IndexOf(Lords, parent.Lord);
        var result = new List<DashaPeriod>(9);
        var start = parent.Start;
        for (var childOffset = 0; childOffset < Lords.Length; childOffset++)
        {
            var index = (parentIndex + childOffset) % Lords.Length;
            var end = childOffset == Lords.Length - 1 ? parent.End : parent.Start.AddTicks((long)(parent.Duration.Ticks * (decimal)LordYears[index] / 120m));
            result.Add(new DashaPeriod(parent.Level + 1, Lords[index], start, end));
            start = end;
        }
        return result;
    }

    private static bool Contains(DashaPeriod period, DateTimeOffset value) => period.Start <= value && value < period.End;
    private static bool Overlaps(DateTimeOffset start, DateTimeOffset end, DateTimeOffset filterStart, DateTimeOffset filterEnd) => start < filterEnd && end > filterStart;
    private static double Normalize360(double value) => (value % 360 + 360) % 360;
    private static double TruncateFraction(double value) => Math.Truncate(value * 1_000_000_000_000d) / 1_000_000_000_000d;
    private static DateTimeOffset ParseDate(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static DateTimeOffset ParseDateValue(JsonElement value, string field)
    {
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException($"{field} must be an ISO 8601 datetime");
        var text = value.GetString()!;
        if (!HasExplicitOffset(text)) throw new InvalidDataException($"{field} must be an offset ISO 8601 datetime");
        try { return ParseDate(text); } catch (FormatException) { throw new InvalidDataException($"{field} must be an ISO 8601 datetime"); }
    }

    private static bool HasExplicitOffset(string value) =>
        value.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
        value.Length >= 6 && (value[^6] == '+' || value[^6] == '-') && value[^3] == ':';

    private static TimeSpan ParseOffset(double hours)
    {
        var minutes = hours * 60;
        if (double.IsNaN(hours) || double.IsInfinity(hours) || minutes != Math.Truncate(minutes) || Math.Abs(minutes) > 14 * 60)
            throw new InvalidDataException("input.utcOffsetHours is invalid");
        return TimeSpan.FromMinutes(minutes);
    }

    private static void ValidateSupported(DateTimeOffset value, DateTimeOffset birth, DateTimeOffset cycleEnd, string name, bool allowEnd)
    {
        if (value < birth || (allowEnd ? value > cycleEnd : value >= cycleEnd)) throw new InvalidDataException($"{name} is outside supported range");
    }

    private static void ValidatePeriod(DateTimeOffset start, DateTimeOffset end, DateTimeOffset birth, DateTimeOffset cycleEnd)
    {
        if (start < birth || start >= end || end > cycleEnd) throw new InvalidDataException("output.period must satisfy birthDateTime <= start < end <= cycleEnd");
    }

    private static string FormatDate(DateTimeOffset value, TimeSpan offset)
    {
        var local = value.ToOffset(offset);
        var ticks = local.Ticks - local.Ticks % TimeSpan.TicksPerMillisecond;
        return new DateTimeOffset(ticks, offset).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
    }

    private static JsonElement Required(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value : throw new InvalidDataException($"Required field is missing: {name}");
    private static JsonElement RequiredObject(JsonElement element, string name)
    {
        var value = Required(element, name);
        return value.ValueKind == JsonValueKind.Object ? value : throw new InvalidDataException($"{name} must be an object");
    }
    private static string RequiredString(JsonElement element, string name, string field) => Required(element, name).ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(Required(element, name).GetString()) ? Required(element, name).GetString()! : throw new InvalidDataException($"{field} must be a string");
    private static double RequiredNumber(JsonElement element, string name, string field) => Required(element, name).ValueKind == JsonValueKind.Number && Required(element, name).TryGetDouble(out var value) ? value : throw new InvalidDataException($"{field} must be a number");
    private sealed record DashaPeriod(int Level, string Lord, DateTimeOffset Start, DateTimeOffset End) { public TimeSpan Duration => End - Start; }
}

public static class LegacyChartValidator
{
    private static readonly string[] LegacyNames = ["アシュヴィニー", "バラニー", "クリッティカー", "ローヒニー", "ムリガシラ", "アールドラー", "プナルヴァス", "プシュヤ", "アシュレーシャー", "マガー", "プールヴァファルグニー", "ウッタラファルグニー", "ハスタ", "チトラー", "スワーティー", "ヴィシャーカー", "アヌラーダー", "ジェーシュタ", "ムーラ", "プールヴァアシャーダー", "ウッタラアシャーダー", "シュラヴァナ", "ダニシュタ", "シャタビシャー", "プールヴァバドラパダー", "ウッタラバドラパダー", "レーヴァティー"];

    public static List<Warning> Validate(JsonElement chart, MoonNakshatra expected, double siderealMoon)
    {
        var vedic = RequiredObject(chart, "vedic_sidereal_lahiri");
        var ayanamsha = RequiredNumber(vedic, "ayanamshaDeg", "vedic_sidereal_lahiri.ayanamshaDeg");
        var moonNakshatra = RequiredObject(RequiredObject(vedic, "nakshatras"), "Moon");
        var nakIndex = RequiredInt(moonNakshatra, "nakIdx", "vedic_sidereal_lahiri.nakshatras.Moon.nakIdx");
        var nakName = RequiredString(moonNakshatra, "nakName", "vedic_sidereal_lahiri.nakshatras.Moon.nakName");
        var pada = RequiredInt(moonNakshatra, "pada", "vedic_sidereal_lahiri.nakshatras.Moon.pada");
        var ruler = RequiredString(moonNakshatra, "ruler", "vedic_sidereal_lahiri.nakshatras.Moon.ruler");
        var tropical = RequiredNumber(RequiredObject(RequiredObject(chart, "western_tropical_placidus"), "lons"), "Moon", "western_tropical_placidus.lons.Moon");
        var warnings = new List<Warning>();
        Compare(warnings, "vedic_sidereal_lahiri.nakshatras.Moon.nakIdx", expected.Number - 1, nakIndex);
        Compare(warnings, "vedic_sidereal_lahiri.nakshatras.Moon.nakName", LegacyNames[expected.Number - 1], nakName);
        Compare(warnings, "vedic_sidereal_lahiri.nakshatras.Moon.pada", expected.Pada, pada);
        Compare(warnings, "vedic_sidereal_lahiri.nakshatras.Moon.ruler", expected.Lord, ruler);
        var expectedAyanamsha = Normalize360(tropical - siderealMoon);
        if (AngularDistance(expectedAyanamsha, ayanamsha) > 0.000001) warnings.Add(new Warning("LEGACY_AYANAMSHA_MISMATCH", "vedic_sidereal_lahiri.ayanamshaDeg", expectedAyanamsha, ayanamsha));
        return warnings;
    }

    private static void Compare(List<Warning> warnings, string field, object expected, object actual) { if (!Equals(expected, actual)) warnings.Add(new Warning("LEGACY_NAKSHATRA_MISMATCH", field, expected, actual)); }
    private static double Normalize360(double value) => (value % 360 + 360) % 360;
    private static double AngularDistance(double first, double second) { var distance = Math.Abs(Normalize360(first) - Normalize360(second)); return Math.Min(distance, 360 - distance); }
    private static JsonElement Required(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value : throw new InvalidDataException($"Required field is missing: {name}");
    private static JsonElement RequiredObject(JsonElement element, string name)
    {
        var value = Required(element, name);
        return value.ValueKind == JsonValueKind.Object ? value : throw new InvalidDataException($"{name} must be an object");
    }
    private static double RequiredNumber(JsonElement element, string name, string field) => Required(element, name).ValueKind == JsonValueKind.Number && Required(element, name).TryGetDouble(out var value) ? value : throw new InvalidDataException($"{field} must be a number");
    private static int RequiredInt(JsonElement element, string name, string field) => Required(element, name).ValueKind == JsonValueKind.Number && Required(element, name).TryGetInt32(out var value) ? value : throw new InvalidDataException($"{field} must be an integer");
    private static string RequiredString(JsonElement element, string name, string field) => Required(element, name).ValueKind == JsonValueKind.String ? Required(element, name).GetString()! : throw new InvalidDataException($"{field} must be a string");
}

public record DashaResult(DashaMeta Meta, Birth Birth, List<DashaNode> Dashas, List<Warning>? Warnings);
public record DashaMeta(string DashaSystem, int Depth, double DashaYearDays, string? ReferenceDateTime, OutputPeriod OutputPeriod);
public record OutputPeriod(string Start, string End);
public record Birth(string DateTime, double MoonSiderealLongitude, MoonNakshatra MoonNakshatra);
public record MoonNakshatra(int Number, string Name, int Pada, string Lord, double FractionCompleted);
public record DashaNode(int LevelNumber, string LevelName, string Lord, string StartDate, string EndDate, List<DashaNode>? Children);
public record Warning(string Code, string Field, object Expected, object Actual);
public static class DashaJsonOptions { public static readonly JsonSerializerOptions Default = new(JsonOptions.Default) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }; }
