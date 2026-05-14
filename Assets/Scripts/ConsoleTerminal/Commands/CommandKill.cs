using UnityEngine;
using System.Collections.Generic;

public class CommandKill : ICommand
{
    public string CommandName => "kill";
    public string Description => "Kills all the units selected. Parameters: kill list, kill all, kill enemies, kill selected, or kill <UnitID>";
    private static Dictionary<int, Unit> cachedUnits = new Dictionary<int, Unit>();

    public bool ExecuteCommand(string[] args)
    {
        if (args.Length == 0)
        {
            TerminalManager.Instance.LogError("Missing arguments. Use: kill list, kill all, kill enemies, kill selected, or kill <UnitID>");
            return false;
        }

        string target = args[0].ToLower();
        AICommand dieCmd = new AICommand(AICommand.CommandType.Die);
        Unit[] allUnits = Object.FindObjectsOfType<Unit>();

        switch (target)
        {
            case "all":
                foreach (Unit u in allUnits) { u.ExecuteCommand(dieCmd); }
                TerminalManager.Instance.LogSuccess($"Eliminated {allUnits.Length} units.");
                return true;

            case "enemies":
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Foreigners");
                foreach (GameObject obj in enemies) { obj.GetComponent<Unit>().ExecuteCommand(dieCmd); }
                TerminalManager.Instance.LogSuccess($"Eliminated {enemies.Length} enemies.");
                return true;

            case "selected":
                Transform[] selected = GameManager.Instance.GetSelectionTransforms();
                foreach (Transform t in selected) { t.GetComponent<Unit>().ExecuteCommand(dieCmd); }
                TerminalManager.Instance.LogSuccess($"Eliminated {selected.Length} selected units.");
                return true;

            case "list":
                TerminalManager.Instance.LogMessage("Units list:");
                cachedUnits.Clear();
                for (int i = 0; i < allUnits.Length; i++)
                {
                    cachedUnits[i] = allUnits[i];
                    TerminalManager.Instance.LogMessage($"- [ID: {i}] {allUnits[i].name}");
                }
                return true;

            default:
                if (int.TryParse(args[0], out int idToKill))
                {
                    if (cachedUnits.ContainsKey(idToKill))
                    {
                        Unit unitToKill = cachedUnits[idToKill];
                        if (unitToKill != null)
                        {
                            unitToKill.ExecuteCommand(dieCmd);
                            TerminalManager.Instance.LogSuccess($"Unit [ID: {idToKill}] eliminated.");
                            return true;
                        }
                        else
                        {
                            TerminalManager.Instance.LogError($"Unit [ID: {idToKill}] is already dead.");
                            return false;
                        }
                    }
                    else
                    {
                        TerminalManager.Instance.LogError($"ID {idToKill} not found. Run 'kill list' first to get the IDs.");
                        return false;
                    }
                }

                TerminalManager.Instance.LogError($"Invalid argument '{args[0]}'. Use an ID number, or 'all', 'enemies', 'selected', 'list'.");
                return false;
        }
    }

}