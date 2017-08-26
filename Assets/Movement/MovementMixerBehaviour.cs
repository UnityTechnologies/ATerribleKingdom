using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class MovementMixerBehaviour : PlayableBehaviour
{
    Vector3 m_DefaultPosition;

    Transform m_TrackBinding;
    bool m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_TrackBinding = playerData as Transform;

        if (m_TrackBinding == null)
            return;

        if (!m_FirstFrameHappened)
        {
            m_DefaultPosition = m_TrackBinding.position;
            m_FirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount ();

        Vector3 blendedPosition = Vector3.zero;
        float totalWeight = 0f;
        float greatestWeight = 0f;
        int currentInputs = 0;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<MovementBehaviour> inputPlayable = (ScriptPlayable<MovementBehaviour>)playable.GetInput(i);
            MovementBehaviour input = inputPlayable.GetBehaviour ();
            
            blendedPosition += input.position * inputWeight;
            totalWeight += inputWeight;

            if (inputWeight > greatestWeight)
            {
                greatestWeight = inputWeight;
            }

            if (!Mathf.Approximately (inputWeight, 0f))
                currentInputs++;
        }

        m_TrackBinding.position = blendedPosition + m_DefaultPosition * (1f - totalWeight);
    }

    public override void OnGraphStop (Playable playable)
    {
        m_FirstFrameHappened = false;

        if (m_TrackBinding == null)
            return;

        m_TrackBinding.position = m_DefaultPosition;
    }
}
