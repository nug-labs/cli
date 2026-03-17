using NugLabs;
using NugLabs.Cli.Interfaces;
using NugLabs.Cli.Output;

namespace NugLabs.Cli.Menu;

public class NugLabsMenu : IMenuDropper
{
    private readonly NugLabsClient _strainClient;

    public NugLabsMenu(NugLabsClient strainClient)
    {
        _strainClient = strainClient;
    }

    public void Run(string[] args)
    {
        var raw = args.Length > 0 ? string.Join(" ", args) : "";
        var query = NormalizeQuery(raw);
        var exact = _strainClient.GetStrainAsync(query).GetAwaiter().GetResult();
        var results = exact is not null
            ? new[] { exact }
            : _strainClient.SearchStrains(query);

        if (results.Count == 0)
        {
            Console.WriteLine("No strains found.");
            return;
        }

        foreach (var strain in results)
            StrainConsolePrinter.Print(strain);
    }

    private static string NormalizeQuery(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var words = input
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < words.Length; i++)
        {
            var w = words[i];
            if (w.Length == 0) continue;
            words[i] = char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w[1..] : string.Empty);
        }

        return string.Join(" ", words);
    }
}
