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
        return ScriptPlayable<AICommandMixerBehaviour>.Create (graph, inputCount);
    }
}
