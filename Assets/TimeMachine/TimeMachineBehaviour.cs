using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeMachineBehaviour : PlayableBehaviour
{
	public TimeMachineClipType type;
    public Platoon platoon;
    public string labelToJumpTo;

	public enum TimeMachineClipType
	{
		Marker,
		Rewind,
		Pause,
	}
}
