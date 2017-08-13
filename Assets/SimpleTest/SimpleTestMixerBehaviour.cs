using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class SimpleTestMixerBehaviour : PlayableBehaviour
{
	Vector3 m_DefaultPosition, previousInputFinalPosition;

	Transform m_TrackBinding;
	bool m_FirstFrameHappened;

    // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
		m_TrackBinding = playerData as Transform;

		if (!m_TrackBinding)
			return;

		if (!m_FirstFrameHappened)
		{
			m_DefaultPosition = m_TrackBinding.position;
			m_FirstFrameHappened = true;
		}

		previousInputFinalPosition = m_DefaultPosition;
        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<SimpleTestBehaviour> inputPlayable = (ScriptPlayable<SimpleTestBehaviour>)playable.GetInput(i);
            SimpleTestBehaviour input = inputPlayable.GetBehaviour ();

			if(inputWeight > 0f)
			{
				double progress = inputPlayable.GetTime()/inputPlayable.GetDuration();
				m_TrackBinding.position = Vector3.Lerp(previousInputFinalPosition, input.finalPosition, (float)progress);

				continue;
			}
			else
			{
				previousInputFinalPosition = input.finalPosition; //cached to act as initial position for the next input
			}
        }
    }

	public override void OnGraphStop (Playable playable)
	{
		m_FirstFrameHappened = false;

		if (m_TrackBinding == null)
			return;

		m_TrackBinding.position = m_DefaultPosition;
	}
}
