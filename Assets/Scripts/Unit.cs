using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
	private NavMeshAgent navMeshAgent;
	private Animator animator;

	public enum UnitState
	{
		Idle,
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
		if(navMeshAgentSpeed > 0f)
		{
			if(navMeshAgent.remainingDistance < .1f)
			{
				navMeshAgent.velocity = Vector3.zero;
			}
			animator.SetFloat("Speed", navMeshAgentSpeed * .05f);
		}

	}

	public void GoTo(Vector3 location)
	{
		Debug.Log("Moving to " + location);
		navMeshAgent.destination = location;
	}
}
