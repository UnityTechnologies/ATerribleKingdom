using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace DefaultPlayables
{
	[TrackColor(0.1394896f, 0.4411765f, 0.3413077f)]
	[TrackClipType(typeof(TMPTextSwitcherClip))]
	[TrackBindingType(typeof(TextMeshProUGUI))]
	public class TMPTextSwitcherTrack : TrackAsset
	{
	    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
	    {
			return ScriptPlayable<TMPTextSwitcherMixerBehaviour>.Create (graph, inputCount);
	    }

	    public override void GatherProperties (PlayableDirector director, IPropertyCollector driver)
	    {
	#if UNITY_EDITOR
			TextMeshProUGUI trackBinding = director.GetGenericBinding(this) as TextMeshProUGUI;
	        if (trackBinding == null)
	            return;

	        var serializedObject = new UnityEditor.SerializedObject (trackBinding);
	        var iterator = serializedObject.GetIterator();
	        while (iterator.NextVisible(true))
	        {
	            if (iterator.hasVisibleChildren)
	                continue;

				driver.AddFromName<TextMeshProUGUI>(trackBinding.gameObject, iterator.propertyPath);
	        }
	#endif
	        base.GatherProperties (director, driver);
	    }
	}
}