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

	private void Start()
	{
		for(int i = 0; i < units.Count; i++)
		{
			units[i].OnDie += UnitDeadHandler;
		}
	}

	//Executes a command on all Units
	public void ExecuteCommand(AICommand command)
	{
		tempPositions = GetFormationPositions(command.destination);
		for(int i=0; i<units.Count; i++)
		{
			if(units.Count > 1)
			{
				//change the position for the command for each unit
				//so they move to a formation position rather than in the exact same place
				command.destination = tempPositions[i];
			}

			units[i].ExecuteCommand(command);
		}
	}

	public void AddUnit(Unit unitToAdd)
	{
		unitToAdd.OnDie += UnitDeadHandler;
		units.Add(unitToAdd);
	}

	//Adds an array of Units to the Platoon, and returns the new length
	public int AddUnits(Unit[] unitsToAdd)
	{
		for(int i=0; i<unitsToAdd.Length; i++)
		{
			AddUnit(unitsToAdd[i]);
		}

		return units.Count;
	}

	//Removes an Unit from the Platoon and returns if the operation was successful
	public bool RemoveUnit(Unit unitToRemove)
	{
		bool isThere = units.Contains(unitToRemove);

		if(isThere)
		{
			units.Remove(unitToRemove);
			unitToRemove.OnDie -= UnitDeadHandler;
		}

		return isThere;
	}

	public void Clear()
	{
		for(int i = 0; i < units.Count; i++)
		{
			units[i].OnDie -= UnitDeadHandler;
		}
		units.Clear();
	}

	//Returns the current position of the units
	public Vector3[] GetCurrentPositions()
	{
		tempPositions = new Vector3[units.Count];

		for(int i=0; i<units.Count; i++)
		{
			tempPositions[i] = units[i].transform.position;
		}

		return tempPositions;
	}

	//Returns an array of positions to be used to send units into a circular formation
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

	//Forces the position of the units. Useful in Edit mode only (Play mode would use the NavMeshAgent)
	public void SetPositions(Vector3[] _newPositions)
	{
		for(int i=0; i<units.Count; i++)
		{
			units[i].transform.position = _newPositions[i];
		}
	}

	//Returns true if all the Units are dead
	public bool CheckIfAllDead()
	{
		bool allDead = true;

		for(int i=0; i<units.Count; i++)
		{
			if(units[i] != null
				&& units[i].state != Unit.UnitState.Dead)
			{
				allDead = false;
				break;
			}
		}

		return allDead;
	}

	//Fired when a unit belonging to this Platoon dies
	private void UnitDeadHandler(Unit whoDied)
	{
		RemoveUnit(whoDied); //will also remove the handler
	}

	private void OnDrawGizmosSelected()
	{
		for(int i=0; i<units.Count; i++)
		{
			if(units[i] != null)
			{
				Gizmos.color = new Color(.8f, .8f, 1f, 1f);
				Gizmos.DrawCube(units[i].transform.position, new Vector3(1f, .1f, 1f));
			}
		}
	}
}