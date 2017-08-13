using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class AICommandBehaviour : PlayableBehaviour
{
    public Vector3 targetPosition;
	[HideInInspector]
	public bool destinationSet = false;
}
