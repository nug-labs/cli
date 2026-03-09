using System.Net.Http.Json;
using System.Text.Json;

namespace NugLabs.Cli.Services;

public class DataRefreshService
{
    private readonly StrainService _strainService;
    private readonly HttpClient _httpClient;
    private readonly string _timestampPath;
    private const int RefreshIntervalHours = 12;
    private const string DefaultApiBase = "http://localhost:8080";

    public DataRefreshService(StrainService strainService)
    {
        _strainService = strainService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var baseDir = AppContext.BaseDirectory;
        _timestampPath = Path.Combine(baseDir, "assets", ".last_fetch");
    }

    public void StartBackgroundRefresh(string? apiBase = null)
    {
        _ = Task.Run(() => TryRefreshAsync(apiBase ?? DefaultApiBase));
    }

    private async Task TryRefreshAsync(string apiBase)
    {
        try
        {
            var lastFetch = GetLastFetchTime();
            if (lastFetch.HasValue && (DateTime.UtcNow - lastFetch.Value).TotalHours < RefreshIntervalHours)
                return;

            var url = $"{apiBase.TrimEnd('/')}/api/v1/strains";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                return;

            var strains = new List<Dictionary<string, object?>>();
            foreach (var el in root.EnumerateArray())
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in el.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.TryGetInt64(out var i) ? i : prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        JsonValueKind.Array => prop.Value.EnumerateArray().Select(e => (object?)e.ToString()).ToList(),
                        _ => prop.Value.GetRawText()
                    };
                }
                strains.Add(dict);
            }

            var path = Path.Combine(AppContext.BaseDirectory, "assets", "data.json");
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var outJson = JsonSerializer.Serialize(strains, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, outJson);

            await File.WriteAllTextAsync(_timestampPath, DateTime.UtcNow.ToString("O"));
            _strainService.ReloadFromDisk();
        }
        catch
        {
            // Silent fail; use local data
        }
    }

    private DateTime? GetLastFetchTime()
    {
        if (!File.Exists(_timestampPath))
            return null;
        var s = File.ReadAllText(_timestampPath);
        return DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
    }
}
