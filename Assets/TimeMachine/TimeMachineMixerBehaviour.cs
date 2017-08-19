using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimeMachineMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        int inputCount = playable.GetInputCount ();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<TimeMachineBehaviour> inputPlayable = (ScriptPlayable<TimeMachineBehaviour>)playable.GetInput(i);
            TimeMachineBehaviour input = inputPlayable.GetBehaviour ();
            
			/*
			if(inputWeight > 0f)
			{
				

				switch(input.type)
				{
					case TimeMachineBehaviour.TimeMachineClipType.Rewind:
						if(input.platoon != null)
						{
							bool allDead = input.platoon.CheckIfAllDead();
							if(!allDead)
							{
								//Rewind
								//TODO: rewind
							}
						}
						break;

					case TimeMachineBehaviour.TimeMachineClipType.Pause:
						playable.GetGraph().Stop();
						break;
				}
			}
			*/
        }
    }
}
