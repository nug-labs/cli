using System.Text.Json;
using NugLabs.Cli.Models;

namespace NugLabs.Cli.Services;

public class StrainService
{
    private readonly string _dataPath;
    private List<Strain>? _cache;

    public StrainService()
    {
        var baseDir = AppContext.BaseDirectory;
        _dataPath = Path.Combine(baseDir, "assets", "data.json");
    }

    public IReadOnlyList<Strain> GetAll()
    {
        if (_cache != null)
            return _cache;

        if (!File.Exists(_dataPath))
            return Array.Empty<Strain>();

        var json = File.ReadAllText(_dataPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            _cache = Strain.FromJsonArray(root).ToList();
        else if (root.TryGetProperty("strains", out var strains))
            _cache = Strain.FromJsonArray(strains).ToList();
        else if (root.TryGetProperty("filtered_strains", out var filtered))
            _cache = Strain.FromJsonArray(filtered).ToList();
        else
            _cache = new List<Strain>();

        return _cache;
    }

    public void ReloadFromDisk()
    {
        _cache = null;
        GetAll();
    }

    public void SaveToDisk(IReadOnlyList<Strain> strains)
    {
        var dir = Path.GetDirectoryName(_dataPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var list = strains.Select(s => s.Data).ToList();
        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_dataPath, json);
        _cache = strains.ToList();
    }

    public IReadOnlyList<Strain> Search(string query)
    {
        var all = GetAll();
        if (string.IsNullOrWhiteSpace(query))
            return all;

        var q = query.Trim();

        bool Matches(object? value) =>
            value != null &&
            string.Equals(value.ToString(), q, StringComparison.OrdinalIgnoreCase);

        return all
            .Where(s =>
            {
                // Exact match on name
                if (s.Data.TryGetValue("name", out var nameObj) && Matches(nameObj))
                    return true;

                // Exact match on any AKA
                if (s.Data.TryGetValue("akas", out var akasObj) && akasObj is IEnumerable<object?> akas)
                {
                    foreach (var aka in akas)
                    {
                        if (Matches(aka))
                            return true;
                    }
                }

                return false;
            })
            .ToList();
    }
}
