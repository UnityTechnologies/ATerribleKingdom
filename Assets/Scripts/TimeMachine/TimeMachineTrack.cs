using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.7366781f, 0.3261246f, 0.8529412f)]
[TrackClipType(typeof(TimeMachineClip))]
public class TimeMachineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<TimeMachineMixerBehaviour>.Create (graph, inputCount);
    }
}
