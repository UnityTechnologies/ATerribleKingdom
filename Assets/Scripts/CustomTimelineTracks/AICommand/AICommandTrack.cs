using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0f, 0.4866645f, 1f)]
[TrackClipType(typeof(AICommandClip))]
[TrackBindingType(typeof(Platoon))]
public class AICommandTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
		foreach (var c in GetClips())
		{
			//Clips are renamed after the actionType of the clip itself
			AICommandClip clip = (AICommandClip)c.asset;
			c.displayName = clip.commandType.ToString();
		}

        return ScriptPlayable<AICommandMixerBehaviour>.Create (graph, inputCount);
    }
}
