using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Playables;

public class GameManager : Singleton<GameManager>
{
	public GameMode gameMode = GameMode.Gameplay;

	private Platoon selectedPlatoon;
	private PlayableDirector activeDirector;

	private void Awake()
	{
		selectedPlatoon = GetComponent<Platoon>();
		Cursor.lockState = CursorLockMode.Confined;
		#if UNITY_EDITOR
		Application.targetFrameRate = 30; //just to keep things "smooth" during presentations
		#endif
	}

	public void IssueCommand(AICommand cmd)
	{
		selectedPlatoon.ExecuteCommand(cmd);
	}

	public int GetSelectionLength()
	{
		return selectedPlatoon.units.Count;
	}

	public Transform[] GetSelectionTransforms()
	{
		return selectedPlatoon.units.Select(x => x.transform).ToArray();
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

		if(CameraManager.Instance.IsFramingPlatoon)
		{
			CameraManager.Instance.SetPlatoonFramingMode(false);
		}
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

	//Called by the TimeMachine Clip (of type Pause)
	public void PauseTimeline(PlayableDirector whichOne)
	{
		activeDirector = whichOne;
		activeDirector.playableGraph.GetRootPlayable(0).SetSpeed(0d);
		gameMode = GameMode.DialogueMoment; //InputManager will be waiting for a spacebar to resume
		UIManager.Instance.TogglePressSpacebarMessage(true);
	}

	//Called by the InputManager
	public void ResumeTimeline()
	{
		UIManager.Instance.TogglePressSpacebarMessage(false);
		UIManager.Instance.ToggleDialoguePanel(false);
		activeDirector.playableGraph.GetRootPlayable(0).SetSpeed(1d);
		gameMode = GameMode.Gameplay;
	}

	public enum GameMode
	{
		Gameplay,
		//Cutscene,
		DialogueMoment, //waiting for input
	}
}
