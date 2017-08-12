using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	private List<Unit> selectedUnits;

	public int GetSelectionLength()
	{
		return selectedUnits.Count;
	}

	public void AddToSelection(List<Unit> newSelectedUnits, bool clearPrevious = true)
	{
		if(clearPrevious)
			ClearSelection();
		
		selectedUnits.AddRange(newSelectedUnits);
	}

	public void AddToSelection(Unit newSelectedUnit, bool clearPrevious = true)
	{
		if(clearPrevious)
			ClearSelection();

		selectedUnits.Add(newSelectedUnit);
	}

	public void RemoveFromSelection(Unit u)
	{
		selectedUnits.Remove(u);
	}

	public void ClearSelection()
	{
		selectedUnits.Clear();
	}
}
