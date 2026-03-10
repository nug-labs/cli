using System.CommandLine;
using NugLabs.Cli.Menu;
using NugLabs.Cli.Services;

var strainService = new StrainService();
var refreshService = new DataRefreshService(strainService);

// Background: refresh from API if last fetch > 12 hours
var apiOption = "https://strains.nuglabs.co";
refreshService.StartBackgroundRefresh(apiOption);

var rootCommand = new RootCommand("nug-labs – search strains by name or term");
var searchArg = new Argument<string[]>("name", "Strain name or search term (e.g. \"Mimosa\")");
rootCommand.AddArgument(searchArg);

rootCommand.SetHandler((string[] name) =>
{
    var menu = new NugLabsMenu(strainService);
    menu.Run(name);
}, searchArg);

return await rootCommand.InvokeAsync(args);
