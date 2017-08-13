using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class Unit : MonoBehaviour
{
	public int health = 10;
	[Tooltip("Damage dealt each attack")]
	public int attackPower = 2;
	[Tooltip("The attack rate. The higher, the faster the Unit is in attacking. 1 second/attackSpeed = time it takes for a single attack")]
	public float attackSpeed = 1f;
	[Tooltip("When it has reached this distance from its target, the Unit stops and attacks it")]
	public float engageDistance = 1f;
	[Tooltip("When guarding, if any enemy enters this range it will be attacked")]
	public float guardDistance = 5f;

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
			case UnitState.MovingToSpotIdle:
				if(navMeshAgent.remainingDistance < .1f)
				{
					Stop();
				}
				break;

			case UnitState.MovingToSpotGuard:
				if(navMeshAgent.remainingDistance < .1f)
				{
					Guard();
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

			case UnitState.Guarding:
				//TODO: look for enemies in range
				//use guardDistance
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
				GoToAndIdle(c.destination);
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
		
	//move to a position and be idle
	private void GoToAndIdle(Vector3 location)
	{
		state = UnitState.MovingToSpotIdle;
		targetOfAttack = null;
		navMeshAgent.isStopped = false;
		navMeshAgent.SetDestination(location);
	}

	//move to a position and be guarding
	private void GoToAndGuard(Vector3 location)
	{
		state = UnitState.MovingToSpotGuard;
		targetOfAttack = null;
		navMeshAgent.isStopped = false;
		navMeshAgent.SetDestination(location);
	}

	//stop and stay Idle
	private void Stop()
	{
		state = UnitState.Idle;
		targetOfAttack = null;
		navMeshAgent.isStopped = true;
		navMeshAgent.velocity = Vector3.zero;
	}

	//stop but watch for enemies nearby
	public void Guard()
	{
		state = UnitState.Guarding;
		targetOfAttack = null;
		navMeshAgent.isStopped = true;
		navMeshAgent.velocity = Vector3.zero;
	}

	//move towards a target to attack it
	private void MoveToAttack(Unit target)
	{
		if(target.state != UnitState.Dead)
		{
			state = UnitState.MovingToTarget;
			targetOfAttack = target;
			navMeshAgent.isStopped = false;
			navMeshAgent.SetDestination(target.transform.position);
		}
		else
		{
			//if the command is dealt by a Timeline, the target might be already dead
			Guard();
		}
	}

	//reached the target (within engageDistance), time to attack
	private void StartAttacking()
	{
		//somebody might have killed the target while this Unit was approaching it
		if(targetOfAttack.state != UnitState.Dead)
		{
			state = UnitState.Attacking;
			navMeshAgent.isStopped = true;
			StartCoroutine(DealAttack());
		}
		else
		{
			Guard();
		}
	}

	//the single blows
	private IEnumerator DealAttack()
	{
		while(targetOfAttack != null) //TODO: check for other exit conditions, such as this unit is dead
		{
			animator.SetTrigger("DoAttack");
			bool isDead = targetOfAttack.SufferAttack(attackPower);

			yield return new WaitForSeconds(1f / attackSpeed);

			//check is performed after the wait, because somebody might have killed the target in the meantime
			if(isDead)
			{
				break;
			}
		}

		Guard();
	}

	//called by an attacker
	private bool SufferAttack(int damage)
	{
		if(state == UnitState.Dead)
		{
			//already dead
			return true;
		}

		health -= damage;

		if(health <= 0)
		{
			health = 0;
			Die();
		}

		return health == 0;
	}

	//called in SufferAttack, but can also be from a Timeline clip
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
		MovingToSpotIdle,
		MovingToSpotGuard,
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