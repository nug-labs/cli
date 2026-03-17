using System.Text.Json;
using NugLabs.Models;

namespace NugLabs.Cli.Output;

public static class StrainConsolePrinter
{
    private static bool _bannerPrinted;

    public static void Print(Strain strain)
    {
        PrintBanner();
        Console.WriteLine("----- Strain Information -----");
        Console.WriteLine();

        object? Get(string key)
        {
            return key.ToLowerInvariant() switch
            {
                "name" => strain.Name,
                "type" => strain.Type,
                "thc" => strain.Thc,
                "description" => strain.Description,
                "akas" => strain.Akas,
                _ => TryGetAdditionalValue(strain, key),
            };
        }

        static string? FormatValue(string key, object? value)
        {
            if (value is null)
                return null;

            if (value is JsonElement json)
                return FormatJsonElement(key, json);

            if (value is IEnumerable<string> stringEnumerable && value is not string)
            {
                var joined = string.Join(", ", stringEnumerable.Where(x => !string.IsNullOrWhiteSpace(x)));
                return string.IsNullOrWhiteSpace(joined) ? null : joined;
            }

            if (value is IEnumerable<object?> objectEnumerable && value is not string)
            {
                var joined = string.Join(", ", objectEnumerable.Select(x => x?.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)));
                return string.IsNullOrWhiteSpace(joined) ? null : joined;
            }

            if (string.Equals(key, "thc", StringComparison.OrdinalIgnoreCase))
            {
                return value switch
                {
                    double d => $"{d}%",
                    float f => $"{f}%",
                    int i => $"{i}%",
                    long l => $"{l}%",
                    _ => value.ToString()
                };
            }

            var str = value.ToString();
            return string.IsNullOrWhiteSpace(str) ? null : str;
        }

        string? growNotes = null;

        if (Get("grow_info") is JsonElement growInfoElement)
        {
            growNotes = ExtractGrowNotes(growInfoElement);
        }
        else if (Get("grow_info") is string growJson && !string.IsNullOrWhiteSpace(growJson))
        {
            TryParseStringifiedObject(growJson, element => growNotes = ExtractGrowNotes(element));
        }

        var printedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? BuildLine(string key, object? rawValue)
        {
            var display = FormatValue(key, rawValue);
            if (string.IsNullOrWhiteSpace(display))
                return null;

            printedKeys.Add(key);
            return $"{key}: {display}";
        }

        static void PrintSection(List<string> lines, ref bool hasPrintedSection)
        {
            if (lines.Count == 0)
                return;

            if (hasPrintedSection)
                Console.WriteLine();

            foreach (var line in lines)
                Console.WriteLine(line);

            hasPrintedSection = true;
        }

        var name = FormatValue("name", Get("name"));
        var akas = FormatValue("akas", Get("akas"));
        var type = FormatValue("type", Get("type"));
        var thc = FormatValue("thc", Get("thc"));
        var flavours = FormatValue("flavours", Get("flavours")) ?? FormatValue("flavors", Get("flavors"));
        var terpenes = FormatValue("terpenes", Get("terpenes")) ?? FormatValue("detailed_terpenes", Get("detailed_terpenes"));
        var effects = FormatValue("positive_effects", Get("positive_effects"));
        var helpsWith = FormatValue("helps_with", Get("helps_with"));
        var description = FormatValue("description", Get("description"));
        var hasPrintedSection = false;

        var identityLines = new List<string>();
        var nameLine = BuildLine("Name", name);
        var akaLine = BuildLine("AKAs", akas);
        if (nameLine is not null) identityLines.Add(nameLine);
        if (akaLine is not null) identityLines.Add(akaLine);
        PrintSection(identityLines, ref hasPrintedSection);

        var typeLines = new List<string>();
        var typeLine = BuildLine("Type", type);
        var averagingLine = BuildLine("Averaging", string.IsNullOrWhiteSpace(thc) ? null : $"THC {thc}");
        if (typeLine is not null) typeLines.Add(typeLine);
        if (averagingLine is not null) typeLines.Add(averagingLine);
        PrintSection(typeLines, ref hasPrintedSection);

        var profileLines = new List<string>();
        var flavoursLine = BuildLine("Flavours", flavours);
        var terpenesLine = BuildLine("Terpenes", terpenes);
        if (flavoursLine is not null) profileLines.Add(flavoursLine);
        if (terpenesLine is not null) profileLines.Add(terpenesLine);
        PrintSection(profileLines, ref hasPrintedSection);

        var effectsLines = new List<string>();
        var effectsLine = BuildLine("Effects", effects);
        var helpsLine = BuildLine("Helps with", helpsWith);
        if (effectsLine is not null) effectsLines.Add(effectsLine);
        if (helpsLine is not null) effectsLines.Add(helpsLine);
        PrintSection(effectsLines, ref hasPrintedSection);

        var descriptionLines = new List<string>();
        var descriptionLine = BuildLine("Description", description);
        if (descriptionLine is not null) descriptionLines.Add(descriptionLine);
        PrintSection(descriptionLines, ref hasPrintedSection);

        if (!string.IsNullOrWhiteSpace(growNotes))
        {
            var growLines = new List<string>();
            var growLine = BuildLine("Grow notes", growNotes);
            if (growLine is not null) growLines.Add(growLine);
            PrintSection(growLines, ref hasPrintedSection);
            printedKeys.Add("grow_info");
        }

        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "review_count",
            "genetics",
            "grow_info",
            "category",
            "name",
            "type",
            "thc",
            "description",
            "akas",
            "parents",
            "children",
            "top_effect",
            "positive_effects",
            "negative_effects",
            "flavors",
            "flavours",
            "detailed_terpenes",
            "terpenes",
            "helps_with",
            "rating"
        };

        var additionalLines = new List<string>();
        foreach (var (key, value) in strain.AdditionalData?.OrderBy(kv => kv.Key) ?? Enumerable.Empty<KeyValuePair<string, JsonElement>>())
        {
            if (skip.Contains(key) || printedKeys.Contains(key))
                continue;

            var label = char.ToUpperInvariant(key[0]) + key[1..];
            var line = BuildLine(label, value);
            if (line is not null)
                additionalLines.Add(line);
        }
        PrintSection(additionalLines, ref hasPrintedSection);

        if (hasPrintedSection)
            Console.WriteLine();
    }

    private static object? TryGetAdditionalValue(Strain strain, string key)
    {
        if (strain.AdditionalData is null)
            return null;

        return strain.AdditionalData.TryGetValue(key, out var value) ? value : null;
    }

    private static string? FormatJsonElement(string key, JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(FormatScalar).Where(x => !string.IsNullOrWhiteSpace(x))),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when string.Equals(key, "thc", StringComparison.OrdinalIgnoreCase) => $"{element.ToString()}%",
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static string? FormatScalar(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static string? ExtractGrowNotes(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        if (!root.TryGetProperty("notes", out var notesEl))
            return null;

        var text = notesEl.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void TryParseStringifiedObject(string raw, Action<JsonElement> action)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            action(doc.RootElement);
        }
        catch
        {
            // Ignore malformed JSON content.
        }
    }

    private static void PrintBanner()
    {
        if (_bannerPrinted)
            return;

        _bannerPrinted = true;

        try
        {
            var assembly = typeof(StrainConsolePrinter).Assembly;
            const string resourceName = "NugLabs.Cli.assets.ascii.txt";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return;

            using var reader = new StreamReader(stream);
            var art = reader.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(art))
            {
                Console.WriteLine(art);
                Console.WriteLine();
            }
        }
        catch
        {
            // Ignore banner failures; CLI should still work.
        }
    }
}
