using UnityEngine;
using Cinemachine.Utility;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System;

namespace Cinemachine
{
    /// <summary>
    /// A Cinemachine Camera geared towards a 3rd person camera experience.
    /// The camera orbits around its subject with three separate camera rigs defining
    /// rings around the target. Each rig has its own radius, height offset, composer,
    /// and lens settings.
    /// Depending on the camera's position along the spline connecting these three rigs,
    /// these settings are interpolated to give the final camera position and state.
    /// </summary>
    [DocumentationSorting(11, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode, DisallowMultipleComponent]
    [AddComponentMenu("Cinemachine/CinemachineFreeLook")]
    public class CinemachineFreeLook : CinemachineVirtualCameraBase
    {
        /// <summary>Object for the camera children to look at (the aim target)</summary>
        [Tooltip("Object for the camera children to look at (the aim target).")]
        [NoSaveDuringPlay]
        public Transform m_LookAt = null;

        /// <summary>Object for the camera children wants to move with (the body target)</summary>
        [Tooltip("Object for the camera children wants to move with (the body target).")]
        [NoSaveDuringPlay]
        public Transform m_Follow = null;

        /// <summary>If enabled, this lens setting will apply to all three child rigs, otherwise the child rig lens settings will be used</summary>
        [Space]
        [Tooltip("If enabled, this lens setting will apply to all three child rigs, otherwise the child rig lens settings will be used")]
        public bool m_UseCommonLensSetting = true;

        /// <summary>Specifies the lens properties of this Virtual Camera</summary>
        [FormerlySerializedAs("m_LensAttributes")]
        [Tooltip("Specifies the lens properties of this Virtual Camera.  This generally mirrors the Unity Camera's lens settings, and will be used to drive the Unity camera when the vcam is active")]
        [LensSettingsProperty]
        public LensSettings m_Lens = LensSettings.Default;

        [Header("Axis Control")]
        [Tooltip("The Vertical axis.  Value is 0..1.  Chooses how to blend the child rigs")]
        public CinemachineOrbitalTransposer.AxisState m_YAxis
            = new CinemachineOrbitalTransposer.AxisState(2f, 0.2f, 0.1f, 0.5f, "Mouse Y", false);

        [Tooltip("The Horizontal axis.  Value is 0..359.  This is passed on to the rigs' OrbitalTransposer component")]
        public CinemachineOrbitalTransposer.AxisState m_XAxis
            = new CinemachineOrbitalTransposer.AxisState(300f, 0.1f, 0.1f, 0f, "Mouse X", true);

        [Tooltip("The definition of Forward.  Camera will follow behind.")]
        public CinemachineOrbitalTransposer.Heading m_Heading = new CinemachineOrbitalTransposer.Heading();

        [Tooltip("Controls how automatic recentering of the X axis is accomplished")]
        public CinemachineOrbitalTransposer.Recentering m_RecenterToTargetHeading
            = new CinemachineOrbitalTransposer.Recentering(false, 1, 2);

        [Header("Orbits")]
        /// <summary>The coordinate space to use when interpreting the offset from the target</summary>
        [Tooltip("The coordinate space to use when interpreting the offset from the target.  This is also used to set the camera's Up vector, which will be maintained when aiming the camera.")]
        public CinemachineOrbitalTransposer.BindingMode m_BindingMode 
            = CinemachineOrbitalTransposer.BindingMode.LockToTargetWithWorldUp;

        [Tooltip("Controls how taut is the line that connects the rigs' orbits, which determines final placement on the Y axis")]
        [Range(0f, 1f)]
        public float m_SplineTension = 1f;

        [Serializable]
        public struct Orbit 
        { 
            public float m_Height; 
            public float m_Radius; 
            public Orbit(float h, float r) { m_Height = h; m_Radius = r; }
        }
        [Tooltip("The radius and height of the three orbiting rigs.")]
        public Orbit[] m_Orbits = new Orbit[3] 
        { 
            // These are the default orbits
            new Orbit(4.5f, 1.75f),
            new Orbit(2.5f, 3f),
            new Orbit(0.4f, 1.3f)
        };

        // Legacy support
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeadingBias")] 
        private float m_LegacyHeadingBias = float.MaxValue;
        bool mUseLegacyRigDefinitions = false;

        /// <summary>Enforce bounds for fields, when changed in inspector.</summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Lens.NearClipPlane = Mathf.Max(m_Lens.NearClipPlane, 0.01f);
            m_Lens.FarClipPlane = Mathf.Max(m_Lens.FarClipPlane, m_Lens.NearClipPlane + 0.01f);

            // Upgrade after a legacy deserialize
            if (m_LegacyHeadingBias != float.MaxValue)
            {
                m_Heading.m_HeadingBias = m_LegacyHeadingBias;
                m_LegacyHeadingBias = float.MaxValue;
                m_RecenterToTargetHeading.LegacyUpgrade(
                    ref m_Heading.m_HeadingDefinition, ref m_Heading.m_VelocityFilterStrength);
                mUseLegacyRigDefinitions = true;
            }

            InvalidateRigCache();
        }

        /// <summary>Get a child rig</summary>
        /// <param name="i">Rig index.  Can be 0, 1, or 2</param>
        /// <returns>The rig, or null if index is bad.</returns>
        public CinemachineVirtualCamera GetRig(int i) 
        { 
            UpdateRigCache();  
            return (i < 0 || i > 2) ? null : m_Rigs[i]; 
        }

        /// <summary>Names of the 3 child rigs</summary>
        public static string[] RigNames { get { return new string[] { "TopRig", "MiddleRig", "BottomRig" }; } }

        bool mIsDestroyed = false;

        /// <summary>Updates the child rig cache</summary>
        protected override void OnEnable()
        {
            mIsDestroyed = false;
            base.OnEnable();
            InvalidateRigCache();
        }

        /// <summary>Makes sure that the child rigs get destroyed in an undo-firndly manner.
        /// Invalidates the rig cache.</summary>
        protected override void OnDestroy()
        {
            // Make the rigs visible instead of destroying - this is to keep Undo happy
            if (m_Rigs != null)
                foreach (var rig in m_Rigs)
                    rig.gameObject.hideFlags
                        &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);

            mIsDestroyed = true;
            base.OnDestroy();
        }

        /// <summary>Invalidates the rig cache</summary>
        void OnTransformChildrenChanged()
        {
            InvalidateRigCache();
        }

        void Reset()
        {
            DestroyRigs();
        }

        /// <summary>The cacmera state, which will be a blend of the child rig states</summary>
        override public CameraState State { get { return m_State; } }

        /// <summary>Get the current LookAt target.  Returns parent's LookAt if parent
        /// is non-null and no specific LookAt defined for this camera</summary>
        override public Transform LookAt
        {
            get { return ResolveLookAt(m_LookAt); }
            set
            {
                if (m_LookAt != value)
                    PreviousStateIsValid = false;
                m_LookAt = value;
            }
        }

        /// <summary>Get the current Follow target.  Returns parent's Follow if parent
        /// is non-null and no specific Follow defined for this camera</summary>
        override public Transform Follow
        {
            get { return ResolveFollow(m_Follow); }
            set
            {
                if (m_Follow != value)
                    PreviousStateIsValid = false;
                m_Follow = value;
            }
        }

        /// <summary>Returns the rig with the greatest weight</summary>
        public override ICinemachineCamera LiveChildOrSelf 
        { 
            get 
            { 
                // Do not update the rig cache here or there will be infinite loop at creation time 
                if (m_Rigs == null || m_Rigs.Length != 3)
                    return this;
                if (m_YAxis.Value < 0.33f)
                    return m_Rigs[2];
                if (m_YAxis.Value > 0.66f)
                    return m_Rigs[0];
                return m_Rigs[1];
            }
        }

        /// <summary>Check whether the vcam a live child of this camera.  
        /// Returns true if the child is currently contributing actively to the camera state.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public override bool IsLiveChild(ICinemachineCamera vcam) 
        {
            // Do not update the rig cache here or there will be infinite loop at creation time 
            if (m_Rigs == null || m_Rigs.Length != 3)
                return false;

            if (m_YAxis.Value < 0.33f)
                return vcam == (ICinemachineCamera)m_Rigs[2];
            if (m_YAxis.Value < 0.66f)
                return vcam == (ICinemachineCamera)m_Rigs[0];
            return vcam == (ICinemachineCamera)m_Rigs[1]; 
        }

        /// <summary>Remove a Pipeline stage hook callback.
        /// Make sure it is removed from all the children.</summary>
        /// <param name="d">The delegate to remove.</param>
        public override void RemovePostPipelineStageHook(OnPostPipelineStageDelegate d)
        {
            base.RemovePostPipelineStageHook(d);
            UpdateRigCache();
            if (m_Rigs != null)
                foreach (var vcam in m_Rigs)
                    vcam.RemovePostPipelineStageHook(d);
        }

        /// <summary>Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  All 3 child rigs are updated,
        /// and a blend calculated, depending on the value of the Y axis.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        override public void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            if (!PreviousStateIsValid)
                deltaTime = -1;
            PreviousStateIsValid = true;

            UpdateRigCache();

            // Read the Height
            bool activeCam = (deltaTime > 0) || CinemachineCore.Instance.IsLive(this);
            if (activeCam)
                m_YAxis.Update(deltaTime);

            // Reads the heading.  Make sure all the rigs get updated first
            PushSettingsToRigs();
            if (activeCam)
                UpdateHeading(deltaTime, m_State.ReferenceUp);

            // Drive the rigs
            for (int i = 0; i < m_Rigs.Length; ++i)
                if (m_Rigs[i] != null)
                    CinemachineCore.Instance.UpdateVirtualCamera(m_Rigs[i], worldUp, deltaTime);

            // Reset the base camera state, in case the game object got moved in the editor
            if (deltaTime <= 0)
                m_State = PullStateFromVirtualCamera(worldUp); // Not in gameplay

            // Update the current state by invoking the component pipeline
            m_State = CalculateNewState(worldUp, deltaTime);

            // Push the raw position back to the game object's transform, so it
            // moves along with the camera.  Leave the orientation alone, because it
            // screws up camera dragging when there is a LookAt behaviour.
            if (Follow != null)
                transform.position = State.RawPosition;
        }

        /// <summary>If we are transitioning from another FreeLook, grab the axis values from it.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        override public void OnTransitionFromCamera(ICinemachineCamera fromCam)
        {
            if ((fromCam != null) && (fromCam is CinemachineFreeLook))
            {
                CinemachineFreeLook freeLookFrom = fromCam as CinemachineFreeLook;
                if (freeLookFrom.Follow == Follow)
                {
                    m_XAxis.Value = freeLookFrom.m_XAxis.Value;
                    m_YAxis.Value = freeLookFrom.m_YAxis.Value;
                    PushSettingsToRigs();
                }
            }
        }

        CameraState m_State = CameraState.Default;          // Current state this frame

        /// Serialized in order to support copy/paste
        [SerializeField][HideInInspector][NoSaveDuringPlay] private CinemachineVirtualCamera[] m_Rigs 
            = new CinemachineVirtualCamera[3];

        void InvalidateRigCache() { mOrbitals = null; }
        CinemachineOrbitalTransposer[] mOrbitals = null;
        CinemachineBlend mBlendA;
        CinemachineBlend mBlendB;

        /// <summary>
        /// Override component pipeline creation.
        /// This needs to be done by the editor to support Undo.
        /// The override must do exactly the same thing as the CreatePipeline method in this class.
        /// </summary>
        public static CreateRigDelegate CreateRigOverride;

        /// <summary>
        /// Override component pipeline creation.
        /// This needs to be done by the editor to support Undo.
        /// The override must do exactly the same thing as the CreatePipeline method in this class.
        /// </summary>
        public delegate CinemachineVirtualCamera CreateRigDelegate(
            CinemachineFreeLook vcam, string name, CinemachineVirtualCamera copyFrom);

        /// <summary>
        /// Override component pipeline destruction.
        /// This needs to be done by the editor to support Undo.
        /// </summary>
        public static DestroyRigDelegate DestroyRigOverride;

        /// <summary>
        /// Override component pipeline destruction.
        /// This needs to be done by the editor to support Undo.
        /// </summary>
        public delegate void DestroyRigDelegate(GameObject rig);

        private void DestroyRigs()
        {
            CinemachineVirtualCamera[] oldRigs = new CinemachineVirtualCamera[RigNames.Length];
            for (int i = 0; i < RigNames.Length; ++i)
            {
                foreach (Transform child in transform)
                    if (child.gameObject.name == RigNames[i])
                        oldRigs[i] = child.GetComponent<CinemachineVirtualCamera>();
            }
            for (int i = 0; i < oldRigs.Length; ++i)
            {
                if (oldRigs[i] != null)
                {
                    if (DestroyRigOverride != null)
                        DestroyRigOverride(oldRigs[i].gameObject);
                    else
                        Destroy(oldRigs[i].gameObject);
                }
            }
            m_Rigs = null;
            mOrbitals = null;
        }

        private CinemachineVirtualCamera[] CreateRigs(CinemachineVirtualCamera[] copyFrom)
        {
            // Invalidate the cache
            mOrbitals = null;
            float[] softCenterDefaultsV = new float[] { 0.5f, 0.55f, 0.6f };
            CinemachineVirtualCamera[] newRigs = new CinemachineVirtualCamera[3];
            for (int i = 0; i < RigNames.Length; ++i)
            {
                CinemachineVirtualCamera src = null;
                if (copyFrom != null && copyFrom.Length > i)
                    src = copyFrom[i];

                if (CreateRigOverride != null)
                    newRigs[i] = CreateRigOverride(this, RigNames[i], src);
                else
                {
                    // Create a new rig with default components
                    GameObject go = new GameObject(RigNames[i]);
                    go.transform.parent = transform;
                    newRigs[i] = go.AddComponent<CinemachineVirtualCamera>();
                    if (src != null)
                        ReflectionHelpers.CopyFields(src, newRigs[i]);
                    else
                    {
                        go = newRigs[i].GetComponentOwner().gameObject;
                        go.AddComponent<CinemachineOrbitalTransposer>();
                        go.AddComponent<CinemachineComposer>();
                    }
                }

                // Set up the defaults
                newRigs[i].InvalidateComponentPipeline();
                CinemachineOrbitalTransposer orbital = newRigs[i].GetCinemachineComponent<CinemachineOrbitalTransposer>();
                if (orbital == null)
                    orbital = newRigs[i].AddCinemachineComponent<CinemachineOrbitalTransposer>(); // should not happen
                if (src == null)
                {
                    // Only set defaults if not copying
                    orbital.m_YawDamping = 0;
                    CinemachineComposer composer = newRigs[i].GetCinemachineComponent<CinemachineComposer>();
                    if (composer != null)
                    {
                        composer.m_HorizontalDamping = composer.m_VerticalDamping = 0;
                        composer.m_ScreenX = 0.5f;
                        composer.m_ScreenY = softCenterDefaultsV[i];
                        composer.m_DeadZoneWidth = composer.m_DeadZoneHeight = 0;
                        composer.m_SoftZoneWidth = composer.m_SoftZoneHeight = 0.8f;
                        composer.m_BiasX = composer.m_BiasY = 0;
                    }
                }
            }
            return newRigs;
        }

        private void UpdateRigCache()
        {
            if (mIsDestroyed)
                return;

            // Special condition: Did we just get copy/pasted?
            string[] rigNames = RigNames;
            if (m_Rigs != null && m_Rigs.Length == rigNames.Length
                && m_Rigs[0] != null && m_Rigs[0].transform.parent != transform)
            {
                DestroyRigs();
                m_Rigs = CreateRigs(m_Rigs);
            }

            // Early out if we're up to date
            if (mOrbitals != null && mOrbitals.Length == rigNames.Length)
                return;

            // Locate existing rigs, and recreate them if any are missing
            if (LocateExistingRigs(rigNames, false) != rigNames.Length)
            {
                DestroyRigs();
                m_Rigs = CreateRigs(null);
                LocateExistingRigs(rigNames, true);
            }

            foreach (var rig in m_Rigs)
            {
                // Configure the UI
                rig.m_HideHeaderInInspector = true;
                rig.m_ExcludedPropertiesInInspector = m_UseCommonLensSetting 
                    ? new string[] { "m_Script", "m_Priority", "m_LookAt", "m_Follow", "m_Lens" }
                    : new string[] { "m_Script", "m_Priority", "m_LookAt", "m_Follow" };
                rig.m_LockStageInInspector = new CinemachineCore.Stage[] { CinemachineCore.Stage.Body };

                // Chain into the pipeline callback
                rig.AddPostPipelineStageHook(PostPipelineStageCallback);
            }

            // Create the blend objects
            mBlendA = new CinemachineBlend(m_Rigs[1], m_Rigs[0], AnimationCurve.Linear(0, 0, 1, 1), 0);
            mBlendB = new CinemachineBlend(m_Rigs[2], m_Rigs[1], AnimationCurve.Linear(0, 0, 1, 1), 0);

            // Horizontal rotation clamped to [0,360] (with wraparound)
            m_XAxis.SetThresholds(0f, 360f, true);

            // Vertical rotation cleamped to [0,1] as it is a t-value for the
            // catmull-rom spline going through the 3 points on the rig
            m_YAxis.SetThresholds(0f, 1f, false);
        }

        private int LocateExistingRigs(string[] rigNames, bool forceOrbital)
        {
            mOrbitals = new CinemachineOrbitalTransposer[rigNames.Length];
            m_Rigs = new CinemachineVirtualCamera[rigNames.Length];
            int rigsFound = 0;
            foreach (Transform child in transform)
            {
                CinemachineVirtualCamera vcam = child.GetComponent<CinemachineVirtualCamera>();
                if (vcam != null)
                {
                    GameObject go = child.gameObject;
                    for (int i = 0; i < rigNames.Length; ++i)
                    {
                        if (mOrbitals[i] == null && go.name == rigNames[i])
                        {
                            // Must have an orbital transposer or it's no good
                            mOrbitals[i] = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                            if (mOrbitals[i] == null && forceOrbital)
                                mOrbitals[i] = vcam.AddCinemachineComponent<CinemachineOrbitalTransposer>();
                            if (mOrbitals[i] != null)
                            {
                                mOrbitals[i].m_HeadingIsSlave = true;
                                m_Rigs[i] = vcam;
                                ++rigsFound;
                            }
                        }
                    }
                }
            }
            return rigsFound;
        }

        void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState newState, float deltaTime)
        {
            if (OnPostPipelineStage != null)
                OnPostPipelineStage(vcam, stage, ref newState, deltaTime);
        }

        void PushSettingsToRigs()
        {
            UpdateRigCache();
            for (int i = 0; i < m_Rigs.Length; ++i)
            {
                if (m_Rigs[i] == null)
                    continue;
                if (m_UseCommonLensSetting)
                    m_Rigs[i].m_Lens = m_Lens;

                // If we just deserialized from a legacy version, 
                // pull the orbits and targets from the rigs
                if (mUseLegacyRigDefinitions)
                {
                    mUseLegacyRigDefinitions = false;
                    m_Orbits[i].m_Height = mOrbitals[i].m_FollowOffset.y;
                    m_Orbits[i].m_Radius = -mOrbitals[i].m_FollowOffset.z;
                    if (m_Rigs[i].LookAt != null)
                        LookAt = m_Rigs[i].LookAt;
                    if (m_Rigs[i].Follow != null)
                        Follow = m_Rigs[i].Follow;
                    m_Rigs[i].LookAt = m_Rigs[i].Follow = null;
                }

                // Hide the rigs from prying eyes
                if (CinemachineCore.sShowHiddenObjects)
                    m_Rigs[i].gameObject.hideFlags
                        &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);
                else
                    m_Rigs[i].gameObject.hideFlags
                        |= (HideFlags.HideInHierarchy | HideFlags.HideInInspector);

                mOrbitals[i].m_FollowOffset = new Vector3(0, m_Orbits[i].m_Height, -m_Orbits[i].m_Radius);
                mOrbitals[i].m_BindingMode = m_BindingMode;
                mOrbitals[i].m_Heading = m_Heading;
                mOrbitals[i].m_HeadingIsSlave = true;
                mOrbitals[i].m_XAxis = m_XAxis;
                mOrbitals[i].m_RecenterToTargetHeading = m_RecenterToTargetHeading;
                if (i > 0)
                    mOrbitals[i].m_RecenterToTargetHeading.m_enabled = false;
                mOrbitals[i].UseOffsetOverride = true;
                mOrbitals[i].OffsetOverride = GetLocalPositionForCameraFromInput(m_YAxis.Value);
            }
        }

        private CameraState CalculateNewState(Vector3 worldUp, float deltaTime)
        {
            CameraState state = PullStateFromVirtualCamera(worldUp);

            // Blend from the appropriate rigs
            float t = m_YAxis.Value;
            if (t > 0.5f)
            {
                if (mBlendA != null)
                {
                    mBlendA.TimeInBlend = (t - 0.5f) * 2f;
                    mBlendA.UpdateCameraState(worldUp, deltaTime);
                    state = mBlendA.State;
                }
            }
            else
            {
                if (mBlendB != null)
                {
                    mBlendB.TimeInBlend = t * 2f;
                    mBlendB.UpdateCameraState(worldUp, deltaTime);
                    state = mBlendB.State;
                }
            }
            return state;
        }

        void UpdateHeading(float deltaTime, Vector3 up)
        {
            // We let the first rig calculate the heading
            if (mOrbitals[0] != null)
            {
                mOrbitals[0].UpdateHeading(deltaTime, up);
                m_XAxis = mOrbitals[0].m_XAxis;
            }
            // Then push it to the other rigs
            for (int i = 1; i < mOrbitals.Length; ++i)
                if (mOrbitals[i] != null)
                    mOrbitals[i].m_XAxis = m_XAxis;
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

        /// <summary>
        /// Returns the local position of the camera along the spline used to connect the
        /// three camera rigs. Does not take into account the current heading of the
        /// camera (or its target)
        /// </summary>
        /// <param name="t">The t-value for the camera on its spline. Internally clamped to
        /// the value [0,1]</param>
        /// <returns>The local offset (back + up) of the camera WRT its target based on the
        /// supplied t-value</returns>
        public Vector3 GetLocalPositionForCameraFromInput(float t)
        {
            if (mOrbitals == null)
                return Vector3.zero;

            Vector3 topPos = Vector3.up * m_Orbits[0].m_Height + Vector3.back * m_Orbits[0].m_Radius;
            Vector3 middlePos = Vector3.up * m_Orbits[1].m_Height + Vector3.back * m_Orbits[1].m_Radius;
            Vector3 bottomPos = Vector3.up * m_Orbits[2].m_Height + Vector3.back * m_Orbits[2].m_Radius;

            float hTop = topPos.y - middlePos.y;
            float hBot = middlePos.y - bottomPos.y;
            Vector3 ctrl = middlePos;

            if (t > 0.5f)
            {
                ctrl.y += (Mathf.Abs(hTop) < Mathf.Abs(hBot)) ? hTop : hBot;
                ctrl = Vector3.Lerp(Vector3.Lerp(middlePos, topPos, 0.5f), ctrl, m_SplineTension);
                return SplinePoint(middlePos, ctrl, topPos, (t - 0.5f) * 2f);
            }
            ctrl.y -= (Mathf.Abs(hTop) < Mathf.Abs(hBot)) ? hTop : hBot;
            ctrl = Vector3.Lerp(Vector3.Lerp(bottomPos, middlePos, 0.5f), ctrl, m_SplineTension);
            return SplinePoint(bottomPos, ctrl, middlePos, t * 2f);
        }

        static Vector3 SplinePoint(Vector3 p1, Vector3 c, Vector3 p2, float t)
        {
            Vector3 pA = Vector3.Lerp(p1, c, t);
            Vector3 pB = Vector3.Lerp(c, p2, t);
            return Vector3.Lerp(pA, pB, t);
        }
    }
}
