using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AICommandMixerBehaviour : PlayableBehaviour
{
	private Platoon trackBinding;
	private bool firstFrameHappened = false;
	private Vector3[] defaultPositions, newPositions, finalPositions, previousInputFinalPositions;
	private int lastInputPlayed = -1;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
		trackBinding = playerData as Platoon;

        if (trackBinding == null)
			return;

		if (!firstFrameHappened)
		{
			defaultPositions = trackBinding.GetCurrentPositions();
			firstFrameHappened = true;
		}

		if(Application.isPlaying)
		{
			ProcessPlayModeFrame(playable);
		}
		else
		{
			ProcessEditModeFrame(playable);
		}
    }

	private void ProcessEditModeFrame(Playable playable)
	{
		previousInputFinalPositions = defaultPositions;
		int inputCount = playable.GetInputCount();
		int unitCount = trackBinding.units.Count;

		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			ScriptPlayable<AICommandBehaviour> inputPlayable = (ScriptPlayable<AICommandBehaviour>)playable.GetInput(i);
			AICommandBehaviour input = inputPlayable.GetBehaviour();

			//force the finalPosition to the attack target in case of an Attack action
			if(input.actionType == AICommand.CommandType.AttackTarget
				&& input.targetUnit != null)
			{
				input.targetPosition = input.targetUnit.transform.position;
			}

			//create an array of final positions for the entire Platoon
			finalPositions = trackBinding.GetFormationPositions(input.targetPosition);

			if(inputWeight > 0f)
			{
				double progress = inputPlayable.GetTime()/inputPlayable.GetDuration();
				newPositions = new Vector3[unitCount];
				for(int j=0; j<unitCount; j++)
				{
					newPositions[j] = Vector3.Lerp(previousInputFinalPositions[j], finalPositions[j], (float)progress);
				}
				trackBinding.SetPositions(newPositions);

				continue;
			}
			else
			{
				previousInputFinalPositions = finalPositions; //cached to act as initial position for the next input
			}
		}
	}

	private void ProcessPlayModeFrame(Playable playable)
	{
		int inputCount = playable.GetInputCount();

		for(int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			ScriptPlayable<AICommandBehaviour> inputPlayable = (ScriptPlayable<AICommandBehaviour>)playable.GetInput(i);
			AICommandBehaviour input = inputPlayable.GetBehaviour();

			//Make the Unit script execute the command
			if(inputWeight > 0f)
			{
				if(lastInputPlayed != i
				   && !input.commandExecuted)
				{
					AICommand c = new AICommand(input.actionType, input.targetPosition, input.targetUnit);
					trackBinding.ExecuteCommand(c);
					input.commandExecuted = true;
				}
			}
		}
	}

	public override void OnGraphStop (Playable playable)
	{
		if(!Application.isPlaying)
		{
			firstFrameHappened = false;
			
			if (trackBinding == null)
				return;
			
			trackBinding.SetPositions(defaultPositions);
		}
	}
}
