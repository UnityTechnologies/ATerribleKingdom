using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeMachineBehaviour : PlayableBehaviour
{
	public TimeMachineClipType clipType;
    public Platoon platoon;
    public string labelToJumpTo;

	[HideInInspector]
	public bool clipExecuted = false; //the user shouldn't author this, the Mixer does

	public enum TimeMachineClipType
	{
		Marker,
		Rewind,
		Pause,
	}
}
