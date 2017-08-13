using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class Unit : MonoBehaviour
{
	private NavMeshAgent navMeshAgent;
	private Animator animator;

	public enum UnitState
	{
		Idle,
		Guarding,
		Attacking,
		Moving,
		Dead,
	}
	public UnitState state = UnitState.Idle;

	void Awake ()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
	}
	
	void Update ()
	{
		float navMeshAgentSpeed = navMeshAgent.velocity.magnitude;
		if(navMeshAgent.remainingDistance < .1f)
		{
			navMeshAgent.velocity = Vector3.zero;
		}
		animator.SetFloat("Speed", navMeshAgentSpeed * .05f);

	}

	public void ExecuteCommand(AICommand c)
	{
		switch(c.commandType)
		{
			case AICommand.CommandType.GoToAndIdle:
			case AICommand.CommandType.GoToAndGuard:
				GoTo(c.destination);
				break;

			case AICommand.CommandType.Stop:
				Stop();
				break;
		}
	}

	private void GoTo(Vector3 location)
	{
		Debug.Log("Moving to " + location);
		navMeshAgent.SetDestination(location);
	}

	private void Stop()
	{
		navMeshAgent.isStopped = true;
	}
}

[Serializable]
public class SelectionGroup
{
	public List<Unit> units;

	public void ExecuteCommand(AICommand command)
	{
		for(int i=0; i<units.Count; i++)
		{
			units[i].ExecuteCommand(command);
		}
	}
}

[Serializable]
public class AICommand
{
	public enum CommandType
	{
		GoToAndIdle,
		GoToAndGuard,
		AttackTarget, //attacks a specific target, then becomes Guarding
		Stop,
	}
	public CommandType commandType;

	public Vector3 destination;
	public Unit target;

	public AICommand(CommandType ty, Vector3 v, Unit ta)
	{
		commandType = ty;
		destination = v;
		target = ta;
	}

	public AICommand(CommandType ty, Vector3 v)
	{
		commandType = ty;
		destination = v;
	}

	public AICommand(CommandType ty, Unit ta)
	{
		commandType = ty;
		target = ta;
	}

	public AICommand(CommandType ty)
	{
		commandType = ty;
	}
}