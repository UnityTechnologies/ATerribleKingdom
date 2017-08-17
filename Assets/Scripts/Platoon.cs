using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Platoon : MonoBehaviour
{
	public List<Unit> units;
	private Vector3[] positions;
	private float formationOffset = 1f;

	public void ExecuteCommand(AICommand command)
	{
		positions = GetFormationPositions(command.destination);
		for(int i=0; i<units.Count; i++)
		{
			command.destination = positions[i];
			units[i].ExecuteCommand(command);
		}
	}

	public Vector3[] GetPositions()
	{
		positions = new Vector3[units.Count];

		for(int i=0; i<units.Count; i++)
		{
			positions[i] = units[i].transform.position;
		}

		return positions;
	}

	public Vector3[] GetFormationPositions(Vector3 formationCenter)
	{
		//float currentOffset = formationOffset;
		positions = new Vector3[units.Count];

		float increment = 360f / units.Count;
		for(int k=0; k<units.Count; k++)
		{
			float angle = increment * k;
			Vector3 offset = new Vector3(formationOffset * Mathf.Cos(angle * Mathf.Deg2Rad), 0f, formationOffset * Mathf.Sin(angle * Mathf.Deg2Rad));
			positions[k] = formationCenter + offset;
		}

		return positions;
	}

	public void SetPositions(Vector3[] newPositions)
	{
		for(int i=0; i<units.Count; i++)
		{
			units[i].transform.position = newPositions[i];
		}
	}
}