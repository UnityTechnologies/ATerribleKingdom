using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class Unit : MonoBehaviour
{
	public int health = 10;
	public int attackPower = 2;
	public float attackSpeed = 1f; //1 second/attackSpeed = time for a single attack
	public float engageDistance = 1f;
	public UnitState state = UnitState.Idle;

	//references
	private NavMeshAgent navMeshAgent;
	private Animator animator;

	private Unit targetOfAttack;

	void Awake ()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
	}
	
	void Update ()
	{
		switch(state)
		{
			case UnitState.MovingToSpot:
				if(navMeshAgent.remainingDistance < .1f)
				{
					navMeshAgent.velocity = Vector3.zero;
				}
				break;

			case UnitState.MovingToTarget:
				if(navMeshAgent.remainingDistance < engageDistance)
				{
					navMeshAgent.velocity = Vector3.zero;
					StartAttacking();
				}
				else
				{
					navMeshAgent.SetDestination(targetOfAttack.transform.position); //update target position in case it's moving
				}
				break;
		}

		float navMeshAgentSpeed = navMeshAgent.velocity.magnitude;
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

			case AICommand.CommandType.AttackTarget:
				MoveToAttack(c.target);
				break;
			
			case AICommand.CommandType.Die:
				Die();
				break;
		}
	}
		
	private void GoTo(Vector3 location, bool andStop = false)
	{
		state = UnitState.MovingToSpot;
		targetOfAttack = null;
		navMeshAgent.isStopped = false;
		navMeshAgent.SetDestination(location);
	}

	private void Stop()
	{
		state = UnitState.Idle;
		targetOfAttack = null;
		navMeshAgent.isStopped = true;
		navMeshAgent.velocity = Vector3.zero;
	}

	private void MoveToAttack(Unit target)
	{
		state = UnitState.MovingToTarget;
		targetOfAttack = target;
		navMeshAgent.isStopped = false;
		navMeshAgent.SetDestination(target.transform.position);
	}

	private void StartAttacking()
	{
		state = UnitState.Attacking;
		navMeshAgent.isStopped = true;
		StartCoroutine(DealAttack());
	}

	private IEnumerator DealAttack()
	{
		while(targetOfAttack != null) //TODO: check for other exit conditions, such as this unit is dead
		{
			animator.SetTrigger("DoAttack");
			bool isDead = targetOfAttack.SufferAttack(attackPower);
			Debug.Log("DealAttack | isDead: " + isDead);
			
			if(isDead)
			{
				Debug.Log("Stop Coroutine");
				break;
			}
			
			yield return new WaitForSeconds(1f / attackSpeed);
		}
	}

	private bool SufferAttack(int damage)
	{
		health -= damage;
		if(health <= 0)
		{
			Die();
		}

		return health <= 0;
	}

	private void Die()
	{
		state = UnitState.Dead;
		animator.SetTrigger("DoDeath");
	}

	public enum UnitState
	{
		Idle,
		Guarding,
		Attacking,
		MovingToTarget,
		MovingToSpot,
		Dead,
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

	public enum CommandType
	{
		GoToAndIdle,
		GoToAndGuard,
		AttackTarget, //attacks a specific target, then becomes Guarding
		Stop,
		//Flee,
		Die,
	}
}