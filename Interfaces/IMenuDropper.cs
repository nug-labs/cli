namespace NugLabs.Cli.Interfaces;

/// <summary>
/// Base interface for the CLI menu system.
/// </summary>
public interface IMenuDropper
{
    /// <summary>
    /// Run the menu with the given arguments (e.g. strain name to search).
    /// </summary>
    void Run(string[] args);
}
