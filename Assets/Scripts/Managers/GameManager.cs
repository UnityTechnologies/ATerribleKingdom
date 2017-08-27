using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : Singleton<GameManager>
{
	private Platoon selectedPlatoon;

	private void Awake()
	{
		selectedPlatoon = GetComponent<Platoon>();
		Cursor.lockState = CursorLockMode.Confined;
	}

	public void IssueCommand(AICommand cmd)
	{
		selectedPlatoon.ExecuteCommand(cmd);
	}

	public int GetSelectionLength()
	{
		return selectedPlatoon.units.Count;
	}

	public void AddToSelection(Unit[] newSelectedUnits, bool clearPrevious = true)
	{
		if(clearPrevious)
			ClearSelection();
		
		selectedPlatoon.AddUnits(newSelectedUnits);
		for(int i = 0; i < newSelectedUnits.Length; i++)
		{
			newSelectedUnits[i].SetSelected(true);
		}
	}

	public void AddToSelection(Unit newSelectedUnit, bool clearPrevious = true)
	{
		if(clearPrevious)
			ClearSelection();

		selectedPlatoon.AddUnit(newSelectedUnit);
		newSelectedUnit.SetSelected(true);
	}

	public void RemoveFromSelection(Unit u)
	{
		selectedPlatoon.RemoveUnit(u);
		u.SetSelected(false);
	}

	public void ClearSelection()
	{
		for(int i = 0; i < selectedPlatoon.units.Count; i++)
		{
			selectedPlatoon.units[i].SetSelected(false);
		}

		selectedPlatoon.Clear();
	}

	public void SentSelectedUnitsTo(Vector3 pos)
	{
		AICommand newCommand = new AICommand(AICommand.CommandType.GoToAndGuard, pos);
		IssueCommand(newCommand);
	}

	public void AttackTarget(Unit tgtUnit)
	{
		AICommand newCommand = new AICommand(AICommand.CommandType.AttackTarget, tgtUnit);
		IssueCommand(newCommand);
	}

	public Unit[] GetAllSelectableUnits()
	{
		return GameObject.FindGameObjectsWithTag("Locals").Select(x => x.GetComponent<Unit>()).ToArray();
	}
}
