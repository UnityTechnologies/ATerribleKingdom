using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : Singleton<CameraManager>
{
	public Transform gameplayDummy;
	public CinemachineBrain cmBrain;
	public CinemachineFreeLook dummyFreeLook;
	public CinemachineTargetGroup targetGroup;

	private CinemachineFreeLook groupFreeLook;

	private bool isFramingPlatoon = false;
	public bool IsFramingPlatoon { get{return isFramingPlatoon;} } //from the outside it's read-only

	private void Start()
	{
		//Instantiate a copy of the FreeLook VCam pointing at the dummy,
		//and use it to point at the group
		groupFreeLook = Instantiate<GameObject>(dummyFreeLook.gameObject).GetComponent<CinemachineFreeLook>();
		groupFreeLook.name = "FreeLook TargetGroup";
		groupFreeLook.transform.SetParent(this.transform, true);
		groupFreeLook.LookAt = targetGroup.transform;
		groupFreeLook.Follow = targetGroup.transform;
		groupFreeLook.Priority = 0;
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
				//set the list of targets in the TargetGroup to the selected Units
				Transform[] allTargets = GameManager.Instance.GetSelectionTransforms();
				targetGroup.m_Targets = new CinemachineTargetGroup.Target[allTargets.Length]; //reset the targets list
				for(int i = 0; i < allTargets.Length; i++)
				{
					targetGroup.m_Targets[i].target = allTargets[i];
					targetGroup.m_Targets[i].weight = 1f;
					targetGroup.m_Targets[i].radius = 1f;
				}

				//take the current "zoom level" (y axis) from the dummy camera to the group one
				groupFreeLook.m_YAxis = dummyFreeLook.m_YAxis;

				//set camera priorities, the CMBrain will do the transition
				dummyFreeLook.Priority = 0;
				groupFreeLook.Priority = 100;
			}
		}
		else
		{
			targetGroup.m_Targets = new CinemachineTargetGroup.Target[0]; //reset the targets to nothing

			//take the current "zoom level" (y axis) from the group camera to the dummy one
			groupFreeLook.m_YAxis = dummyFreeLook.m_YAxis;

			//set camera priorities, the CMBrain will do the transition
			groupFreeLook.Priority = 0;
			dummyFreeLook.Priority = 100;

			//Move the dummy around to avoid any snap in position when going from targetGroup to dummy
			CinemachineBlend blend = cmBrain.ActiveBlend;
			if(blend != null)
			{
				//a blend is in progress, we get the in-between position
				gameplayDummy.localPosition = Vector3.Lerp(gameplayDummy.localPosition, targetGroup.transform.localPosition, blend.BlendWeight);
			}
			else
			{
				//No blending happening, so we bring the dummy exactly to the target group
				gameplayDummy.localPosition = targetGroup.transform.localPosition;
			}
		}

		//Visualise the small camera locked icon in the top-left corner
		UIManager.Instance.ToggleCameraLockedIcon(isFramingPlatoon);
	}
}