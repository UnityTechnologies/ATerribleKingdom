using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : Singleton<CameraManager>
{
	public Transform gameplayDummy;
	//public CinemachineVirtualCamera gameplayVCam; //unused

	//private CinemachineBrain CMBrain; //unused

	private void Awake()
	{
		//CMBrain = GetComponentInChildren<CinemachineBrain>();
	}

	public void MoveGameplayCamera(Vector2 amount)
	{
		gameplayDummy.Translate(amount.x, 0f, amount.y, Space.World);
	}

	/*public Vector2 GetVCamDeadZone()
	{
		CinemachineComposer composer = gameplayVCam.GetCinemachineComponent<CinemachineComposer>();
		return new Vector2(composer.m_SoftZoneWidth, composer.m_SoftZoneHeight);
	}*/
}
