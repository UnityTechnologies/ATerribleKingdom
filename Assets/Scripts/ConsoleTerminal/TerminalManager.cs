using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class TerminalManager : MonoBehaviour
{
    // Singleton pattern so any script can easily access the terminal manager
    public static TerminalManager Instance { get; private set; }
    public Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();

    // First string is the message, second string is the color tag ("green", "red", etc)
    public event Action<string, string> OnLogGenerated;

    [Header("Spawn Prefabs catalog")]
    public GameObject[] spawnableUnits;

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        RegisterAllCommands();
    }

    private void RegisterAllCommands()
    {
        commands.Clear();

        // Set-time command
        CommandTime timeCmd = new CommandTime();
        commands.Add(timeCmd.CommandName, timeCmd);

        // Help command
        CommandHelp helpCmd = new CommandHelp();
        commands.Add(helpCmd.CommandName, helpCmd);

        // Kill command
        CommandKill killCmd = new CommandKill();
        commands.Add(killCmd.CommandName, killCmd);

        // Spawn command
        CommandSpawn spawnCmd = new CommandSpawn();
        commands.Add(spawnCmd.CommandName, spawnCmd);

        // Show command
        CommandShow showCmd = new CommandShow();
        commands.Add(showCmd.CommandName, showCmd);
    }


    /// <summary>
    /// Processes the raw string received from the UI
    /// </summary>
    public void ProcessCommand(string rawInput)
    {
        // 1. Clean the input
        string input = rawInput.Trim();

        // 2. Ignore if empty
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        // For now, let's just log it to the Unity Console to prove it works!
        LogMessage(input);

        string[] splitArgsInput = input.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        string commandName = splitArgsInput[0];

        if (commands.ContainsKey(commandName))
        {
            string[] cleanArgs = splitArgsInput.Skip(1).ToArray();
            bool success = commands[commandName].ExecuteCommand(cleanArgs);

            if (success)
            {
                LogSuccess(commandName + " executed successfully.");
            }
            else
            {
                LogError(commandName + " failed to execute.");
            }
        }
        else
        {
            LogError("Command " + commandName + " not found.");
        }
    }

    public void LogMessage(string message)
    {
        OnLogGenerated?.Invoke(message, "#aaaaaa");
    }

    public void LogSuccess(string message)
    {
        OnLogGenerated?.Invoke(message, "green");
    }

    public void LogError(string message)
    {
        OnLogGenerated?.Invoke(message, "red");
    }
}
