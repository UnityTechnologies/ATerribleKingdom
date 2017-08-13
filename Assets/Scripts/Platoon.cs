using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Platoon : MonoBehaviour
{
	public List<Unit> units;
	private Vector3[] positions;

	public void ExecuteCommand(AICommand command)
	{
		for(int i=0; i<units.Count; i++)
		{
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

	public void SetPositions(Vector3[] newPositions)
	{
		for(int i=0; i<units.Count; i++)
		{
			units[i].transform.position = newPositions[i];
		}
	}
}