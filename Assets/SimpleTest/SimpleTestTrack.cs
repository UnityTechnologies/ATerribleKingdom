using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.855f, 0.666f, 0.759f)]
[TrackClipType(typeof(SimpleTestClip))]
[TrackBindingType(typeof(Transform))]
public class SimpleTestTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<SimpleTestMixerBehaviour>.Create (graph, inputCount);
    }
}
