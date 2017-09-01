using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class SimpleClip : PlayableAsset, ITimelineClipAsset
{
	public SimpleBehaviour template = new SimpleBehaviour ();

	public ClipCaps clipCaps
	{
		get { return ClipCaps.Blending; }
	}

	public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
	{
		var playable = ScriptPlayable<SimpleBehaviour>.Create (graph, template);
		return playable;
	}
}
