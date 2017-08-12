//GetPrefabType and InstantiatePrefab are obsolete,
//but I don't have time to fix them right now
#pragma warning disable CS0618

using UnityEngine;
using UnityEditor;
using System.Collections;

public class ReplaceSelection : ScriptableWizard
{
	static GameObject replacement = null;
	static bool keep = false;

	public GameObject replacementObject = null;
	public bool keepOriginals = false;

	public bool keepRotation = true;
	public bool keepScaling = true;

	[MenuItem("Custom/Replace Selection...")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard("Replace Selection", typeof(ReplaceSelection), "Replace");
	}

	public ReplaceSelection()
	{
		replacementObject = replacement;
		keepOriginals = keep;
	}

	void OnWizardUpdate()
	{
		titleContent = new GUIContent("Replace Selection");
		helpString = "Replace the selection with a GameObject or Prefab";

		replacement = replacementObject;
		keep = keepOriginals;
	}

	void OnWizardCreate()
	{
		if (replacement == null)
			return;

		Transform[] transforms = Selection.GetTransforms(
			SelectionMode.TopLevel | SelectionMode.OnlyUserModifiable);


		foreach (Transform t in transforms)
		{

			GameObject newGO = null;
			PrefabType pref = EditorUtility.GetPrefabType(replacement);


			if (pref == PrefabType.Prefab || pref == PrefabType.ModelPrefab)
			{
				newGO = (GameObject)EditorUtility.InstantiatePrefab(replacement);
			}
			else
			{
				newGO = (GameObject)Editor.Instantiate(replacement);
			}

			Undo.RegisterCreatedObjectUndo(newGO, "Replace Selection");

			newGO.name = replacement.name;

			//apply the old object's transformation to the new GO
			Transform newTransform = newGO.transform;
			newTransform.parent = t.parent;
			newTransform.localPosition = t.localPosition;
			if(keepScaling) { newTransform.localScale = t.localScale; }
			if(keepRotation) { newTransform.localRotation = t.localRotation; }
		}

		if (!keep)
		{
			foreach (GameObject g in Selection.gameObjects)
			{
				Undo.DestroyObjectImmediate(g);
			}
		}
	}
}