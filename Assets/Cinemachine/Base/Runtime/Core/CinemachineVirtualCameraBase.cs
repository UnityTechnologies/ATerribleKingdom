using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Base class for a Monobehaviour that represents a Virtual Camera within the Unity scene.
    ///
    /// This is intended to be attached to an empty Transform GameObject.
    /// Inherited classes can be either standalone virtual cameras such
    /// as CinemachineVirtualCamera, or meta-cameras such as
    /// CinemachineClearShot or CinemachineFreeLook.
    ///
    /// A CinemachineVirtualCameraBase exposes a Priority property.  When the behaviour is
    /// enabled in the game, the Virtual Camera is automatically placed in a queue
    /// maintained by the static CinemachineCore singleton.
    /// The queue is sorted by priority.  When a Unity camera is equipped with a
    /// CinemachineBrain behaviour, the brain will choose the camera
    /// at the head of the queue.  If you have multiple Unity cameras with CinemachineBrain
    /// behaviours (say in a split-screen context), then you can filter the queue by
    /// setting the culling flags on the virtual cameras.  The culling mask of the
    /// Unity Camera will then act as a filter for the brain.  Apart from this,
    /// there is nothing that prevents a virtual camera from controlling multiple
    /// Unity cameras simultaneously.
    /// </summary>
    [SaveDuringPlay]
    public abstract class CinemachineVirtualCameraBase : MonoBehaviour, ICinemachineCamera
    {
        /// <summary>This is deprecated.  It is here to support the soon-to-be-removed
        /// Cinemachine Debugger in the Editor.</summary>
        [HideInInspector, NoSaveDuringPlay]
        public Action CinemachineGUIDebuggerCallback = null;

        /// <summary>Inspector control - Use for hiding sections of the Inspector UI.</summary>
        [HideInInspector, SerializeField, NoSaveDuringPlay]
        public bool m_HideHeaderInInspector;

        /// <summary>Inspector control - Use for hiding sections of the Inspector UI.</summary>
        [HideInInspector, SerializeField, NoSaveDuringPlay]
        public string[] m_ExcludedPropertiesInInspector = new string[] { "m_Script" };

        /// <summary>Inspector control - Use for enabling sections of the Inspector UI.</summary>
        [HideInInspector, SerializeField, NoSaveDuringPlay]
        public CinemachineCore.Stage[] m_LockStageInInspector;

        /// <summary>The priority will determine which camera becomes active based on the
        /// state of other cameras and this camera.  Higher numbers have greater priority.
        /// </summary>
        [NoSaveDuringPlay]
        [Tooltip("The priority will determine which camera becomes active based on the state of other cameras and this camera.  Higher numbers have greater priority.")]
        public int m_Priority = 10;

        /// <summary>
        /// A delegate to hook into the state calculation pipeline.
        /// This will be called after each pipeline stage, to allow others to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// </summary>
        /// <param name="d">The delegate to call.</param>
        public virtual void AddPostPipelineStageHook(OnPostPipelineStageDelegate d)
        {
            OnPostPipelineStage -= d;
            OnPostPipelineStage += d;
        }

        /// <summary>Remove a Pipeline stage hook callback.</summary>
        /// <param name="d">The delegate to remove.</param>
        public virtual void RemovePostPipelineStageHook(OnPostPipelineStageDelegate d)
        {
            OnPostPipelineStage -= d;
        }

        /// <summary>
        /// A delegate to hook into the state calculation pipeline.
        /// Implementaion must be sure to call this after each pipeline stage, to allow
        /// other services to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// </summary>
        protected OnPostPipelineStageDelegate OnPostPipelineStage;

        /// <summary>
        /// A delegate to hook into the state calculation pipeline.
        /// This will be called after each pipeline stage, to allow other
        /// services to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// 
        /// Parameters:
        /// 
        /// * CinemachineVirtualCameraBase vcam: the virtual camera being updated
        /// * CinemachineCore.Stage stage: what stage in the pipeline has just been updated
        /// * ref CameraState newState: the current state of the vcam
        /// * float deltaTime: the frame timestep.  0 or -1 means "don't consider the previous frame"
        /// </summary>
        public delegate void OnPostPipelineStageDelegate(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState newState, float deltaTime);

        /// <summary>Get the name of the Virtual Camera.  Base implementation
        /// returns the owner GameObject's name.</summary>
        public string Name { get { return name; } }

        /// <summary>Get the Priority of the virtual camera.  This determines its placement
        /// in the CinemachineCore's queue of eligible shots.</summary>
        public int Priority { get { return m_Priority; } set { m_Priority = value; } }

        /// <summary>The GameObject owner of the Virtual Camera behaviour.</summary>
        public GameObject VirtualCameraGameObject
        {
            get
            {
                if (this == null)
                    return null; // object deleted
                return gameObject;
            }
        }

        /// <summary>The CameraState object holds all of the information
        /// necessary to position the Unity camera.  It is the output of this class.</summary>
        public abstract CameraState State { get; }

        /// <summary>Just returns self.</summary>
        public virtual ICinemachineCamera LiveChildOrSelf { get { return this; } }

        /// <summary>Support for meta-virtual-cameras.  This is the situation where a
        /// virtual camera is in fact the public face of a private army of virtual cameras, which
        /// it manages on its own.  This method gets the VirtualCamera owner, if any.
        /// Private armies are implemented as Transform children of the parent vcam.</summary>
        public ICinemachineCamera ParentCamera
        {
            get
            {
                if (!mSlaveStatusUpdated || !Application.isPlaying)
                    UpdateSlaveStatus();
                return m_parentVcam;
            }
        }

        /// <summary>Check whether the vcam a live child of this camera.  
        /// This base class implementation always returns false.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public virtual bool IsLiveChild(ICinemachineCamera vcam) { return false; }

        /// <summary>Get the LookAt target for the Aim component in the CinemachinePipeline.</summary>
        public abstract Transform LookAt { get; set; }

        /// <summary>Get the Follow target for the Body component in the CinemachinePipeline.</summary>
        public abstract Transform Follow { get; set; }

        /// <summary>Set this to force the next update to ignore deltaTime and reset itself</summary>
        public bool PreviousStateIsValid { get; set; }

        /// <summary>Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  
        /// Do not call this method.  Let the framework do it at the appropriate time</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public abstract void UpdateCameraState(Vector3 worldUp, float deltaTime);

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        public virtual void OnTransitionFromCamera(ICinemachineCamera fromCam) {}

        /// <summary>Support for opaque post-processing module.  
        /// Called from each vcam's OnEnable.</summary>
        internal static CinemachineBrain.VcamEvent sPostProcessingOnEnableHook = new CinemachineBrain.VcamEvent();

        /// <summary>Support for opaque post-processing module</summary>
        internal Component PostProcessingComponent { get; set; }

        /// <summary>Base class implementation does nothing.</summary>
        protected virtual void Start()
        {
        }

        /// <summary>Base class implementation removes the virtual camera from the priority queue.</summary>
        protected virtual void OnDestroy()
        {
            CinemachineCore.Instance.RemoveActiveCamera(this);
        }

        /// <summary>Enforce bounds for fields, when changed in inspector.</summary>
        protected virtual void OnValidate()
        {
        }

        /// <summary>Base class implementation adds the virtual camera from the priority queue.</summary>
        protected virtual void OnEnable()
        {
            // Sanity check - if another vcam component is enabled, shut down
            var vcamComponents = GetComponents<CinemachineVirtualCameraBase>();
            if (vcamComponents.Length > 1)
            {
                foreach (var vcam in vcamComponents)
                {
                    if (vcam.enabled && vcam != this)
                    {
                        Debug.LogError(Name
                            + " has multiple CinemachineVirtualCameraBase-derived components.  Disabling "
                            + GetType().Name + ".");
                        enabled = false;
                    }
                }
            }
            if (sPostProcessingOnEnableHook != null)
                sPostProcessingOnEnableHook.Invoke(this);
            UpdateSlaveStatus();
            UpdatePriorityQueueStatus();    // Add to queue
            PreviousStateIsValid = false;
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnDisable()
        {
            UpdatePriorityQueueStatus();    // Remove from queue
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void Update()
        {
            if (m_Priority != m_QueuePriority)
                UpdatePriorityQueueStatus();
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnTransformParentChanged()
        {
            UpdateSlaveStatus();
            UpdatePriorityQueueStatus();
        }

#if UNITY_EDITOR
        /// <summary>Support for the deprecated CinemachineDebugger.</summary>
        protected virtual void OnGUI()
        {
            if (CinemachineGUIDebuggerCallback != null)
                CinemachineGUIDebuggerCallback();
        }
#endif
        private bool mSlaveStatusUpdated = false;
        private CinemachineVirtualCameraBase m_parentVcam = null;

        private void UpdateSlaveStatus()
        {
            // Look for the first CinemachineVirtualCameraBase ancestor
            mSlaveStatusUpdated = true;
            for (Transform p = transform.parent; p != null; p = p.parent)
            {
                CinemachineVirtualCameraBase cam = p.GetComponent<CinemachineVirtualCameraBase>();
                if (cam != null)
                {
                    m_parentVcam = cam;
                    return;
                }
            }
            m_parentVcam = null;
        }

        /// <summary>Returns this vcam's LookAt target, or if that is null, will retrun
        /// the parent vcam's LookAt target.</summary>
        /// <param name="localLookAt">This vcam's LookAt value.</param>
        /// <returns>The same value, or the parent's if null and a parent exists.</returns>
        protected Transform ResolveLookAt(Transform localLookAt)
        {
            Transform lookAt = localLookAt;
            if (lookAt == null && ParentCamera != null)
                lookAt = ParentCamera.LookAt; // Parent provides default
            return lookAt;
        }

        /// <summary>Returns this vcam's Follow target, or if that is null, will retrun
        /// the parent vcam's Follow target.</summary>
        /// <param name="localFollow">This vcam's Follow value.</param>
        /// <returns>The same value, or the parent's if null and a parent exists.</returns>
        protected Transform ResolveFollow(Transform localFollow)
        {
            Transform follow = localFollow;
            if (follow == null && ParentCamera != null)
                follow = ParentCamera.Follow; // Parent provides default
            return follow;
        }

        private int m_QueuePriority = int.MaxValue;
        private void UpdatePriorityQueueStatus()
        {
            if (m_parentVcam != null || !isActiveAndEnabled)
            {
                CinemachineCore.Instance.RemoveActiveCamera(this);
                m_QueuePriority = int.MaxValue;
            }
            else
            {
                CinemachineCore.Instance.AddActiveCamera(this);
                m_QueuePriority = m_Priority;
            }
        }
    }
}
