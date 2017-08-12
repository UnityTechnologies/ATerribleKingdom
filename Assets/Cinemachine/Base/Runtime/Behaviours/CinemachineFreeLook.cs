using UnityEngine;
using Cinemachine.Utility;
using System.Collections.Generic;
using UnityEngine.Serialization;

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
        /// <summary>Default object for the camera children to look at (the aim target), if not specified in a child rig.  May be empty</summary>
        [Tooltip("Default object for the camera children to look at (the aim target), if not specified in a child rig.  May be empty if LookAt targets are specified in the child rigs.")]
        public Transform m_LookAt = null;

        /// <summary>Default object for the camera children wants to move with (the body target), if not specified in a child rig.  May be empty</summary>
        [Tooltip("Default object for the camera children wants to move with (the body target), if not specified in a child rig.  May be empty if Follow targets are specified in the child rigs.")]
        public Transform m_Follow = null;

        [Space]
        [Tooltip("How the damping values will be interpreted. Polar will attempt to preserve a constant distance from the target, subject to Z damping")]
        public CinemachineOrbitalTransposer.DampingStyle m_DampingStyle
            = CinemachineOrbitalTransposer.DampingStyle.Polar;

        /// <summary>Additional Y rotation applied to the target heading.
        /// When this value is 0, the camera will be placed behind the target</summary>
        [Range(-180f, 180f)]
        [Tooltip("Additional Y rotation applied to the target heading.  When this value is 0, the camera will be placed behind the target.")]
        public float m_HeadingBias = 0;

        /// <summary>If enabled, this lens setting will apply to all three child rigs, otherwise the child rig lens settings will be used</summary>
        [Tooltip("If enabled, this lens setting will apply to all three child rigs, otherwise the child rig lens settings will be used")]
        public bool m_UseCommonLensSetting = false;

        /// <summary>Specifies the lens properties of this Virtual Camera</summary>
        [FormerlySerializedAs("m_LensAttributes")]
        [Tooltip("Specifies the lens properties of this Virtual Camera.  This generally mirrors the Unity Camera's lens settings, and will be used to drive the Unity camera when the vcam is active")]
        [LensSettingsProperty]
        public LensSettings m_Lens = LensSettings.Default;

        [Tooltip("The Horizontal axis.  Value is 0..359.  This is passed on to the rigs' OrbitalTransposer component")]
        [Header("Axis Control")]
        public CinemachineOrbitalTransposer.AxisState m_XAxis
            = new CinemachineOrbitalTransposer.AxisState(3000f, 1f, 2f, 0f, "Mouse X");

        [Tooltip("The Vertical axis.  Value is 0..1.  Chooses how to blend the child rigs")]
        public CinemachineOrbitalTransposer.AxisState m_YAxis
            = new CinemachineOrbitalTransposer.AxisState(3f, 3f, 3f, 0.5f, "Mouse Y");

        [Tooltip("Controls how automatic recentering of the X axis is accomplished")]
        public CinemachineOrbitalTransposer.Recentering m_RecenterToTargetHeading
            = new CinemachineOrbitalTransposer.Recentering(
                    false, 1, 2,
                    CinemachineOrbitalTransposer.Recentering.HeadingDerivationMode.TargetForward, 4);

        [Header("Orbits")]
        [Tooltip("Controls how taut is the line that connects the rigs' orbits, which determines final placement on the Y axis")]
        [Range(0f, 1f)]
        public float m_SplineTension = 1f;

        /// <summary>Get a child rig</summary>
        /// <param name="i">Rig index.  Can be 0, 1, or 2</param>
        /// <returns>The rig, or null if index is bad.</returns>
        public CinemachineVirtualCamera GetRig(int i) { UpdateRigCache();  return (i < 0 || i > 2) ? null : m_Rigs[i]; }

        /// <summary>Names of the 3 child rigs</summary>
        public static string[] RigNames { get { return new string[] { "TopRig", "MiddleRig", "BottomRig" }; } }

        /// <summary>Default values for the child orbit radii</summary>
        public float[] DefaultRadius { get { return new float[] { 1.75f, 3f, 1.3f }; } }

        /// <summary>Default values for the child orbit heights</summary>
        public float[] DefaultHeight { get { return new float[] { 4.5f, 2.5f, 0.4f }; } }

        /// <summary>Updates the child rig cache</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            InvalidateRigCache();

            // Snap to target
            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(this);
            UpdateCameraState((brain != null) ? brain.DefaultWorldUp : Vector3.up, -1);
        }

        /// <summary>Makes sure that the child rigs get destroyed in an undo-firndly manner.
        /// Invalidates the rig cache.</summary>
        protected override void OnDestroy()
        {
            if (m_Rigs != null)
            {
                foreach (var rig in m_Rigs)
                {
                    if (DestroyRigOverride != null)
                        DestroyRigOverride(rig.gameObject);
                    else
                        DestroyImmediate(rig.gameObject);
                }
                m_Rigs = null;
            }
            InvalidateRigCache();
            base.OnDestroy();
        }

        /// <summary>Invalidates the rig cache</summary>
        void OnTransformChildrenChanged()
        {
            InvalidateRigCache();
        }

        void Reset()
        {
            CreateRigs(null);
        }

        /// <summary>Enforce bounds for fields, when changed in inspector.</summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Lens.NearClipPlane = Mathf.Max(m_Lens.NearClipPlane, 0.01f);
            m_Lens.FarClipPlane = Mathf.Max(m_Lens.FarClipPlane, m_Lens.NearClipPlane + 0.01f);
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
                    PreviousStateInvalid = true;
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
                    PreviousStateInvalid = true;
                m_Follow = value;
            }
        }

        /// <summary>Remove a Pipeline stage hook callback.
        /// Make sure it is removed from all the children.</summary>
        /// <param name="d">The delegate to remove.</param>
        public override void RemovePostPipelineStageHook(OnPostPipelineStageDelegate d)
        {
            base.RemovePostPipelineStageHook(d);
            UpdateRigCache();
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
            if (PreviousStateInvalid)
                deltaTime = -1;
            PreviousStateInvalid = false;

            UpdateRigCache();

            // Read the Height
            bool activeCam = CinemachineCore.Instance.IsLive(this);
            if (activeCam)
                m_YAxis.Update(deltaTime, false);

            // Reads the heading.  Make sure all the rigs get updated first
            PushSettingsToRigs();
            if (activeCam)
                UpdateHeading(deltaTime, m_State.ReferenceUp);

            // Drive the rigs
            for (int i = 0; i < m_Rigs.Length; ++i)
                if (m_Rigs[i] != null)
                    m_Rigs[i].UpdateCameraState(worldUp, deltaTime);

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
        [SerializeField][HideInInspector][NoSaveDuringPlay] private CinemachineVirtualCamera[] m_Rigs = new CinemachineVirtualCamera[3];

        void InvalidateRigCache() { mOribitals = null; }
        CinemachineOrbitalTransposer[] mOribitals = null;
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


        private void CreateRigs(CinemachineVirtualCamera[] copyFrom)
        {
            // Invalidate the cache
            m_Rigs = null;
            mOribitals = null;

            string[] rigNames = RigNames;
            float[] softCenterDefaultsV = new float[] { 0.5f, 0.55f, 0.6f };
            for (int i = 0; i < rigNames.Length; ++i)
            {
                CinemachineVirtualCamera src = null;
                if (copyFrom != null && copyFrom.Length > i)
                    src = copyFrom[i];

                CinemachineVirtualCamera rig = null;
                if (CreateRigOverride != null)
                    rig = CreateRigOverride(this, rigNames[i], src);
                else
                {
                    // If there is an existing rig with this name, delete it
                    List<Transform> list = new List<Transform>();
                    foreach (Transform child in transform)
                        if (child.GetComponent<CinemachineVirtualCamera>() != null
                            && child.gameObject.name == rigNames[i])
                            list.Add(child);
                    foreach (Transform child in list)
                        DestroyImmediate(child.gameObject);

                    // Create a new rig with default components
                    GameObject go = new GameObject(rigNames[i]);
                    go.transform.parent = transform;
                    rig = go.AddComponent<CinemachineVirtualCamera>();
                    if (src != null)
                        ReflectionHelpers.CopyFields(src, rig);
                    else
                    {
                        go = rig.GetComponentOwner().gameObject;
                        go.AddComponent<CinemachineOrbitalTransposer>();
                        go.AddComponent<CinemachineComposer>();
                    }
                }

                // Set up the defaults
                rig.InvalidateComponentPipeline();
                CinemachineOrbitalTransposer orbital = rig.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                if (orbital == null)
                    orbital = rig.AddCinemachineComponent<CinemachineOrbitalTransposer>(); // should not happen
                if (src == null)
                {
                    // Only set defaults if not copying
                    orbital.m_Radius = DefaultRadius[i];
                    orbital.m_HeightOffset = DefaultHeight[i];
                    CinemachineComposer composer = rig.GetCinemachineComponent<CinemachineComposer>();
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
        }

        private void UpdateRigCache()
        {
            // Did we just get copy/pasted?
            string[] rigNames = RigNames;
            if (m_Rigs != null && m_Rigs.Length == rigNames.Length
                && m_Rigs[0] != null && m_Rigs[0].transform.parent != transform)
                CreateRigs(m_Rigs);

            // Early out if we're up to date
            if (mOribitals != null && mOribitals.Length == rigNames.Length)
                return;

            // Locate existiong rigs, and recreate them if any are missing
            if (LocateExistingRigs(rigNames, false) != rigNames.Length)
            {
                CreateRigs(null);
                LocateExistingRigs(rigNames, true);
            }

            foreach (var rig in m_Rigs)
            {
                // Hide the rigs from prying eyes
                if (CinemachineCore.sShowHiddenObjects)
                    rig.gameObject.hideFlags
                        &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);
                else
                    rig.gameObject.hideFlags
                        |= (HideFlags.HideInHierarchy | HideFlags.HideInInspector);

                // Configure the UI
                rig.m_HideHeaderInInspector = true;
                rig.m_ExcludedPropertiesInInspector = new string[] { "m_Script", "m_Priority" };
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
            mOribitals = new CinemachineOrbitalTransposer[rigNames.Length];
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
                        if (mOribitals[i] == null && go.name == rigNames[i])
                        {
                            // Must have an orbital transposer or it's no good
                            mOribitals[i] = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                            if (mOribitals[i] == null && forceOrbital)
                                mOribitals[i] = vcam.AddCinemachineComponent<CinemachineOrbitalTransposer>();
                            if (mOribitals[i] != null)
                            {
                                mOribitals[i].m_HeadingIsSlave = true;
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
            ref CameraState newState, CameraState previousState, float deltaTime)
        {
            if (OnPostPipelineStage != null)
                OnPostPipelineStage(vcam, stage, ref newState, previousState, deltaTime);
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
                mOribitals[i].m_DampingStyle = m_DampingStyle;
                mOribitals[i].m_HeadingBias = m_HeadingBias;
                mOribitals[i].m_HeadingIsSlave = true;
                mOribitals[i].SetXAxisState(m_XAxis);
                mOribitals[i].m_RecenterToTargetHeading = m_RecenterToTargetHeading;
                if (i > 0)
                    mOribitals[i].m_RecenterToTargetHeading.m_enabled = false;
                mOribitals[i].UseOffsetOverride = true;
                mOribitals[i].OffsetOverride = GetLocalPositionForCameraFromInput(m_YAxis.Value);
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
            if (mOribitals[0] != null)
            {
                mOribitals[0].UpdateHeading(deltaTime, up, true);
                m_XAxis.Value = mOribitals[0].m_XAxis.Value;
                m_XAxis.m_InputAxisValue = mOribitals[0].m_XAxis.m_InputAxisValue;
            }
            // Then push it to the other rigs
            for (int i = 1; i < mOribitals.Length; ++i)
                if (mOribitals[i] != null)
                    mOribitals[i].m_XAxis.Value = m_XAxis.Value;
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
            if (mOribitals == null)
                return Vector3.zero;

            Vector3 topPos = Vector3.up * mOribitals[0].m_HeightOffset + Vector3.back * mOribitals[0].m_Radius;
            Vector3 middlePos = Vector3.up * mOribitals[1].m_HeightOffset + Vector3.back * mOribitals[1].m_Radius;
            Vector3 bottomPos = Vector3.up * mOribitals[2].m_HeightOffset + Vector3.back * mOribitals[2].m_Radius;

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
