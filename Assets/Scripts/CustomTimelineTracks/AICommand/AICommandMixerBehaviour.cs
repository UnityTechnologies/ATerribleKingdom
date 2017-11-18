using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AICommandMixerBehaviour : PlayableBehaviour
{
	private Platoon trackBinding;
	private bool firstFrameHappened = false;
	private Vector3[] defaultPositions, newPositions, finalPositions, previousInputFinalPositions;

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

		//different behaviour depending if Unity is in Play mode or not,
		//because NavMeshAgent is not available in Edit mode
		if(Application.isPlaying)
		{
			ProcessPlayModeFrame(playable);
		}
		else
		{
			ProcessEditModeFrame(playable);
		}
    }

	//Happens every frame in Edit mode.
	//Uses transform.position of the units to approximate what they would do in Play mode with the NavMeshAgent
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

			//Some actionTypes have special needs
			switch(input.commandType)
			{
				case AICommand.CommandType.Die:
				case AICommand.CommandType.Stop:
					//Do nothing if it's a Die or Stop action
					continue; //Will skip to the next input clip in the for loop above

				case AICommand.CommandType.AttackTarget:
					//Force the finalPosition to the attack target in case of an Attack action
					if(input.targetUnit != null)
					{
						input.targetPosition = input.targetUnit.transform.position;
					}
					break;
			}

			//Create an array of final positions for the entire Platoon
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

	//Happens in Play mode
	//Uses the NavMeshAgent to control the units, delegating their movement and animations to the AI
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
				if(!input.commandExecuted)
				{
					AICommand c = new AICommand(input.commandType, input.targetPosition, input.targetUnit);
					trackBinding.ExecuteCommand(c);
					input.commandExecuted = true; //this prevents the command to be executed every frame of this clip
				}
			}
		}
	}

	public override void OnPlayableDestroy(Playable playable)
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
