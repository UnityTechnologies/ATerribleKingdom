using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : Singleton<CameraManager>
{
	public Transform gameplayDummy;
	public CinemachineTargetGroup targetGroup;

	private bool isFramingPlatoon;
	public bool IsFramingPlatoon { get{return isFramingPlatoon;} } //from the outside it's read-only

	private void Awake()
	{
		
	}

	public void MoveGameplayCamera(Vector2 amount)
	{
		gameplayDummy.Translate(amount.x, 0f, amount.y, Space.World);
	}

	public void SetPlatoonFramingMode(bool enable)
	{
		//Only change it if it's different
		if(isFramingPlatoon != enable)
		{
			//Force the value to its opposite, and then call the TogglePlatoonFramingMode
			//will achieve the effect and trigger all the necessary changes
			isFramingPlatoon = !enable;
			TogglePlatoonFramingMode();
		}
	}

	public void TogglePlatoonFramingMode()
	{
		isFramingPlatoon = !isFramingPlatoon;
		if(isFramingPlatoon)
		{
			if(GameManager.Instance.GetSelectionLength() > 0)
			{
				Transform[] allTargets = GameManager.Instance.GetSelectionTransforms();
				targetGroup.m_Targets = new CinemachineTargetGroup.Target[allTargets.Length]; //reset the targets
				for(int i = 0; i < allTargets.Length; i++)
				{
					targetGroup.m_Targets[i].target = allTargets[i];
					targetGroup.m_Targets[i].weight = 1f;
					targetGroup.m_Targets[i].radius = 1f;
				}
			}
		}
		else
		{
			targetGroup.m_Targets = new CinemachineTargetGroup.Target[1]; //reset the targets to only the gameplay dummy
			targetGroup.m_Targets[0].target = gameplayDummy;
			targetGroup.m_Targets[0].weight = 1f;
			targetGroup.m_Targets[0].radius = 1f;
		}
	}

	private void Update()
	{
		if(isFramingPlatoon)
		{
			//If we're framing a group, keep the gameplayDummy position in sync with the targetGroup
			//so when the camera stops following the group, there's no jump in position
			gameplayDummy.localPosition = targetGroup.transform.localPosition;
		}
	}

	/*public Vector2 GetVCamDeadZone()
	{
		CinemachineComposer composer = gameplayVCam.GetCinemachineComponent<CinemachineComposer>();
		return new Vector2(composer.m_SoftZoneWidth, composer.m_SoftZoneHeight);
	}*/
}
