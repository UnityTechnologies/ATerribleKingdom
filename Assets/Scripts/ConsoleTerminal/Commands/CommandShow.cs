using System.Runtime.CompilerServices;
using System.Windows.Input;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.Collections.Generic;

public class CommandShow : ICommand
{
    public string CommandName => "show";
    public string Description => "Show debug tools navmesh or hitboxes.";
    private static GameObject navMeshHologram;
    private static List<GameObject> activeHitboxes = new List<GameObject>();

    private Collider[] allColliders;
    public bool ExecuteCommand(string[] args)
    {
        if (args.Length == 0)
        {
            TerminalManager.Instance.LogError("Use: show [hitboxes or navmesh]");
            return false;
        }

        int mood = -1;

        if (args.Length > 1)
        {
            int.TryParse(args[1], out mood);
        }

        switch (args[0].ToLower())
        {
            case "hitboxes":
                ShowHitboxes(mood);
                return true;
            case "navmesh":
                ShowNavMesh(mood);
                return true;
            default:
                TerminalManager.Instance.LogError("Invalid argument. Use: show hitboxes or show navmesh");
                return false;
        }
    }

    private void ShowHitboxes(int mood)
    {
        if (mood == 1)
        {
            if (activeHitboxes.Count == 0)
            {

                allColliders = Object.FindObjectsOfType<Collider>();

                foreach (Collider col in allColliders)
                {
                    if (col.CompareTag("MainCamera")) continue;
                    if (col.GetComponent<Unit>() == null) continue;

                    GameObject hitbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    GameObject.Destroy(hitbox.GetComponent<Collider>());
                    hitbox.transform.SetParent(col.transform);
                    activeHitboxes.Add(hitbox);
                    hitbox.transform.position = col.bounds.center;
                    hitbox.transform.localScale = col.bounds.size;

                    MeshRenderer rend = hitbox.GetComponent<MeshRenderer>();

                    rend.material = new Material(Shader.Find("Sprites/Default"));
                    if (col.CompareTag("Foreigners"))
                    {
                        rend.material.color = new Color(1f, 0f, 0f, 0.15f);
                    }
                    else
                    {
                        rend.material.color = new Color(0f, 1f, 0f, 0.15f);
                    }
                }

                TerminalManager.Instance.LogSuccess("Hitboxes visualization enabled.");
                return;
            }
        }
        else
        {
            foreach (GameObject box in activeHitboxes)
            {
                if (box != null) Object.Destroy(box);
            }
            activeHitboxes.Clear();
            TerminalManager.Instance.LogSuccess("Hitboxes visualization disabled.");
            return;
        }
    }

    private void ShowNavMesh(int mood)
    {
        if (mood == 1)
        {
            navMeshHologram = new GameObject();

            MeshRenderer rend = navMeshHologram.AddComponent<MeshRenderer>();
            MeshFilter filter = navMeshHologram.AddComponent<MeshFilter>();

            rend.material = new Material(Shader.Find("Sprites/Default"));

            NavMeshTriangulation dataPlane = NavMesh.CalculateTriangulation();

            Mesh mesh = new Mesh();
            mesh.vertices = dataPlane.vertices;
            mesh.triangles = dataPlane.indices;

            filter.mesh = mesh;

            rend.material.color = new Color(0f, 0f, 1f, 0.15f);
            navMeshHologram.transform.position += Vector3.up * 0.05f;
            TerminalManager.Instance.LogSuccess("NavMesh visualization enabled.");
            return;
        }
        else
        {
            GameObject.Destroy(navMeshHologram);
            navMeshHologram = null;
            TerminalManager.Instance.LogSuccess("NavMesh visualization hidden.");
            return;
        }
    }
}

