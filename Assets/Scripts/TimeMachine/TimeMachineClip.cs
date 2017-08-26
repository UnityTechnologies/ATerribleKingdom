using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeMachineClip : PlayableAsset, ITimelineClipAsset
{
	//[HideInInspector]
    public TimeMachineBehaviour template = new TimeMachineBehaviour ();

	public TimeMachineBehaviour.TimeMachineClipType clipType;
	public string labelToJumpTo;
	public ExposedReference<Platoon> platoon;

	public TrackAsset track;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TimeMachineBehaviour>.Create (graph, template);
        TimeMachineBehaviour clone = playable.GetBehaviour ();
        clone.platoon = platoon.Resolve (graph.GetResolver ());
		clone.labelToJumpTo = labelToJumpTo;
		clone.clipType = clipType;
        return playable;
    }
}
