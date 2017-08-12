using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
	private NavMeshAgent navMeshAgent;

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
	}
	
	void Update ()
	{
		
	}

	public void GoTo(Vector3 location)
	{
		navMeshAgent.destination = location;
	}
}
