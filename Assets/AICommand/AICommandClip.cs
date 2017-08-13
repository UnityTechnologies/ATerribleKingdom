using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class AICommandClip : PlayableAsset, ITimelineClipAsset
{
	public Vector3 targetPosition;
	[HideInInspector]
    public AICommandBehaviour template = new AICommandBehaviour ();

    public ClipCaps clipCaps
    {
		get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<AICommandBehaviour>.Create(graph, template);
		AICommandBehaviour clone = playable.GetBehaviour();
		clone.targetPosition = targetPosition;
        return playable;
    }
}
