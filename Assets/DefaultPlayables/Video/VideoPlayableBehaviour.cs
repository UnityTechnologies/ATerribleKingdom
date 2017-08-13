using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace UnityEngine.Timeline
{
	public class VideoPlayableBehaviour : PlayableBehaviour
    {
        public VideoPlayer videoPlayer;
		public VideoClip videoClip;
        public bool mute = false;
        public bool loop = true;
        public double preloadTime = 0.3;
        public double clipInTime = 0.0;

        private bool playedOnce = false;
        private bool preparing = false;

        public void PrepareVideo()
        {
            if (videoPlayer == null || videoClip == null)
                return;

            videoPlayer.targetCameraAlpha = 0.0f;

            if (videoPlayer.clip != videoClip)
                StopVideo();

            if (videoPlayer.isPrepared || preparing)
                return;

            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
		    videoPlayer.isLooping = loop;

            for (ushort i = 0; i < videoClip.audioTrackCount; ++i)
            {
                if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                    videoPlayer.SetDirectAudioMute(i, mute || !Application.isPlaying);
                else if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
                {
                    AudioSource audioSource = videoPlayer.GetTargetAudioSource(i);
                    if (audioSource != null)
                        audioSource.mute = mute || !Application.isPlaying;
                }
            }

            videoPlayer.loopPointReached += LoopPointReached;
            videoPlayer.time = clipInTime;
            videoPlayer.Prepare();
            preparing = true;
        }

        void LoopPointReached(VideoPlayer vp)
        {
            playedOnce = !loop;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
		{
			if (videoPlayer == null || videoClip == null)
				return;

            videoPlayer.timeReference = Application.isPlaying ? VideoTimeReference.ExternalTime :
                                                                VideoTimeReference.Freerun;
																
		    if (videoPlayer.isPlaying && Application.isPlaying)
			    videoPlayer.externalReferenceTime = playable.GetTime();
            else if (!Application.isPlaying)
                SyncVideoToPlayable(playable);
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (videoPlayer == null)
                return;

            if (!playedOnce)
            {
                PlayVideo();
                SyncVideoToPlayable(playable);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (videoPlayer == null)
                return;

            if (Application.isPlaying)
                PauseVideo();
            else
                StopVideo();
        }

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			if (videoPlayer == null || videoPlayer.clip == null)
				return;

            videoPlayer.targetCameraAlpha = info.weight;

		    if (Application.isPlaying)
		    {
		        for (ushort i = 0; i < videoPlayer.clip.audioTrackCount; ++i)
		        {
		            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
		                videoPlayer.SetDirectAudioVolume(i, info.weight);
		            else if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
		            {
		                AudioSource audioSource = videoPlayer.GetTargetAudioSource(i);
		                if (audioSource != null)
		                    audioSource.volume = info.weight;
		            }
		        }
		    }
		}

		public override void OnGraphStart(Playable playable)
		{
		    playedOnce = false;
		}

		public override void OnGraphStop(Playable playable)
		{
		    if (!Application.isPlaying)
		        StopVideo();
		}

        public override void OnPlayableDestroy(Playable playable)
        {
            StopVideo();
        }

        public void PlayVideo()
        {
            if (videoPlayer == null)
                return;

            videoPlayer.Play();
            preparing = false;

            if (!Application.isPlaying)
                PauseVideo();
        }

        public void PauseVideo()
        {
            if (videoPlayer == null)
                return;

            videoPlayer.Pause();
            preparing = false;
        }

        public void StopVideo()
        {
            if (videoPlayer == null)
                return;

            playedOnce = false;
            videoPlayer.Stop();
            preparing = false;
        }

        private void SyncVideoToPlayable(Playable playable)
        {
            if (videoPlayer == null || videoPlayer.clip == null)
                return;

			videoPlayer.time = (clipInTime + (playable.GetTime() * videoPlayer.playbackSpeed)) % videoPlayer.clip.length;
        }
    }
}
