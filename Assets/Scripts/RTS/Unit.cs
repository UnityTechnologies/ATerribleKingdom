using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Events;

public class Unit : MonoBehaviour
{
	public UnitState state = UnitState.Idle;
	public UnitTemplate template;

	//references
	private NavMeshAgent navMeshAgent;
	private Animator animator;
	private SpriteRenderer selectionCircle;

	//private bool isSelected; //is the Unit currently selected by the Player
	private Unit targetOfAttack;
	private Unit[] hostiles;
	private float lastGuardCheckTime, guardCheckInterval = 1f;
	private bool isReady = false;

	public UnityAction<Unit> OnDie;

	void Awake ()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
		selectionCircle = transform.Find("SelectionCircle").GetComponent<SpriteRenderer>();

		//Randomization of NavMeshAgent speed. More fun!
		float rndmFactor = navMeshAgent.speed * .15f;
		navMeshAgent.speed += Random.Range(-rndmFactor, rndmFactor);
	}

	private void Start()
	{
		template = Instantiate<UnitTemplate>(template); //we copy the template otherwise it's going to overwrite the original asset!

		//Set some defaults, including the default state
		SetSelected(false);
		Guard();
	}
	
	void Update()
	{
		//Little hack to give time to the NavMesh agent to set its destination.
		//without this, the Unit would switch its state before the NavMeshAgent can kick off, leading to unpredictable results
		if(!isReady)
		{
			isReady = true;
			return;
		}

		switch(state)
		{
			case UnitState.MovingToSpotIdle:
				if(navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance + .1f)
				{
					Stop();
				}
				break;

			case UnitState.MovingToSpotGuard:
				if(navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance + .1f)
				{
					Guard();
				}
				break;

			case UnitState.MovingToTarget:
				//check if target has been killed by somebody else
				if(IsDeadOrNull(targetOfAttack))
				{
					Guard();
				}
				else
				{
					//Check for distance from target
					if(navMeshAgent.remainingDistance < template.engageDistance)
					{
						navMeshAgent.velocity = Vector3.zero;
						StartAttacking();
					}
					else
					{
						navMeshAgent.SetDestination(targetOfAttack.transform.position); //update target position in case it's moving
					}
				}

				break;

			case UnitState.Guarding:
				if(Time.time > lastGuardCheckTime + guardCheckInterval)
				{
					lastGuardCheckTime = Time.time;
					Unit t = GetNearestHostileUnit();
					if(t != null)
					{
						MoveToAttack(t);
					}
				}
				break;
			case UnitState.Attacking:
				//check if target has been killed by somebody else
				if(IsDeadOrNull(targetOfAttack))
				{
					Guard();
				}
				else
				{
					//look towards the target
					Vector3 desiredForward = (targetOfAttack.transform.position - transform.position).normalized;
					transform.forward = Vector3.Lerp(transform.forward, desiredForward, Time.deltaTime * 10f);
				}
				break;
		}

		float navMeshAgentSpeed = navMeshAgent.velocity.magnitude;
		animator.SetFloat("Speed", navMeshAgentSpeed * .05f);
	}

	public void ExecuteCommand(AICommand c)
	{
		if(state == UnitState.Dead)
		{
			//already dead
			return;
		}

		switch(c.commandType)
		{
			case AICommand.CommandType.GoToAndIdle:
				GoToAndIdle(c.destination);
				break;

			case AICommand.CommandType.GoToAndGuard:
				GoToAndGuard(c.destination);
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
		isReady = false;

		navMeshAgent.isStopped = false;
		navMeshAgent.SetDestination(location);
	}

	//move to a position and be guarding
	private void GoToAndGuard(Vector3 location)
	{
		state = UnitState.MovingToSpotGuard;
		targetOfAttack = null;
		isReady = false;

		navMeshAgent.isStopped = false;
		navMeshAgent.SetDestination(location);
	}

	//stop and stay Idle
	private void Stop()
	{
		state = UnitState.Idle;
		targetOfAttack = null;
		isReady = false;

		navMeshAgent.isStopped = true;
		navMeshAgent.velocity = Vector3.zero;
	}

	//stop but watch for enemies nearby
	public void Guard()
	{
		state = UnitState.Guarding;
		targetOfAttack = null;
		isReady = false;

		navMeshAgent.isStopped = true;
		navMeshAgent.velocity = Vector3.zero;
	}

	//move towards a target to attack it
	private void MoveToAttack(Unit target)
	{
		if(!IsDeadOrNull(target))
		{
			state = UnitState.MovingToTarget;
			targetOfAttack = target;
			isReady = false;

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
		if(!IsDeadOrNull(targetOfAttack))
		{
			state = UnitState.Attacking;
			isReady = false;
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
		while(targetOfAttack != null)
		{
			animator.SetTrigger("DoAttack");
			targetOfAttack.SufferAttack(template.attackPower);

			yield return new WaitForSeconds(1f / template.attackSpeed);

			//check is performed after the wait, because somebody might have killed the target in the meantime
			if(IsDeadOrNull(targetOfAttack))
			{
				animator.SetTrigger("InterruptAttack");
				break;

			}

			if(state == UnitState.Dead)
			{
				yield break;
			}

			//Check if the target moved away for some reason
			if(Vector3.Distance(targetOfAttack.transform.position, transform.position) > template.engageDistance)
			{
				MoveToAttack(targetOfAttack);
			}
		}


		//only move into Guard if the attack was interrupted (dead target, etc.)
		if(state == UnitState.Attacking)
		{
			Guard();
		}
	}

	//called by an attacker
	private void SufferAttack(int damage)
	{
		if(state == UnitState.Dead)
		{
			//already dead
			return;
		}

		template.health -= damage;

		if(template.health <= 0)
		{
			template.health = 0;
			Die();
		}
	}

	//called in SufferAttack, but can also be from a Timeline clip
	private void Die()
	{
		state = UnitState.Dead; //still makes sense to set it, because somebody might be interacting with this script before it is destroyed
		animator.SetTrigger("DoDeath");

		//Remove itself from the selection Platoon
		GameManager.Instance.RemoveFromSelection(this);
		SetSelected(false);
		
		//Fire an event so any Platoon containing this Unit will be notified
		if(OnDie != null)
		{
			OnDie(this);
		}

		//To avoid the object participating in any Raycast or tag search
		gameObject.tag = "Untagged";
		gameObject.layer = 0;

		//Remove unneeded Components
		Destroy(selectionCircle);
		Destroy(navMeshAgent);
		Destroy(GetComponent<Collider>()); //will make it unselectable on click
		Destroy(animator, 4f); //give it some time to complete the animation
		Destroy(this);
	}

	private bool IsDeadOrNull(Unit u)
	{
		return (u == null || u.state == UnitState.Dead);
	}

	private Unit GetNearestHostileUnit()
	{
		hostiles = GameObject.FindGameObjectsWithTag(template.GetOtherFaction().ToString()).Select(x => x.GetComponent<Unit>()).ToArray();

		Unit nearestEnemy = null;
		float nearestEnemyDistance = 1000f;
		for(int i=0; i<hostiles.Count(); i++)
		{
			if(IsDeadOrNull(hostiles[i]))
			{
				continue;
			}

			float distanceFromHostile = Vector3.Distance(hostiles[i].transform.position, transform.position);
			if(distanceFromHostile <= template.guardDistance)
			{
				if(distanceFromHostile < nearestEnemyDistance)
				{
					nearestEnemy = hostiles[i];
					nearestEnemyDistance = distanceFromHostile;
				}
			}
		}

		return nearestEnemy;
	}

	public void SetSelected(bool selected)
	{
		//Set transparency dependent on selection
		Color newColor = selectionCircle.color;
		newColor.a = (selected) ? 1f : .3f;
		selectionCircle.color = newColor;
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

	private void OnDrawGizmos()
	{
		if(navMeshAgent != null
			&& navMeshAgent.isOnNavMesh
			&& navMeshAgent.hasPath)
		{
			Gizmos.DrawLine(transform.position, navMeshAgent.destination);
		}
	}
}