using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TMPTextSwitcherClip : PlayableAsset, ITimelineClipAsset
{
	public TMPTextSwitcherBehaviour template = new TMPTextSwitcherBehaviour ();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
		var playable = ScriptPlayable<TMPTextSwitcherBehaviour>.Create (graph, template);
        return playable;    }
}
