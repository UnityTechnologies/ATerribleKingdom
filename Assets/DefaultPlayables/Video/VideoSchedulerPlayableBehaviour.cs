using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.Timeline
{
    public sealed class VideoSchedulerPlayableBehaviour : PlayableBehaviour
    {
		private IEnumerable<TimelineClip> m_Clips;
        private PlayableDirector m_Director;

        internal PlayableDirector director
        {
            get { return m_Director; }
            set { m_Director = value; }
        }

        internal IEnumerable<TimelineClip> clips
        {
            get { return m_Clips; }
            set { m_Clips = value; }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (m_Clips == null)
                return;

            int inputPort = 0;
            foreach (TimelineClip clip in m_Clips)
            {
				ScriptPlayable<VideoPlayableBehaviour> scriptPlayable =
					(ScriptPlayable<VideoPlayableBehaviour>)playable.GetInput(inputPort);

				VideoPlayableBehaviour videoPlayableBehaviour = scriptPlayable.GetBehaviour();

				if (videoPlayableBehaviour != null)
				{
					double preloadTime = Math.Max(0.0, videoPlayableBehaviour.preloadTime);
					if (m_Director.time >= clip.start + clip.duration ||
						m_Director.time <= clip.start - preloadTime)
						videoPlayableBehaviour.StopVideo();
					else if (m_Director.time > clip.start - preloadTime)
						videoPlayableBehaviour.PrepareVideo();
				}
					
                ++inputPort;
            }
        }
	}
}
