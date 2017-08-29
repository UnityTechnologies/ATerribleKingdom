using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
	public Image selectionRectangle;
	public Image cameraLockedIcon;

	private void Start()
	{
		selectionRectangle.enabled = false;
		cameraLockedIcon.enabled = false;
	}

	public void ToggleSelectionRectangle(bool active)
	{
		selectionRectangle.enabled = active;
	}

	public void ToggleCameraLockedIcon(bool active)
	{
		cameraLockedIcon.enabled = active;
	}

	public void SetSelectionRectangle(Rect rectSize)
	{
		selectionRectangle.rectTransform.position = rectSize.center;
		selectionRectangle.rectTransform.sizeDelta = new Vector2(rectSize.width, rectSize.height);
	}

}
