namespace NugLabs.Cli.Models;

/// <summary>
/// Strain with flexible key/value pairs (Dictionary&lt;string, object?&gt;).
/// </summary>
public class Strain
{
    public Dictionary<string, object?> Data { get; } = new();

    public static Strain FromJsonElement(System.Text.Json.JsonElement obj)
    {
        var strain = new Strain();
        foreach (var prop in obj.EnumerateObject())
        {
            strain.Data[prop.Name] = prop.Value.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                System.Text.Json.JsonValueKind.Number => prop.Value.TryGetInt64(out var i) ? i : prop.Value.GetDouble(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined => null,
                System.Text.Json.JsonValueKind.Array => prop.Value.EnumerateArray()
                    .Select(e => e.ValueKind == System.Text.Json.JsonValueKind.String ? (object?)e.GetString() : e.GetRawText())
                    .ToList(),
                _ => prop.Value.GetRawText()
            };
        }
        return strain;
    }

    public static IReadOnlyList<Strain> FromJsonArray(System.Text.Json.JsonElement array)
    {
        var list = new List<Strain>();
        foreach (var item in array.EnumerateArray())
            list.Add(FromJsonElement(item));
        return list;
    }

    public void Print()
    {
        // Helper to get a raw value by key
        object? Get(string key) =>
            Data.TryGetValue(key, out var v) ? v : null;

        // Helper to format values (arrays, THC, etc.)
        static string? FormatValue(string key, object? value)
        {
            if (value is null)
                return null;

            if (value is IEnumerable<object?> arr)
            {
                var joined = string.Join(", ", arr.Select(x => x?.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)));
                return string.IsNullOrWhiteSpace(joined) ? null : joined;
            }

            var str = value.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(str))
                return null;

            if (string.Equals(key, "thc", StringComparison.OrdinalIgnoreCase))
            {
                if (value is double d) return $"{d}%";
                if (value is float f) return $"{f}%";
                if (value is int i) return $"{i}%";
                if (value is long l) return $"{l}%";
            }

            return str;
        }

        string? parents = null;
        string? children = null;
        string? growNotes = null;

        // Parse genetics -> parents / children
        if (Get("genetics") is string geneticsJson && !string.IsNullOrWhiteSpace(geneticsJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(geneticsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("parents", out var parentsEl) &&
                    parentsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var p = string.Join(", ", parentsEl.EnumerateArray().Select(e => e.ToString()));
                    if (!string.IsNullOrWhiteSpace(p))
                        parents = p;
                }
                if (root.TryGetProperty("children", out var childrenEl) &&
                    childrenEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var c = string.Join(", ", childrenEl.EnumerateArray().Select(e => e.ToString()));
                    if (!string.IsNullOrWhiteSpace(c))
                        children = c;
                }
            }
            catch
            {
                // ignore malformed genetics JSON
            }
        }

        // Parse grow_info -> notes only
        if (Get("grow_info") is string growJson && !string.IsNullOrWhiteSpace(growJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(growJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("notes", out var notesEl))
                {
                    var n = notesEl.ToString();
                    if (!string.IsNullOrWhiteSpace(n))
                        growNotes = n;
                }
            }
            catch
            {
                // ignore malformed grow_info JSON
            }
        }

        var printedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void PrintIf(string key, object? rawValue)
        {
            var display = FormatValue(key, rawValue);
            if (string.IsNullOrWhiteSpace(display))
                return;

            Console.WriteLine($"{key}: {display}");
            printedKeys.Add(key);
        }

        // Preferred logical order
        PrintIf("name", Get("name"));
        PrintIf("type", Get("type"));
        PrintIf("category", Get("category"));
        PrintIf("akas", Get("akas"));

        if (!string.IsNullOrWhiteSpace(parents))
            Console.WriteLine($"parents: {parents}");
        if (!string.IsNullOrWhiteSpace(children))
            Console.WriteLine($"children: {children}");

        PrintIf("thc", Get("thc"));
        PrintIf("description", Get("description"));
        PrintIf("top_effect", Get("top_effect"));
        PrintIf("positive_effects", Get("positive_effects"));
        PrintIf("negative_effects", Get("negative_effects"));
        PrintIf("flavors", Get("flavors"));
        PrintIf("detailed_terpenes", Get("detailed_terpenes"));
        PrintIf("helps_with", Get("helps_with"));
        PrintIf("rating", Get("rating"));

        if (!string.IsNullOrWhiteSpace(growNotes))
        {
            Console.WriteLine($"grow_notes: {growNotes}");
            printedKeys.Add("grow_info");
        }

        // Skip these keys entirely
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "id",
            "review_count",
            "genetics",
            "grow_info"
        };

        // Print any remaining keys in alphabetical order
        foreach (var (key, value) in Data.OrderBy(kv => kv.Key))
        {
            if (skip.Contains(key) || printedKeys.Contains(key))
                continue;

            PrintIf(key, value);
        }

        Console.WriteLine();
    }
}
