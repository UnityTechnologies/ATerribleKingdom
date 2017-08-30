using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class LightMixerBehaviour : PlayableBehaviour
{
    Color m_DefaultColor;
    float m_DefaultIntensity;

    Light m_TrackBinding;
    bool m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        m_TrackBinding = playerData as Light;

        if (m_TrackBinding == null)
            return;

        if (!m_FirstFrameHappened)
        {
            m_DefaultColor = m_TrackBinding.color;
            m_DefaultIntensity = m_TrackBinding.intensity;
            m_FirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount ();

        Color blendedColor = Color.clear;
        float blendedIntensity = 0f;
        float totalWeight = 0f;
        float greatestWeight = 0f;
        int currentInputs = 0;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<LightBehaviour> inputPlayable = (ScriptPlayable<LightBehaviour>)playable.GetInput(i);
            LightBehaviour input = inputPlayable.GetBehaviour();
            
            blendedColor += input.color * inputWeight;
            blendedIntensity += input.intensity * inputWeight;
            totalWeight += inputWeight;

            if (inputWeight > greatestWeight)
            {
                greatestWeight = inputWeight;
            }

            if (!Mathf.Approximately (inputWeight, 0f))
                currentInputs++;
        }

        m_TrackBinding.color = blendedColor + m_DefaultColor * (1f - totalWeight);
        m_TrackBinding.intensity = blendedIntensity + m_DefaultIntensity * (1f - totalWeight);
    }

	public override void OnPlayableDestroy(Playable playable)
    {
        m_FirstFrameHappened = false;

        if (m_TrackBinding == null)
            return;

        m_TrackBinding.color = m_DefaultColor;
        m_TrackBinding.intensity = m_DefaultIntensity;
    }
}
