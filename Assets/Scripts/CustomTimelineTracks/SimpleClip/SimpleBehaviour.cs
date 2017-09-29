using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class SimpleBehaviour : PlayableBehaviour
{
	
	//Example of a clip variable
	public string message;



	//ProcessFrame is like "the Update of Timeline"
	public override void ProcessFrame(Playable playable, FrameData info, object playerData)
	{
		Debug.Log(message);
	}

}