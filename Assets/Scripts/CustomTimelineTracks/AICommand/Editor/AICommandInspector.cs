using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AICommandClip))]
public class AICommandInspector : Editor
{
	private SerializedProperty commandProp;
	private int typeIndex;

	private void OnEnable()
	{
		SceneView.onSceneGUIDelegate += OnSceneGUI;
		commandProp = serializedObject.FindProperty("commandType");
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.PropertyField(commandProp);

		typeIndex = serializedObject.FindProperty("commandType").enumValueIndex;
		AICommand.CommandType commandType = (AICommand.CommandType)typeIndex;

		//Draws only the appropriate information based on the Command Type
		switch(commandType)
		{
			case AICommand.CommandType.GoToAndIdle:
			case AICommand.CommandType.GoToAndGuard:
				EditorGUILayout.PropertyField(serializedObject.FindProperty("targetPosition")); //position
				break;

			case AICommand.CommandType.AttackTarget:
				EditorGUILayout.PropertyField(serializedObject.FindProperty("targetUnit")); //Unit to attack
				break;

			case AICommand.CommandType.Die:
			case AICommand.CommandType.Stop:
				//no information needed
				break;
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void OnDisable()
	{
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
	}


	//Draws a position handle on the position associated with the AICommand
	//the handle can be moved to reposition the targetPosition property
	private void OnSceneGUI(SceneView v)
	{
		if((AICommand.CommandType)typeIndex == AICommand.CommandType.GoToAndGuard
			|| (AICommand.CommandType)typeIndex == AICommand.CommandType.GoToAndIdle)
		{
			EditorGUI.BeginChangeCheck();
			Vector3 gizmoPos = Handles.PositionHandle(serializedObject.FindProperty("targetPosition").vector3Value, Quaternion.identity);
			
			if(EditorGUI.EndChangeCheck())
			{
				serializedObject.FindProperty("targetPosition").vector3Value = gizmoPos;
				serializedObject.ApplyModifiedProperties();
				
				Repaint();
			}
		}
	}
}
