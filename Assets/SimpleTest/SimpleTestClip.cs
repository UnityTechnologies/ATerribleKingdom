using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class SimpleTestClip : PlayableAsset, ITimelineClipAsset
{
    public SimpleTestBehaviour template = new SimpleTestBehaviour ();

    public ClipCaps clipCaps
    {
		get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<SimpleTestBehaviour>.Create (graph, template);
        SimpleTestBehaviour clone = playable.GetBehaviour();
        return playable;
    }
}
