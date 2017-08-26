using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
	public Image selectionRectangle;

	public void SetSelectionRectangle(Vector2 topLeftCoords, Vector2 bottomRightCoords)
	{
		//Vector2 rectCentre = new Vector2((topLeftCoords.x + bottomRightCoords.x) * .5f, (topLeftCoords.y + bottomRightCoords.y) * .5f);
		Rect rectSize = Rect.MinMaxRect(topLeftCoords.x, topLeftCoords.y, bottomRightCoords.x, bottomRightCoords.y);

		selectionRectangle.rectTransform.position = rectSize.center;
		selectionRectangle.rectTransform.sizeDelta = new Vector2(rectSize.width, rectSize.height);

		//Debug.Log("Mouse: " + Input.mousePosition);
		//Debug.Log("Centre: " + rectSize.center);
	}

}
