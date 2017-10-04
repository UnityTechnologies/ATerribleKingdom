using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningStrike : MonoBehaviour {

	public AudioClip[] thunders;

	void Start ()
	{
		AudioSource audioSource = GetComponent<AudioSource>();
		audioSource.clip = thunders[Random.Range(0, 2)];
		audioSource.pitch = Random.Range(.9f, 1.1f);
		audioSource.Play();
	}
	

}
