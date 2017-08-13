using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace UnityEngine.Timeline
{
	[Serializable]
    public class VideoScriptPlayableAsset : PlayableAsset
	{
        public ExposedReference<VideoPlayer> videoPlayer;

        [SerializeField, NotKeyable]
		public VideoClip videoClip;

        [SerializeField, NotKeyable]
        public bool mute = false;

        [SerializeField, NotKeyable]
        public bool loop = true;

        [SerializeField, NotKeyable]
        public double preloadTime = 0.3;

        [SerializeField, NotKeyable]
        public double clipInTime = 0.0;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
		{
            ScriptPlayable<VideoPlayableBehaviour> playable =
                ScriptPlayable<VideoPlayableBehaviour>.Create(graph);

            VideoPlayableBehaviour playableBehaviour = playable.GetBehaviour();

            playableBehaviour.videoPlayer = videoPlayer.Resolve(graph.GetResolver());
            playableBehaviour.videoClip = videoClip;
            playableBehaviour.mute = mute;
            playableBehaviour.loop = loop;
            playableBehaviour.preloadTime = preloadTime;
            playableBehaviour.clipInTime = clipInTime;

            return playable;
		}
	}
}
