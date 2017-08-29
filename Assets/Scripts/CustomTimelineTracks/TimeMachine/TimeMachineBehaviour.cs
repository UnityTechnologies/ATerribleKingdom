using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeMachineBehaviour : PlayableBehaviour
{
	public TimeMachineClipType clipType;
	public ConditionType condition;
	public string labelToJumpTo, markerLabel;
	public float timeToJumpTo;
    public Platoon platoon;

	[HideInInspector]
	public bool clipExecuted = false; //the user shouldn't author this, the Mixer does

	public bool ConditionMet()
	{
		switch(condition)
		{
			case ConditionType.Always:
				return true;
				
			case ConditionType.PlatoonIsAlive:
				//The Timeline will jump to the label or time if a specific Platoon still has at least 1 unit alive
				if(platoon != null)
				{
					return !platoon.CheckIfAllDead();
				}
				else
				{
					return false;
				}

			case ConditionType.Never:
			default:
				return false;
		}
	}

	public enum TimeMachineClipType
	{
		Marker,
		JumpToTime,
		JumpToMarker,
		Pause,
	}

	public enum ConditionType
	{
		Always,
		Never,
		PlatoonIsAlive,
	}
}
