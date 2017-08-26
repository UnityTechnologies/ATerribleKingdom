using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimeMachineMixerBehaviour : PlayableBehaviour
{
	private float[] markerTimes;

	public override void OnGraphStart(Playable playable)
	{
		base.OnGraphStart(playable);

		int inputCount = playable.GetInputCount();
		markerTimes = new float[inputCount];

		for(int i = 0; i < inputCount; i++)
		{
			ScriptPlayable<TimeMachineBehaviour> inputPlayable = (ScriptPlayable<TimeMachineBehaviour>)playable.GetInput(i);
			Debug.Log(PlayableExtensions.GetTime<ScriptPlayable<TimeMachineBehaviour>>(inputPlayable));
		}
	}

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<TimeMachineBehaviour> inputPlayable = (ScriptPlayable<TimeMachineBehaviour>)playable.GetInput(i);
            TimeMachineBehaviour input = inputPlayable.GetBehaviour();
            
			if(inputWeight > 0f)
			{
				switch(input.clipType)
				{
					case TimeMachineBehaviour.TimeMachineClipType.Rewind:
						if(input.platoon != null)
						{
							bool allDead = input.platoon.CheckIfAllDead();
							if(allDead)
							{
								//Rewind
								Debug.Log((playable.GetGraph().GetResolver() as PlayableDirector).time);
								(playable.GetGraph().GetResolver() as PlayableDirector).time = 2d;
							}
						}
						break;

					case TimeMachineBehaviour.TimeMachineClipType.Pause:
						playable.GetGraph().Stop();
						break;
				}
			}
        }
    }
}
