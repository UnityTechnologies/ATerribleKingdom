using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
	public Image selectionRectangle;

	private void Awake()
	{
		selectionRectangle.enabled = false;
	}

	public void ToggleSelectionRectangle(bool active)
	{
		selectionRectangle.enabled = active;
	}

	public void SetSelectionRectangle(Rect rectSize)
	{
		selectionRectangle.rectTransform.position = rectSize.center;
		selectionRectangle.rectTransform.sizeDelta = new Vector2(rectSize.width, rectSize.height);
	}

}
