using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.Timeline
{
	[Serializable]
    [TrackClipType(typeof(VideoScriptPlayableAsset))]
    [TrackMediaType(TimelineAsset.MediaType.Script)]
    [TrackColor(0.008f, 0.698f, 0.655f)]
    public class VideoScriptPlayableTrack : TrackAsset
	{
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            PlayableDirector playableDirector = go.GetComponent<PlayableDirector>();

            ScriptPlayable<VideoSchedulerPlayableBehaviour> playable =
                ScriptPlayable<VideoSchedulerPlayableBehaviour>.Create(graph, inputCount);

            VideoSchedulerPlayableBehaviour videoSchedulerPlayableBehaviour =
                   playable.GetBehaviour();

            if (videoSchedulerPlayableBehaviour != null)
            {
                videoSchedulerPlayableBehaviour.director = playableDirector;
                videoSchedulerPlayableBehaviour.clips = GetClips();
            }

            return playable;
        }
    }
}

