using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimeMachineMixerBehaviour : PlayableBehaviour
{
	public Dictionary<string, double> markerClips;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
		//ScriptPlayable<TimeMachineBehaviour> inputPlayable = (ScriptPlayable<TimeMachineBehaviour>)playable.GetInput(i);
		//Debug.Log(PlayableExtensions.GetTime<ScriptPlayable<TimeMachineBehaviour>>(inputPlayable));

		if(!Application.isPlaying)
		{
			return;
		}

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
					case TimeMachineBehaviour.TimeMachineClipType.Pause:
						Debug.Log("Pause");
						(playable.GetGraph().GetResolver() as PlayableDirector).Pause();
						break;

					case TimeMachineBehaviour.TimeMachineClipType.JumpToTime:
					case TimeMachineBehaviour.TimeMachineClipType.JumpToMarker:
						if(input.ConditionMet())
						{
							//Rewind
							if(input.clipType == TimeMachineBehaviour.TimeMachineClipType.JumpToTime)
							{
								//Jump to time
								(playable.GetGraph().GetResolver() as PlayableDirector).time = (double)input.timeToJumpTo;
							}
							else
							{
								//Jump to marker
								double t = markerClips[input.labelToJumpTo];
								(playable.GetGraph().GetResolver() as PlayableDirector).time = t;
							}
						}
						break;
						
				}
			}
        }
    }
}
