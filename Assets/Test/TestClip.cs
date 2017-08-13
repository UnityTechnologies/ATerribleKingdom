using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TestClip : PlayableAsset, ITimelineClipAsset
{
    public TestBehaviour template = new TestBehaviour ();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TestBehaviour>.Create (graph, template);
        return playable;
    }
}
