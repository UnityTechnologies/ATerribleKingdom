using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class AICommandBehaviour : PlayableBehaviour
{
	public Unit targetUnit;
    public Vector3 targetPosition;
	public AICommand.CommandType actionType;

	[HideInInspector]
	public bool commandExecuted = false; //the user shouldn't author this, the Mixer does
}
