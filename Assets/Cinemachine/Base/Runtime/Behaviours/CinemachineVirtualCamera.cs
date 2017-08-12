using Cinemachine.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// This behaviour is intended to be attached to an empty Transform GameObject, 
    /// and it represents a Virtual Camera within the Unity scene.
    /// 
    /// The Virtual Camera will animate its Transform according to the rules contained
    /// in its CinemachineComponent pipeline (Aim, Body, and Noise).  When the virtual
    /// camera is Live, the Unity camera will assume the position and orientation
    /// of the virtual camera.
    /// 
    /// A virtual camera is not a camera. Instead, it can be thought of as a camera controller,
    /// not unlike a cameraman. It can drive the Unity Camera and control its position, 
    /// orientation, lens settings, and PostProcessing effects. Each Virtual Camera owns 
    /// its own Cinemachine Component Pipeline, through which you provide the instructions 
    /// for dynamically tracking specific game objects. 
    /// 
    /// A virtual camera is very lightweight, and does no rendering of its own. It merely 
    /// tracks interesting GameObjects, and positions itself accordingly. A typical game 
    /// can have dozens of virtual cameras, each set up to follow a particular character 
    /// or capture a particular event. 
    /// 
    /// A Virtual Camera can be in any of three states: 
    /// 
    /// * **Live**: The virtual camera is actively controlling the Unity Camera. The 
    /// virtual camera is tracking its targets and being updated every frame. 
    /// * **Standby**: The virtual camera is tracking its targets and being updated 
    /// every frame, but no Unity Camera is actively being controlled by it. This is 
    /// the state of a virtual camera that is enabled in the scene but perhaps at a 
    /// lower priority than the Live virtual camera. 
    /// * **Disabled**: The virtual camera is present but disabled in the scene. It is 
    /// not actively tracking its targets and so consumes no processing power. However, 
    /// the virtual camera can be made live from the Timeline. 
    /// 
    /// The Unity Camera can be driven by any virtual camera in the scene. The game 
    /// logic can choose the virtual camera to make live by manipulating the virtual 
    /// cameras' enabled flags and their priorities, based on game logic. 
    ///
    /// In order to be driven by a virtual camera, the Unity Camera must have a CinemachineBrain 
    /// behaviour, which will select the most eligible virtual camera based on its priority 
    /// or on other criteria, and will manage blending. 
    /// </summary>
    /// <seealso cref="CinemachineVirtualCameraBase"/>
    /// <seealso cref="LensSettings"/>
    /// <seealso cref="CinemachineComposer"/>
    /// <seealso cref="CinemachineTransposer"/>
    /// <seealso cref="CinemachineBasicMultiChannelPerlin"/>
    [DocumentationSorting(1, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode, DisallowMultipleComponent]
    [AddComponentMenu("Cinemachine/CinemachineVirtualCamera")]
    public class CinemachineVirtualCamera : CinemachineVirtualCameraBase
    {
        /// <summary>The object that the camera wants to look at (the Aim target).
        /// The Aim component of the CinemachineComponent pipeline
        /// will refer to this target and orient the vcam in accordance with rules and
        /// settings that are provided to it.
        /// If this is null, then the vcam's Transform orientation will be used.</summary>
        [Tooltip("The object that the camera wants to look at (the Aim target).  If this is null, then the vcam's Transform orientation will define the camera's orientation.")]
        public Transform m_LookAt = null;

        /// <summary>The object that the camera wants to move with (the Body target).
        /// The Body component of the CinemachineComponent pipeline
        /// will refer to this target and position the vcam in accordance with rules and
        /// settings that are provided to it.
        /// If this is null, then the vcam's Transform position will be used.</summary>
        [Tooltip("The object that the camera wants to move with (the Body target).  If this is null, then the vcam's Transform position will define the camera's position.")]
        public Transform m_Follow = null;

        /// <summary>Specifies the LensSettings of this Virtual Camera.
        /// These settings will be transferred to the Unity camera when the vcam is live.</summary>
        [FormerlySerializedAs("m_LensAttributes")]
        [Tooltip("Specifies the lens properties of this Virtual Camera.  This generally mirrors the Unity Camera's lens settings, and will be used to drive the Unity camera when the vcam is active.")]
        [LensSettingsProperty]
        public LensSettings m_Lens = LensSettings.Default;

        /// <summary>This is the name of the hidden GameObject that will be created as a child object
        /// of the virtual camera.  This hidden game object acts as a container for the polymorphic
        /// CinemachineComponent pipeline.  The Inspector UI for the Virtual Camera
        /// provides access to this pipleline, as do the CinemachineComponent-family of
        /// public methods in this class.
        /// The lifecycle of the pipeline GameObject is managed automatically.</summary>
        public const string PipelineName = "cm";

        /// <summary>The CameraState object holds all of the information
        /// necessary to position the Unity camera.  It is the output of this class.</summary>
        override public CameraState State { get { return m_State; } }

        /// <summary>Get the LookAt target for the Aim component in the CinemachinePipeline.
        /// If this vcam is a part of a meta-camera collection, then the owner's target
        /// will be used if the local target is null.</summary>
        override public Transform LookAt
        {
            get { return ResolveLookAt(m_LookAt); }
            set
            {
                if (m_LookAt != value)
                    PreviousStateInvalid = true;
                m_LookAt = value;
            }
        }

        /// <summary>Get the Follow target for the Body component in the CinemachinePipeline.
        /// If this vcam is a part of a meta-camera collection, then the owner's target
        /// will be used if the local target is null.</summary>
        override public Transform Follow
        {
            get { return ResolveFollow(m_Follow); }
            set
            {
                if (m_Follow != value)
                    PreviousStateInvalid = true;
                m_Follow = value;
            }
        }

        /// <summary>Called by CinemachineCore at LateUpdate time
        /// so the vcam can position itself and track its targets.  This class will
        /// invoke its pipeline and generate a CameraState for this frame.</summary>
        override public void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            if (PreviousStateInvalid)
                deltaTime = -1;
            PreviousStateInvalid = false;

            // Reset the base camera state, in case the game object got moved in the editor
            if (deltaTime <= 0)
                m_State = m_PreviousState = PullStateFromVirtualCamera(worldUp); // not in gameplay

            // Update the state by invoking the component pipeline
            m_State = CalculateNewState(worldUp, deltaTime);

            // Save this state for use as a "from" state next frame
            m_PreviousState = State;

            // Push the raw position back to the game object's transform, so it
            // moves along with the camera.
            transform.position = State.RawPosition;

            // Leave the orientation alone when dragging, because it can
            // screw up position dragging local axes
            if (!SuppressOrientationUpdate)
                transform.rotation = State.RawOrientation;
        }

        /// <summary>Make sure that the pipeline cache is up-to-date.</summary>
        override protected void OnEnable()
        {
            base.OnEnable();
            InvalidateComponentPipeline();
            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(this);
            UpdateCameraState((brain != null) ? brain.DefaultWorldUp : Vector3.up, -1); // Snap to center
        }

        /// <summary>Calls the DestroyPipelineDelegate for destroying the hidden
        /// child object, to support undo.</summary>
        protected override void OnDestroy()
        {
            if (m_ComponentOwner != null)
            {
                if (DestroyPipelineOverride != null)
                    DestroyPipelineOverride(m_ComponentOwner.gameObject);
                else
                    DestroyImmediate(m_ComponentOwner.gameObject);
                m_ComponentOwner = null;
            }
            base.OnDestroy();
        }

        /// <summary>Enforce bounds for fields, when changed in inspector.</summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Lens.NearClipPlane = Mathf.Max(m_Lens.NearClipPlane, 0.01f);
            m_Lens.FarClipPlane = Mathf.Max(m_Lens.FarClipPlane, m_Lens.NearClipPlane + 0.01f);
        }

        void OnTransformChildrenChanged()
        {
            InvalidateComponentPipeline();
        }

        void Reset()
        {
            CreatePipeline(null);
        }

        /// <summary>
        /// Override component pipeline creation.
        /// This needs to be done by the editor to support Undo.
        /// The override must do exactly the same thing as the CreatePipeline method in this class.
        /// </summary>
        public static CreatePipelineDelegate CreatePipelineOverride;

        /// <summary>
        /// Override component pipeline creation.
        /// This needs to be done by the editor to support Undo.
        /// The override must do exactly the same thing as the CreatePipeline method in
        /// the CinemachineVirtualCamera class.
        /// </summary>
        public delegate Transform CreatePipelineDelegate(
            CinemachineVirtualCamera vcam, string name, ICinemachineComponent[] copyFrom);

        /// <summary>
        /// Override component pipeline destruction.
        /// This needs to be done by the editor to support Undo.
        /// </summary>
        public static DestroyPipelineDelegate DestroyPipelineOverride;

        /// <summary>
        /// Override component pipeline destruction.
        /// This needs to be done by the editor to support Undo.
        /// </summary>
        public delegate void DestroyPipelineDelegate(GameObject pipeline);

        /// <summary>
        /// Create a default pipeline container.
        /// </summary>
        private Transform CreatePipeline(CinemachineVirtualCamera copyFrom)
        {
            ICinemachineComponent[] components = null;
            if (copyFrom != null)
            {
                copyFrom.InvalidateComponentPipeline(); // make sure it's up to date
                components = copyFrom.GetComponentPipeline();
            }

            // Do the same thing with undo-support
            if (CreatePipelineOverride != null)
                m_ComponentOwner = CreatePipelineOverride(this, PipelineName, components);
            else
            {
                // Delete all existing pipeline childen
                List<Transform> list = new List<Transform>();
                foreach (Transform child in transform)
                    if (child.GetComponent<CinemachinePipeline>() != null)
                        list.Add(child);
                foreach (Transform child in list)
                    DestroyImmediate(child.gameObject);

                // Create a new pipeline
                GameObject go =  new GameObject(PipelineName);
                go.transform.parent = transform;
                go.AddComponent<CinemachinePipeline>();
                m_ComponentOwner = go.transform;

                // If copying, transfer the components
                if (components != null)
                    foreach (Component c in components)
                        ReflectionHelpers.CopyFields(c, go.AddComponent(c.GetType()));
            }
            return m_ComponentOwner;
        }

        /// <summary>
        /// Editor API: Call this when changing the pipeline from the editor.
        /// Will force a rebuild of the pipeline cache.
        /// </summary>
        public void InvalidateComponentPipeline() { m_ComponentPipeline = null; }

        /// <summary>Get the hidden CinemachinePipeline child object.</summary>
        public Transform GetComponentOwner() { UpdateComponentPipeline(); return m_ComponentOwner; }

        /// <summary>Get the component pipeline owned by the hidden child pipline container.
        /// For most purposes, it is preferable to use the GetCinemachineComponent method.</summary>
        public ICinemachineComponent[] GetComponentPipeline() { UpdateComponentPipeline(); return m_ComponentPipeline; }

        /// <summary>Get an existing component from the cinemachine pipeline.</summary>
        public T GetCinemachineComponent<T>() where T : MonoBehaviour
        {
            ICinemachineComponent[] components = GetComponentPipeline();
            if (components != null)
                foreach (var c in components)
                    if (c is T)
                        return c as T;
            return null;
        }

        /// <summary>Add a component to the cinemachine pipeline.</summary>
        public T AddCinemachineComponent<T>() where T : MonoBehaviour
        {
            // Get the existing components
            Transform owner = GetComponentOwner();
            ICinemachineComponent[] components = owner.GetComponents<ICinemachineComponent>();

            T behaviour = owner.gameObject.AddComponent<T>();
            ICinemachineComponent component = (ICinemachineComponent)behaviour;
            if (component != null && components != null)
            {
                // Remove the existing components at that stage
                CinemachineCore.Stage stage = component.Stage;
                for (int i = components.Length - 1; i >= 0; --i)
                {
                    if (components[i].Stage == stage)
                    {
                        DestroyObject(components[i] as MonoBehaviour);
                        components[i] = null;
                    }
                }
            }
            InvalidateComponentPipeline();
            return behaviour;
        }

        /// <summary>Remove a component from the cinemachine pipeline.</summary>
        public void DestroyCinemachineComponent<T>() where T : MonoBehaviour
        {
            ICinemachineComponent[] components = GetComponentPipeline();
            if (components != null)
            {
                foreach (var c in components)
                {
                    if (c is T)
                    {
                        DestroyObject(c as MonoBehaviour);
                        InvalidateComponentPipeline();
                    }
                }
            }
        }

        /// <summary>API for the editor, to make the dragging of position handles behave better.</summary>
        public bool SuppressOrientationUpdate { get; set; }

        CameraState m_State = CameraState.Default;          // Current state this frame
        CameraState m_PreviousState = CameraState.Default;  // State last frame, if simulating

        ICinemachineComponent[] m_ComponentPipeline = null;
        [SerializeField][HideInInspector] private Transform m_ComponentOwner = null;   // serialized to handle copy/paste
        void UpdateComponentPipeline()
        {
            // Did we just get copy/pasted?
            if (m_ComponentOwner != null && m_ComponentOwner.parent != transform)
            {
                CinemachineVirtualCamera copyFrom = (m_ComponentOwner.parent != null)
                    ? m_ComponentOwner.parent.gameObject.GetComponent<CinemachineVirtualCamera>() : null;
                CreatePipeline(copyFrom);
            }

            // Early out if we're up-to-date
            if (m_ComponentOwner != null && m_ComponentPipeline != null)
                return;

            m_ComponentOwner = null;
            List<ICinemachineComponent> list = new List<ICinemachineComponent>();
            foreach (Transform child in transform)
            {
                // skip virtual camera children
                if (child.GetComponent<CinemachinePipeline>() != null)
                {
                    m_ComponentOwner = child;
                    ICinemachineComponent[] components = child.GetComponents<ICinemachineComponent>();
                    foreach (ICinemachineComponent c in components)
                        list.Add(c);
                }
            }

            // Make sure we have a pipeline owner
            if (m_ComponentOwner == null)
                CreatePipeline(null);

            // Make sure the pipeline stays hidden, even through prefab
            if (CinemachineCore.sShowHiddenObjects)
                m_ComponentOwner.gameObject.hideFlags
                    &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);
            else
                m_ComponentOwner.gameObject.hideFlags
                    |= (HideFlags.HideInHierarchy | HideFlags.HideInInspector);

            // Sort the pipeline
            list.Sort((c1, c2) => (int)c1.Stage - (int)c2.Stage);
            m_ComponentPipeline = list.ToArray();
        }

        private CameraState CalculateNewState(Vector3 worldUp, float deltaTime)
        {
            // Initialize the camera state, in case the game object got moved in the editor
            CameraState state = PullStateFromVirtualCamera(worldUp);

            // Pull state from the LookAt and Follow targets
            if (Follow != null)
                state.RawPosition = Follow.position;
            if (LookAt != null)
                state.ReferenceLookAt = LookAt.position;

            // The next stage hook to call
            CinemachineCore.Stage stageHook = CinemachineCore.Stage.Lens;

            // Update the state by invoking the component pipeline
            ICinemachineComponent[] components = GetComponentPipeline();
            if (components != null)
            {
                foreach (ICinemachineComponent c in components)
                {
                    while ((int)stageHook < (int)c.Stage)
                    {
                        if (OnPostPipelineStage != null)
                            OnPostPipelineStage(this, stageHook, ref state, m_PreviousState, deltaTime);
                        ++stageHook;
                        // Just before the Aim component is applied, we initialize
                        // the orientation to look at the target
                        if (stageHook == CinemachineCore.Stage.Aim)
                            state = ApplyBaseLootAtToCameraState(state);
                    }
                    state = c.MutateCameraState(state, m_PreviousState, deltaTime);
                }
            }
            int numStages = Enum.GetValues(typeof(CinemachineCore.Stage)).Length;
            while ((int)stageHook < numStages)
            {
                if (OnPostPipelineStage != null)
                    OnPostPipelineStage(this, stageHook, ref state, m_PreviousState, deltaTime);
                ++stageHook;
                // Just before the Aim component is applied, we initialize
                // the orientation to look at the target
                if (stageHook == CinemachineCore.Stage.Aim)
                    state = ApplyBaseLootAtToCameraState(state);
            }

            return state;
        }

        private CameraState PullStateFromVirtualCamera(Vector3 worldUp)
        {
            CameraState state = CameraState.Default;
            state.RawPosition = transform.position;
            state.RawOrientation = transform.rotation;
            state.ReferenceUp = worldUp;

            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(this);
            m_Lens.Aspect = brain != null ? brain.OutputCamera.aspect : 1;
            m_Lens.Orthographic = brain != null ? brain.OutputCamera.orthographic : false;
            state.Lens = m_Lens;

            return state;
        }

        private CameraState ApplyBaseLootAtToCameraState(CameraState state)
        {
            if (LookAt != null)
            {
                Vector3 dir = LookAt.position - state.CorrectedPosition;
                if (!dir.AlmostZero())
                {
                    if (Vector3.Cross(dir.normalized, state.ReferenceUp).AlmostZero())
                        state.RawOrientation = Quaternion.FromToRotation(Vector3.forward, dir);
                    else
                        state.RawOrientation = Quaternion.LookRotation(dir, state.ReferenceUp);
                }
            }
            return state;
        }
    }
}
