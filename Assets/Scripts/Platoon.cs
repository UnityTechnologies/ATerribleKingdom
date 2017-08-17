using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Platoon : MonoBehaviour
{
	public List<Unit> units;
	private Vector3[] tempPositions; //an array to do position calculations, doesn't necessary represent the position of the units at the moment
	private float formationOffset = 3f;

	public void ExecuteCommand(AICommand command)
	{
		tempPositions = GetFormationPositions(command.destination);
		for(int i=0; i<units.Count; i++)
		{
			command.destination = tempPositions[i]; //change the position for the command for each unit
			units[i].ExecuteCommand(command);
		}
	}

	public Vector3[] GetCurrentPositions()
	{
		tempPositions = new Vector3[units.Count];

		for(int i=0; i<units.Count; i++)
		{
			tempPositions[i] = units[i].transform.position;
		}

		return tempPositions;
	}

	public Vector3[] GetFormationPositions(Vector3 formationCenter)
	{
		//TODO: accomodate bigger numbers
		//float currentOffset = formationOffset;
		tempPositions = new Vector3[units.Count];

		float increment = 360f / units.Count;
		for(int k=0; k<units.Count; k++)
		{
			float angle = increment * k;
			Vector3 offset = new Vector3(formationOffset * Mathf.Cos(angle * Mathf.Deg2Rad), 0f, formationOffset * Mathf.Sin(angle * Mathf.Deg2Rad));
			tempPositions[k] = formationCenter + offset;
		}

		return tempPositions;
	}

	public void SetPositions(Vector3[] newPositions)
	{
		for(int i=0; i<units.Count; i++)
		{
			units[i].transform.position = newPositions[i];
		}
	}

	private void OnDrawGizmosSelected()
	{
		for(int i=0; i<units.Count; i++)
		{
			Gizmos.color = new Color(.8f, .8f, 1f, 1f);
			Gizmos.DrawCube(units[i].transform.position, new Vector3(1f, .1f, 1f));
		}
	}
}