using UnityEngine;

public class CommandSpawn : ICommand
{
    public string CommandName => "spawn";
    public string Description => "Spawns a unit at the cursor position. Parameters: spawn [show list], spawn IDUnit [spawn 1 unit of IDUnit], spawn IDUnit value [spawn <UnitID> <amount>].";

    public bool ExecuteCommand(string[] args)
    {
        if (args.Length == 0)
        {
            for (int i = 0; i < TerminalManager.Instance.spawnableUnits.Length; i++)
            {
                TerminalManager.Instance.LogMessage($"- [ID: {i}] {TerminalManager.Instance.spawnableUnits[i].name}");
            }
            return true;
        }

        GameObject prefabToSpawn = null;

        if (int.TryParse(args[0], out int idToSpawn))
        {
            if (idToSpawn >= 0 && idToSpawn < TerminalManager.Instance.spawnableUnits.Length)
            {
                prefabToSpawn = TerminalManager.Instance.spawnableUnits[idToSpawn];
            }
        }
        else
        {
            TerminalManager.Instance.LogError($"Prefab '{args[0]}' not found. Run 'spawn' to see the list.");
            return false;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            int amountToSpawn = 1;

            if (args.Length > 1)
            {
                if (int.TryParse(args[1], out int parsedAmount))
                {
                    amountToSpawn = parsedAmount;
                }
            }

            for (int i = 0; i < amountToSpawn; i++)
            {
                Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                GameObject.Instantiate(prefabToSpawn, point + randomOffset, Quaternion.identity);
            }

            TerminalManager.Instance.LogSuccess($"Spawned {amountToSpawn} units of type '{args[0]}'");
            return true;
        }
        else
        {
            TerminalManager.Instance.LogError("Failed to spawn " + idToSpawn);
            return false;
        }
    }

}