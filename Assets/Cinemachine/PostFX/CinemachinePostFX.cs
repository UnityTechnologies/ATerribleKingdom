using UnityEngine;

// NOTE: If you are getting errors of the sort that say something like:
//     "The type or namespace name `PostProcessing' does not exist in the namespace"
// it is because you are missing the PostProcessing module.  
//
// To make the errors go away, you can either: 
//   1 - Download PostProcessing and install it into your project, and take advantage of its features,
// or
//   2 - Delete or comment out this file.
//

namespace Cinemachine.PostFX
{
    /// <summary>
    /// This behaviour connects Cinemachine with the PostProcessing module.  
    /// It is used in 2 ways.  
    /// 
    /// <para>1. As a component on the Unity Camera: it serves as the liaison
    /// between the camera's CinemachineBrain and the camera's PostProcessing behaviour.
    /// It listens for camera Cut events and resets the PostProcessing stack when they occur.
    /// If you are using PostProcessing, then you should add this behaviour to your
    /// camera alongside the CinemachineBrain, always.</para>
    /// 
    /// <para>2. As a component on the Virtual Camera: In this capacity, it holds
    /// a PostProcessing Profile asset that will be applied to the camera whenever the Virtual
    /// camera is live.  It also has the (temporary) optional functionality of animating
    /// the FocusDistance and DepthOfField properties of the CameraState, and
    /// applying them to the current PostProcessing profile.</para>
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class CinemachinePostFX : MonoBehaviour
    {
        // Just for the Enabled checkbox
        void Update() {}

        /// <summary>When this behaviour is on a Unity Camera, this setting is the default PostProcessing
        /// profile for the camera, and will be applied whenever it is not overridden by a virtual camera.
        /// When the behaviour is on a virtual camera, then this is the PostProcessing profile that will 
        /// become active whenever this virtual camera is live.</summary>
        [Tooltip("The post-processing profile that will become active whenever this camera is live")]
        public UnityEngine.PostProcessing.PostProcessingProfile m_Profile;

        /// <summary>If checked, then the FocusDistance will be set to the virtual camera's 
        /// distance from the LookAt target.</summary>
        [Tooltip("If checked, then the FocusDistance will be set to the distance from the LookAt target.")]
        public bool m_FocusTracksTarget;

        /// <summary>Offset from target distance, to be used with FollowFocus.</summary>
        [Tooltip("Offset from target distance, to be used with FollowFocus.")]
        public float m_FocusOffset;

        // These are used if this behaviour is on a Unity Camera
        CinemachineBrain mBrain;
        UnityEngine.PostProcessing.PostProcessingBehaviour mPostProcessingBehaviour;

        void Awake()
        {
            // If I am a component on the camera, connect to its brain 
            // and to its post-processing behaviour
            mBrain = GetComponent<CinemachineBrain>();
            if (mBrain != null)
            {
                mBrain.PostFXHandler += PostFXHandler;
                mBrain.m_CameraCutEvent.AddListener(OnCameraCut);
            }

            // Must have one of these if connected to a brain
            mPostProcessingBehaviour = GetComponent<UnityEngine.PostProcessing.PostProcessingBehaviour>();
            if (mPostProcessingBehaviour == null && mBrain != null)
                mPostProcessingBehaviour = gameObject.AddComponent<UnityEngine.PostProcessing.PostProcessingBehaviour>();
        }

        void OnDestroy()
        {
            if (mBrain != null)
            {
                mBrain.PostFXHandler -= PostFXHandler;
                mBrain.m_CameraCutEvent.RemoveListener(OnCameraCut);
            }
        }

        // CinemachineBrain callback used when this behaviour is on the Unity Camera
        bool PostFXHandler(Camera dest, ICinemachineCamera vcam, CameraState state)
        {
            if (enabled && mBrain != null && mPostProcessingBehaviour != null)
            {
                // Look for the vcam's PostFX behaviour
                CinemachinePostFX postFX = (vcam == null) ? this : GetEffectivePstFX(vcam);
                if (postFX != null)
                {
                    if (postFX.m_Profile != null)
                    {
                        // Adjust the focus distance
                        UnityEngine.PostProcessing.DepthOfFieldModel.Settings dof 
                            = postFX.m_Profile.depthOfField.settings;
                        if (postFX.m_FocusTracksTarget && state.HasLookAt)
                            dof.focusDistance = (state.FinalPosition - state.ReferenceLookAt).magnitude
                                + postFX.m_FocusOffset;
                        postFX.m_Profile.depthOfField.settings = dof;
                    }
                    // Apply the profile
                    if (mPostProcessingBehaviour.profile != postFX.m_Profile)
                    {
                        mPostProcessingBehaviour.profile = postFX.m_Profile;
                        mPostProcessingBehaviour.ResetTemporalEffects();
                    }
                    return true;
                }
            }
            return false;
        }

        CinemachinePostFX GetEffectivePstFX(ICinemachineCamera vcam)
        {
            while (vcam != null && vcam.LiveChildOrSelf != vcam)
                vcam = vcam.LiveChildOrSelf;
            CinemachinePostFX postFX = null;
            while (vcam != null && postFX == null)
            {
                postFX = vcam.VirtualCameraGameObject.GetComponent<CinemachinePostFX>();
                if (postFX != null && !postFX.enabled)
                    postFX = null;
                vcam = vcam.ParentCamera;
            }
            return postFX;
        }

        void OnCameraCut()
        {
            //Debug.Log("CinemachinePostFX.OnCameraCut()");
            if (mPostProcessingBehaviour != null)
                mPostProcessingBehaviour.ResetTemporalEffects();
        }
    }    
}
