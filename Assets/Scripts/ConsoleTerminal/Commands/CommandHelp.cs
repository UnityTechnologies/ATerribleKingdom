using UnityEngine;

public class CommandHelp : ICommand
{
    public string CommandName => "help";
    public string Description => "Show the list of availables commands";

    public bool ExecuteCommand(string[] args)
    {
        if (args.Length > 0)
        {
            TerminalManager.Instance.LogError("Requires no arguments!");
            return false;
        }

        TerminalManager.Instance.LogMessage("Availables commands:");

        foreach (var cmd in TerminalManager.Instance.commands)
        {
            TerminalManager.Instance.LogMessage("- " + cmd.Key + ": " + cmd.Value.Description);
        }

        return true;
    }

}