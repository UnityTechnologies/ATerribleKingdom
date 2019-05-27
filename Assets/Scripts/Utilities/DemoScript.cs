using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour
{
	public GameObject testUnits, testMonsters;
	public GameObject flashbackTimeline, stormTimeline, aicommandTimeline;
	public GameObject dialoguePanel;


	void Update ()
	{
		//Gameplay
		if(Input.GetKeyDown(KeyCode.Alpha0))
		{
			testUnits.SetActive(true);
			testMonsters.SetActive(true);

			flashbackTimeline.SetActive(false);
			stormTimeline.SetActive(false);
			aicommandTimeline.SetActive(false);

			dialoguePanel.SetActive(false);
		}

		//Flashback timeline
		if(Input.GetKeyDown(KeyCode.Alpha1))
		{
			testUnits.SetActive(false);
			testMonsters.SetActive(false);

			flashbackTimeline.SetActive(true);
			stormTimeline.SetActive(false);
			aicommandTimeline.SetActive(false);

			//dialoguePanel.SetActive(false);
		}

		//Storm Timeline
		if(Input.GetKeyDown(KeyCode.Alpha2))
		{
			testUnits.SetActive(false);
			testMonsters.SetActive(false);

			flashbackTimeline.SetActive(false);
			stormTimeline.SetActive(true);
			aicommandTimeline.SetActive(false);

			dialoguePanel.SetActive(false);
		}

		//Platoons Timeline
		if(Input.GetKeyDown(KeyCode.Alpha3))
		{
			testUnits.SetActive(false);
			testMonsters.SetActive(false);

			flashbackTimeline.SetActive(false);
			stormTimeline.SetActive(false);
			aicommandTimeline.SetActive(true);

			dialoguePanel.SetActive(false);
		}
	}
}
