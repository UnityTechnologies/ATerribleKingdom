using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeMachineClip : PlayableAsset, ITimelineClipAsset
{
    public TimeMachineBehaviour template = new TimeMachineBehaviour ();
    public ExposedReference<Platoon> platoon;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TimeMachineBehaviour>.Create (graph, template);
        TimeMachineBehaviour clone = playable.GetBehaviour ();
        clone.platoon = platoon.Resolve (graph.GetResolver ());
        return playable;
    }
}
