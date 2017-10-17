using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class SimpleClip : PlayableAsset, ITimelineClipAsset
{
	public SimpleBehaviour template = new SimpleBehaviour ();

	//Necessary function to pair the Clip with the Behaviour
	public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
	{
		var playable = ScriptPlayable<SimpleBehaviour>.Create(graph, template);
		return playable;
	}

	//Defines clip characteristics such as blending, extrapolation, looping, etc.
	public ClipCaps clipCaps
	{
		get { return ClipCaps.None; }
	}
}