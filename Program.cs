using System.CommandLine;
using NugLabs;
using NugLabs.Cli.Menu;

var strainClient = new NugLabsClient();

var rootCommand = new RootCommand("nug-labs – search strains by name or term");
var searchArg = new Argument<string[]>("name", "Strain name or search term (e.g. \"Mimosa\")");
rootCommand.AddArgument(searchArg);

rootCommand.SetHandler((string[] name) =>
{
    var menu = new NugLabsMenu(strainClient);
    menu.Run(name);
}, searchArg);

return await rootCommand.InvokeAsync(args);
