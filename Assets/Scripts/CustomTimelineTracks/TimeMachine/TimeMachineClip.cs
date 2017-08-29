using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeMachineClip : PlayableAsset, ITimelineClipAsset
{
	[HideInInspector]
    public TimeMachineBehaviour template = new TimeMachineBehaviour ();

	public TimeMachineBehaviour.TimeMachineClipType clipType;
	public TimeMachineBehaviour.ConditionType condition;
	public string labelToJumpTo = "", markerLabel = "";
	public float timeToJumpTo = 0f;

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
		clone.labelToJumpTo = labelToJumpTo;
		clone.clipType = clipType;
		clone.condition = condition;
		clone.markerLabel = markerLabel;
		clone.timeToJumpTo = timeToJumpTo;

        return playable;
    }
}
