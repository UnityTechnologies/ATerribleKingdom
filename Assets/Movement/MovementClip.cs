using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class MovementClip : PlayableAsset, ITimelineClipAsset
{
    public MovementBehaviour template = new MovementBehaviour ();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<MovementBehaviour>.Create (graph, template);
        return playable;
    }
}
