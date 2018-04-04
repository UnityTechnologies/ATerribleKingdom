using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : Singleton<UIManager>
{
	public Image selectionRectangle;
	public Image cameraLockedIcon;

	public TextMeshProUGUI charNameText, dialogueLineText;
	public GameObject toggleSpacebarMessage, dialoguePanel;

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
		selectionRectangle.rectTransform.ForceUpdateRectTransforms();
		selectionRectangle.rectTransform.sizeDelta = new Vector2(rectSize.width, rectSize.height);
	}

	public void SetDialogue(string charName, string lineOfDialogue, int sizeOfDialogue)
	{
		charNameText.SetText(charName);
		dialogueLineText.SetText(lineOfDialogue);
		dialogueLineText.fontSize = sizeOfDialogue;

		ToggleDialoguePanel(true);
	}

	public void TogglePressSpacebarMessage(bool active)
	{
		toggleSpacebarMessage.SetActive(active);
	}

	public void ToggleDialoguePanel(bool active)
	{
		dialoguePanel.SetActive(active);
	}
}
