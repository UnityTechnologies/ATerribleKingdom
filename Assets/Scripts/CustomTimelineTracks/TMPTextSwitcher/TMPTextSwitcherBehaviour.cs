using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TMPTextSwitcherBehaviour : PlayableBehaviour
{
    public Color color = Color.white;
    public float fontSize = 14;
    public string text;
}
