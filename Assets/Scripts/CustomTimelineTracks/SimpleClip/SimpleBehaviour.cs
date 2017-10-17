using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class SimpleBehaviour : PlayableBehaviour
{
	//Example of a variable that is going to be unique per-clip
	public string message;

	//ProcessFrame is like "the Update of Timeline"
	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		//Insert logic per frame in here
		Debug.Log(message);
	}
}