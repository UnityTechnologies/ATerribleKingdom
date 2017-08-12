using System;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine.Utility;
using UnityEngine.Events;
using System.Collections;

namespace Cinemachine
{
    /// <summary>
    /// CinemachineBrain is the link between the Unity Camera and the Cinemachine Virtual 
    /// Cameras in the scene.  It monitors the priority stack to choose the current 
    /// Virtual Camera, and blend with another if necessary.  Finally and most importantly, 
    /// it applies the Virtual Camera state to the attached Unity Camera.
    /// 
    /// The CinemachineBrain is also the place where rules for blending between virtual cameras 
    /// are defined.  Camera blending is an interpolation over time of one virtual camera 
    /// position and state to another. If you think of virtual cameras as cameramen, then 
    /// blending is a little like one cameraman smoothly passing the camera to another cameraman. 
    /// You can specify the time over which to blend, as well as the blend curve shape. 
    /// Note that a camera cut is just a zero-time blend.
    /// </summary>
    [DocumentationSorting(0, DocumentationSortingAttribute.Level.UserRef)]
    [RequireComponent(typeof(Camera)), ExecuteInEditMode, DisallowMultipleComponent]
    [AddComponentMenu("Cinemachine/CinemachineBrain")]
    [SaveDuringPlay]
    public class CinemachineBrain : MonoBehaviour
    {
        /// <summary>
        /// When enabled, the current camera and blend will be indicated in the game window, for debugging.
        /// </summary>
        [Tooltip("When enabled, the current camera and blend will be indicated in the game window, for debugging")]
        public bool m_ShowDebugText = false;

        /// <summary>
        /// When enabled, shows the camera's frustum in the scene view.
        /// </summary>
        [Tooltip("When enabled, the camera's frustum will be shown at all times in the scene view")]
        public bool m_ShowCameraFrustum = true;

        /// <summary>
        /// If set, this object's Y axis will define the worldspace Up vector for all the
        /// virtual cameras.  This is useful in top-down game environments.  If not set, Up is worldspace Y.
        /// </summary>
        [Tooltip("If set, this object's Y axis will define the worldspace Up vector for all the virtual cameras.  This is useful for instance in top-down game environments.  If not set, Up is worldspace Y.  Setting this appropriately is important, because Virtual Cameras don't like looking straight up or straight down.")]
        public Transform m_WorldUpOverride;

        /// <summary>This enum defines the options available for the update method.</summary>
        [DocumentationSorting(0.1f, DocumentationSortingAttribute.Level.UserRef)]
        public enum UpdateMethod
        {
            /// <summary>Virtual cameras are updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Virtual cameras are updated in MonoBehaviour LateUpdate.</summary>
            LateUpdate,
            /// <summary>Virtual cameras are updated according to how the target is updated.</summary>
            SmartUpdate
        };

        /// <summary>Depending on how the target objects are animated, adjust the update method to
        /// minimize the potential jitter.  Use FixedUpdate if all your targets are animated with for RigidBody animation.
        /// SmartUpdate will choose the best method for each virtual camera, depending
        /// on how the target is animated.</summary>
        [Tooltip("Use FixedUpdate if all your targets are animated during FixedUpdate (e.g. RigidBodies), LateUpdate if all your targets are animated during the normal Update loop, and SmartUpdate if you want Cinemachine to do the appropriate thing on a per-target basis.  SmartUpdate is the recommended setting")]
        public UpdateMethod m_UpdateMethod = UpdateMethod.SmartUpdate;

        /// <summary>
        /// The blend which is used if you don't explicitly define a blend between two Virtual Cameras.
        /// </summary>
        [CinemachineBlendDefinitionProperty]
        [Tooltip("The blend that is used in cases where you haven't explicitly defined a blend between two Virtual Cameras")]
        public CinemachineBlendDefinition m_DefaultBlend
            = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 2f);

        /// <summary>
        /// This is the asset which contains custom settings for specific blends.
        /// </summary>
        [HideInInspector]
        [Tooltip("This is the asset that contains custom settings for blends between specific virtual cameras in your scene")]
        public CinemachineBlenderSettings m_CustomBlends = null;

        /// <summary>
        /// Get the Unity Camera that is attached to this GameObject.  This is the camera
        /// that will be controlled by the brain.
        /// </summary>
        public Camera OutputCamera
        {
            get
            {
                if (m_OutputCamera == null)
                    m_OutputCamera = GetComponent<Camera>();
                return m_OutputCamera;
            }
        }
        private Camera m_OutputCamera = null; // never use directly - use accessor

        /// <summary>
        /// Because the PostProcessing package is not guaranteed to be present,
        /// we must handle PostFX in this opaque way.  This delegate will be called
        /// every frame (during LateUpdate) after the camera has been positioned.
        /// The intention is that the callback will make the right calls to the PostProcessing module.
        /// Cinemachine provides the CinemachinePostFX behaviour that makes use of this delegate.
        /// 
        /// Parameters:
        /// 
        /// * Camera dest: the Unity camera whose state has just been set
        /// * ICinemachineCamera vcam: the currently active virtual camera 
        /// * CameraState state: The state that has been applied to the Unity Camera 
        /// (may be different from vcam's stae, if blending)
        /// 
        /// Returns: True if PostFX were applied, false otherwise.
        /// </summary>
        public delegate bool PostFXHandlerDelegate(
            Camera dest, ICinemachineCamera vcam, CameraState state);

        /// <summary>
        /// Because the postFX package is not guaranteed to be present,
        /// we must handle it in this opaque way.  This delegate will be called
        /// every frame (during LateUpdtae) after the camera has been positioned.
        /// </summary>
        public PostFXHandlerDelegate PostFXHandler { get; set; }

        [Tooltip("This event will fire whenever a virtual camera goes live and there is no blend")]
        public UnityEvent m_CameraCutEvent;

        [Tooltip("This event will fire whenever a virtual camera goes live.  If a blend is involved, then the event will fire on the first frame of the blend.")]
        public UnityEvent m_CameraActivatedEvent;

        /// <summary>
        /// API for the Unity Editor.
        /// Show this camera no matter what.  This is static, and so affects all Cinemachine brains.
        /// </summary>
        public static ICinemachineCamera SoloCamera { get; set; }

        /// <summary>API for the Unity Editor.</summary>
        /// <returns>Color used to indicate that a camera is in Solo mode.</returns>
        public static Color GetSoloGUIColor() { return Color.Lerp(Color.red, Color.yellow, 0.8f); }

        /// <summary>Get the default world up for the virtual cameras.</summary>
        public Vector3 DefaultWorldUp
            { get { return (m_WorldUpOverride != null) ? m_WorldUpOverride.transform.up : Vector3.up; } }

        private ICinemachineCamera mActiveCameraPreviousFrame;
        private ICinemachineCamera mOutgoingCameraPreviousFrame;
        private CinemachineBlend mActiveBlend = null;
        private bool mPreviousFrameWasOverride = false;

        private class OverrideStackFrame
        {
            public int id = 0;
            public ICinemachineCamera camera = null;
            public CinemachineBlend blend = null;
            public float deltaTime = 0;
            public bool Active { get { return camera != null; } }
        }
        private List<OverrideStackFrame> mOverrideStack = new List<OverrideStackFrame>();
        private int mNextOverrideId = 1;

        /// Get the override and move it to the top of the stack
        private OverrideStackFrame GetOverrideFrame(int id)
        {
            foreach (OverrideStackFrame o in mOverrideStack)
                if (o.id == id)
                    return o;
            OverrideStackFrame ovr = new OverrideStackFrame();
            ovr.id = id;
            mOverrideStack.Insert(0, ovr);
            return ovr;
        }

        /// Clear the override stack if it's entirely inactive
        private void ClearOverrideStackIfInactive()
        {
            foreach (var o in mOverrideStack)
                if (o.Active)
                    return;
            mOverrideStack.Clear();
            mNextOverrideId = 1;
        }

        /// Get the next active blend on the stack.  Used when an override blends in from nothing.
        private OverrideStackFrame GetNextActiveFrame(int overrideId)
        {
            bool pastMine = false;
            foreach (OverrideStackFrame o in mOverrideStack)
            {
                if (o.id == overrideId)
                    pastMine = true;
                else if (o.Active && pastMine)
                    return o;
            }
            // Create a frame representing the non-override state (gameplay)
            OverrideStackFrame ovr = new OverrideStackFrame();
            ovr.camera = TopCameraFromPriorityQueue();
            ovr.blend = mActiveBlend;
            return ovr;
        }

        /// Get the first override that has a camera
        private OverrideStackFrame GetActiveOverride()
        {
            foreach (OverrideStackFrame o in mOverrideStack)
                if (o.Active)
                    return o;
            return null;
        }

        /// <summary>
        /// This API is specifically for Timeline.  Do not use it.
        /// Override the current camera and current blend.  This setting will trump
        /// any in-game logic that sets virtual camera priorities and Enabled states.
        /// This is the main API for the timeline.
        /// </summary>
        /// <param name="overrideId">Id to represent a specific client.  An internal
        /// stack is maintained, with the most recent non-empty override taking precenence.
        /// This id must be > 0.  If you pass -1, a new id will be created, and returned.
        /// Use that id for subsequent calls.  Don't forget to
        /// call ReleaseCameraOverride after all overriding is finished, to
        /// free the OverideStack resources.</param>
        /// <param name="camA"> The camera to set, corresponding to weight=0</param>
        /// <param name="camB"> The camera to set, corresponding to weight=1</param>
        /// <param name="weightB">The blend weight.  0=camA, 1=camB</param>
        /// <param name="deltaTime">override for deltaTime.  Should be Time.FixedDelta for
        /// time-based calculations to be included, 0 otherwise</param>
        /// <returns>The oiverride ID.  Don't forget to call ReleaseCameraOverride
        /// after all overriding is finished, to free the OverideStack resources.</returns>
        internal int SetCameraOverride(
            int overrideId,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime)
        {
            if (overrideId < 0)
                overrideId = mNextOverrideId++;

            OverrideStackFrame ovr = GetOverrideFrame(overrideId);
            ovr.camera = null;
            ovr.blend = null;
            ovr.deltaTime = deltaTime;
            if (camA != null || camB != null)
            {
                if (weightB <= Utility.UnityVectorExtensions.Epsilon)
                {
                    if (camA != null)
                        ovr.camera = camA; // no blend
                }
                else if (weightB >= (1f - Utility.UnityVectorExtensions.Epsilon))
                {
                    if (camB != null)
                        ovr.camera = camB; // no blend
                }
                else
                {
                    // We have a blend.  If one of the supplied cameras is null,
                    // we use the current active virtual camera (blending in/out of game logic),
                    // If we do have a null camera, make sure it's the 'from' camera.
                    // Timeline does not distinguish between from and to cams, but we do.
                    if (camB == null)
                    {
                        // Swap them
                        ICinemachineCamera c = camB;
                        camB = camA;
                        camA = c;
                        weightB = 1f - weightB;
                    }

                    // Are we blending with something in progress?
                    if (camA == null)
                    {
                        OverrideStackFrame frame = GetNextActiveFrame(overrideId);
                        if (frame.blend != null)
                            camA = new BlendSourceVirtualCamera(frame.blend, deltaTime);
                        else
                            camA = frame.camera != null ? frame.camera : camB;
                    }

                    // Create the override blend
                    ovr.blend = new CinemachineBlend(
                            camA, camB, AnimationCurve.Linear(0, 0, 1, 1), weightB);
                    ovr.camera = camB;
                }
            }
            return overrideId;
        }

        /// <summary>
        /// This API is specifically for Timeline.  Do not use it.
        /// Release the resources used for a camera override client.
        /// See SetCameraOverride.
        /// </summary>
        /// <param name="overrideId">The ID to released.  This is the value that
        /// was returned bu SetCameraOverride</param>
        internal void ReleaseCameraOverride(int overrideId)
        {
            foreach (OverrideStackFrame o in mOverrideStack)
            {
                if (o.id == overrideId)
                {
                    mOverrideStack.Remove(o);
                    return;
                }
            }
        }

        private void Awake()
        {
            IsSuspended = false;
        }

        private void OnEnable()
        {
            mActiveBlend = null;
            mActiveCameraPreviousFrame = null;
            mOutgoingCameraPreviousFrame = null;
            mPreviousFrameWasOverride = false;
            CinemachineCore.Instance.AddActiveBrain(this);
        }

        private void OnDisable()
        {
            CinemachineCore.Instance.RemoveActiveBrain(this);
            mActiveBlend = null;
            mActiveCameraPreviousFrame = null;
            mOutgoingCameraPreviousFrame = null;
            mPreviousFrameWasOverride = false;
            mOverrideStack.Clear();
        }

        private void Start()
        {
            UpdateVirtualCameras(CinemachineCore.UpdateFilter.Any, -1f);
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!m_ShowDebugText)
                CinemachineGameWindowDebug.ReleaseScreenPos(this);
            else
            {
                // Show the active camera and blend
                Color color = GUI.color;
                ICinemachineCamera vcam = ActiveVirtualCamera;
                string text = "CM " + gameObject.name + ": ";
                if (SoloCamera != null)
                {
                    text += "SOLO ";
                    GUI.color = GetSoloGUIColor();
                }
                if (ActiveBlend == null)
                    text += (vcam != null ? vcam.Name : "(none)");
                else
                    text += ActiveBlend.Description;
                Rect r = CinemachineGameWindowDebug.GetScreenPos(this, text, GUI.skin.box);
                GUI.Label(r, text, GUI.skin.box);
                GUI.color = color;
            }
        }
#endif
        private void FixedUpdate()
        {
            // We check in after the physics system has had a chance to move things
            StartCoroutine(AfterPhysics());
        }

        private IEnumerator AfterPhysics()
        {
            yield return new WaitForFixedUpdate();
            if (m_UpdateMethod == UpdateMethod.SmartUpdate)
            {
                AddSubframe(); // FixedUpdate can be called multiple times per frame
                UpdateVirtualCameras(CinemachineCore.UpdateFilter.Fixed, GetEffectiveDeltaTime(true));
            }
            else
            {
                if (m_UpdateMethod == UpdateMethod.LateUpdate)
                    msSubframes = 1;
                else
                {
                    AddSubframe(); // FixedUpdate can be called multiple times per frame
                    UpdateVirtualCameras(CinemachineCore.UpdateFilter.Any, GetEffectiveDeltaTime(true));
                }
            }
        }

        private void LateUpdate()
        {
            float deltaTime = GetEffectiveDeltaTime(true);
            if (m_UpdateMethod == UpdateMethod.SmartUpdate)
                UpdateVirtualCameras(CinemachineCore.UpdateFilter.Late, deltaTime);
            else if (m_UpdateMethod == UpdateMethod.LateUpdate)
                UpdateVirtualCameras(CinemachineCore.UpdateFilter.Any, deltaTime);

            // Choose the active vcam and apply it to the Unity camera
            ProcessActiveCamera(GetEffectiveDeltaTime(false));
        }

#if UNITY_EDITOR
        /// This is only needed in editor mode to force timeline to call OnGUI while
        /// timeline is up and the game is not running, in order to allow dragging
        /// the composer guide in the game view.
        private void OnPreCull()
        {
            if (!Application.isPlaying)
            {
                // Note: this call will cause any screen canvas attached to the camera
                // to be painted one frame out of sync.  It will only happen in the editor when not playing.
                float deltaTime = GetEffectiveDeltaTime(true);
                UpdateVirtualCameras(CinemachineCore.UpdateFilter.Any, deltaTime);
                ProcessActiveCamera(GetEffectiveDeltaTime(false));
            }
        }

#endif
        private float GetEffectiveDeltaTime(bool fixedDelta)
        {
            OverrideStackFrame activeOverride = GetActiveOverride();
            float deltaTime = (Application.isPlaying || SoloCamera != null)
                ? (fixedDelta ? Time.fixedDeltaTime : Time.deltaTime) : 0;
            if (activeOverride != null)
                deltaTime = activeOverride.deltaTime;
            return deltaTime;
        }

        private void UpdateVirtualCameras(CinemachineCore.UpdateFilter updateFilter, float deltaTime)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("CinemachineBrain.UpdateVirtualCameras");
            CinemachineCore.Instance.CurrentUpdateFilter = updateFilter;

            // We always update all active virtual cameras in the priority stack
            foreach (ICinemachineCamera cam in CinemachineCore.Instance.AllCameras)
                CinemachineCore.Instance.UpdateVirtualCamera(cam, DefaultWorldUp, deltaTime);

            // Make sure that the active camera gets updated this frame.
            // Only cameras that are enabled and in the priority stack
            // get automatically updated.
            ICinemachineCamera vcam = ActiveVirtualCamera;
            if (vcam != null)
                CinemachineCore.Instance.UpdateVirtualCamera(
                    vcam, DefaultWorldUp, deltaTime);

            CinemachineBlend activeBlend = ActiveBlend;
            if (activeBlend != null)
                activeBlend.UpdateCameraState(DefaultWorldUp, deltaTime);

            // Restore the filter for general use
            CinemachineCore.Instance.CurrentUpdateFilter = CinemachineCore.UpdateFilter.Any;
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        private void ProcessActiveCamera(float deltaTime)
        {
            // This condition should never occur, but let's be defensive
            if ((OutputCamera == null) || !OutputCamera.isActiveAndEnabled)
            {
                mActiveCameraPreviousFrame = null;
                mOutgoingCameraPreviousFrame = null;
                mPreviousFrameWasOverride = false;
                return;
            }

            //UnityEngine.Profiling.Profiler.BeginSample("CinemachineBrain.ProcessActiveCamera");
            OverrideStackFrame activeOverride = GetActiveOverride();
            ICinemachineCamera activeCamera = ActiveVirtualCamera;
            if (activeCamera == null)
                mOutgoingCameraPreviousFrame = null;
            else
            {
                // If there is an override, we kill the in-game blend
                if (activeOverride != null)
                    mActiveBlend = null;
                CinemachineBlend activeBlend = ActiveBlend;

                // Check for unexpected deletion of the cached mActiveCameraPreviousFrame
                if (mActiveCameraPreviousFrame != null && mActiveCameraPreviousFrame.VirtualCameraGameObject == null)
                    mActiveCameraPreviousFrame = null;

                // Are we transitioning cameras?
                if (mActiveCameraPreviousFrame != activeCamera)
                {
                    // Do we need to create a game-play blend?
                    if (mActiveCameraPreviousFrame != null
                        && !mPreviousFrameWasOverride
                        && activeOverride == null
                        && deltaTime > 0)
                    {
                        // Create a blend (will be null if a cut)
                        activeBlend = CreateBlend(
                                mActiveCameraPreviousFrame, activeCamera,
                                LookupBlendCurve(mActiveCameraPreviousFrame, activeCamera),
                                mActiveBlend);
                    }
                    // Need this check because Timeline override sometimes inverts outgoing and incoming
                    if (activeCamera != mOutgoingCameraPreviousFrame)
                    {
                        // If the incoming camera is disabled, then we must assume
                        // that it has not been updated properly
                        if (!activeCamera.VirtualCameraGameObject.activeInHierarchy
                            && (activeBlend == null || !activeBlend.Uses(activeCamera)))
                        {
                            activeCamera.UpdateCameraState(DefaultWorldUp, -1);
                        }
                        // Notify incoming camera of transition
                        activeCamera.OnTransitionFromCamera(mActiveCameraPreviousFrame);
                        if (m_CameraActivatedEvent != null)
                            m_CameraActivatedEvent.Invoke();
                    }
                    // If we're cutting without a blend, or no active cameras
                    // were active last frame, send an event
                    if (activeBlend == null
                        || (activeBlend.CamA != mActiveCameraPreviousFrame
                            && activeBlend.CamB != mActiveCameraPreviousFrame
                            && activeBlend.CamA != mOutgoingCameraPreviousFrame
                            && activeBlend.CamB != mOutgoingCameraPreviousFrame))
                    {
                        if (m_CameraCutEvent != null)
                            m_CameraCutEvent.Invoke();
                    }
                }

                // Advance the current blend (if any)
                if (activeBlend != null)
                {
                    if (activeOverride == null)
                        activeBlend.TimeInBlend += (deltaTime > 0)
                            ? deltaTime : activeBlend.Duration;
                    if (activeBlend.IsComplete)
                        activeBlend = null;
                }
                if (activeOverride == null)
                    mActiveBlend = activeBlend;

                // Apply the result to the Unity camera
                if (!IsSuspended)
                {
                    CameraState state = activeCamera.State;
                    if (activeBlend != null)
                        state = activeBlend.State;
                    PushStateToUnityCamera(state, OutputCamera, activeCamera);
                }

                mOutgoingCameraPreviousFrame = null;
                if (activeBlend != null)
                    mOutgoingCameraPreviousFrame = activeBlend.CamB;
            }
            mActiveCameraPreviousFrame = activeCamera;
            mPreviousFrameWasOverride = (activeOverride != null);
            if (mPreviousFrameWasOverride)
            {
                // Hack: Because we don't know whether blending in or out... grrr...
                if (activeOverride.blend != null)
                {
                    if (activeOverride.blend.BlendWeight < 0.5f)
                    {
                        mActiveCameraPreviousFrame = activeOverride.blend.CamA;
                        mOutgoingCameraPreviousFrame = activeOverride.blend.CamB;
                    }
                    else
                    {
                        mActiveCameraPreviousFrame = activeOverride.blend.CamB;
                        mOutgoingCameraPreviousFrame = activeOverride.blend.CamA;
                    }
                }
            }
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// True if this brain is suspended.  If so, scene Camera is not updated,
        /// although virtual cams are still updated.
        /// </summary>
        public bool IsSuspended { get; private set; }

        /// <summary>
        /// Is there a blend in progress?
        /// </summary>
        public bool IsBlending { get { return ActiveBlend != null && ActiveBlend.IsValid; } }

        /// <summary>
        /// Get the current blend in progress.  Returns null if none.
        /// </summary>
        public CinemachineBlend ActiveBlend
        {
            get
            {
                if (SoloCamera != null)
                    return null;
                OverrideStackFrame ovr = GetActiveOverride();
                return (ovr != null && ovr.blend != null) ? ovr.blend : mActiveBlend;
            }
        }

        /// <summary>
        /// True if the ICinemachineCamera the current active camera,
        /// or part of a current blend, either directly or indirectly because its parents are live.
        /// </summary>
        /// <param name="vcam">The camera to test whether it is live</param>
        /// <returns>True if the camera is live (directly or indirectly)
        /// or part of a blend in progress.</returns>
        public bool IsLive(ICinemachineCamera vcam)
        {
            if (IsLiveItself(vcam))
                return true;

            ICinemachineCamera parent = vcam.ParentCamera;
            while (parent != null && parent.LiveChildOrSelf == vcam)
            {
                if (IsLiveItself(parent))
                    return true;
                vcam = parent;
                parent = vcam.ParentCamera;
            }
            return false;
        }

        // True if this vcam (not considering parents) actually live.
        private bool IsLiveItself(ICinemachineCamera vcam)
        {
            if (mActiveCameraPreviousFrame == vcam)
                return true;
            if (ActiveVirtualCamera == vcam)
                return true;
            if (IsBlending && ActiveBlend.Uses(vcam))
                return true;
            return false;
        }

        /// <summary>
        /// Get the current active virtual camera.
        /// </summary>
        public ICinemachineCamera ActiveVirtualCamera
        {
            get
            {
                if (SoloCamera != null)
                    return SoloCamera;
                OverrideStackFrame ovr = GetActiveOverride();
                return (ovr != null && ovr.camera != null) ? ovr.camera : TopCameraFromPriorityQueue();
            }
        }

        /// <summary>
        /// Get the highest-priority Enabled ICinemachineCamera
        /// that is visible to my camera.  Culling Mask is used to test visibility.
        /// </summary>
        private ICinemachineCamera TopCameraFromPriorityQueue()
        {
            foreach (ICinemachineCamera cam in CinemachineCore.Instance.AllCameras)
            {
                GameObject go = cam != null ? cam.VirtualCameraGameObject : null;
                if (go != null && (OutputCamera.cullingMask & (1 << go.layer)) != 0)
                    return cam;
            }
            return null;
        }

        /// <summary>
        /// Create a blend curve for blending from one ICinemachineCamera to another.
        /// If there is a specific blend defined for these cameras it will be used, otherwise
        /// a default blend will be created, which could be a cut.
        /// </summary>
        private AnimationCurve LookupBlendCurve(
            ICinemachineCamera fromKey, ICinemachineCamera toKey)
        {
            // Get the blend curve that's most appropriate for these cameras
            AnimationCurve blendCurve = m_DefaultBlend.BlendCurve;
            if (m_CustomBlends != null)
            {
                string fromCameraName = (fromKey != null) ? fromKey.Name : string.Empty;
                string toCameraName = (toKey != null) ? toKey.Name : string.Empty;
                blendCurve = m_CustomBlends.GetBlendCurveForVirtualCameras(
                        fromCameraName, toCameraName, blendCurve);
            }
            return blendCurve;
        }

        /// <summary>
        /// Create a blend from one ICinemachineCamera to another,
        /// or to/from a point, if we can't do anything else
        /// </summary>
        private CinemachineBlend CreateBlend(
            ICinemachineCamera camA, ICinemachineCamera camB, AnimationCurve blendCurve,
            CinemachineBlend activeBlend)
        {
            if (blendCurve == null || blendCurve.keys.Length <= 1 || (camA == null && camB == null))
                return null;

            if (camA == null || activeBlend != null)
            {
                // Blend from the current camera position
                string name = "(none)";
                CameraState state = CameraState.Default;
                if (activeBlend != null)
                {
                    state = activeBlend.State;
                    name = "Mid-blend";
                }
                else if (OutputCamera != null)
                {
                    state.Lens = new LensSettings(OutputCamera);
                    state.RawPosition = OutputCamera.transform.position;
                    state.RawOrientation = OutputCamera.transform.rotation;
                }
                camA = new StaticPointVirtualCamera(state, name);
            }

            return new CinemachineBlend(camA, camB, blendCurve, 0);
        }

        /// <summary>
        /// Apply a cref="CameraState"/> to an a cref="Camera"/>
        /// </summary>
        private void PushStateToUnityCamera(CameraState state, Camera cam, ICinemachineCamera vcam)
        {
            cam.transform.position = state.FinalPosition;
            cam.transform.rotation = state.FinalOrientation;
            cam.fieldOfView = state.Lens.FieldOfView;
            cam.orthographicSize = state.Lens.OrthographicSize;
            cam.nearClipPlane = state.Lens.NearClipPlane;
            cam.farClipPlane = state.Lens.FarClipPlane;

            if (PostFXHandler != null && !PostFXHandler(cam, vcam, state))
                PostFXHandler(cam, null, state); // Restore default postFX
        }

        static int msCurrentFrame;
        static int msFirstBrainObjectId;
        static int msSubframes;
        void AddSubframe()
        {
            int now = Time.frameCount;
            if (now == msCurrentFrame)
            {
                if (msFirstBrainObjectId == GetInstanceID())
                    ++msSubframes;
            }
            else
            {
                msCurrentFrame = now;
                msFirstBrainObjectId = GetInstanceID();
                msSubframes = 1;
            }
        }

        /// <summary>API for CinemachineCore only: Get the number of subframes to
        /// update the virtual cameras.</summary>
        /// <returns>Number of subframes registered by the first brain's FixedUpdate</returns>
        internal static int GetSubframeCount() { return Math.Max(1, msSubframes); }
    }

    /// <summary>
    /// Point source for blending. It's not really a virtual camera, but takes
    /// a CameraState and exposes it as a virtual camera for the purposes of blending.
    /// </summary>
    internal class StaticPointVirtualCamera : ICinemachineCamera
    {
        public StaticPointVirtualCamera(CameraState state, string name) { State = state; Name = name; }
        public void SetState(CameraState state) { State = state; }

        public string Name { get; private set; }
        public int Priority { get; set; }
        public Transform LookAt { get; set; }
        public Transform Follow { get; set; }
        public CameraState State { get; private set; }
        public GameObject VirtualCameraGameObject { get { return null; } }
        public ICinemachineCamera LiveChildOrSelf { get { return this; } }
        public ICinemachineCamera ParentCamera { get { return null; } }
        public void UpdateCameraState(Vector3 worldUp, float deltaTime) {}
        public void OnTransitionFromCamera(ICinemachineCamera fromCam) {}
    }


    /// <summary>
    /// Blend result source for blending.   This exposes a CinemachineBlend object
    /// as an ersatz virtual camera for the purposes of blending.  This achieves the purpose
    /// of blending the result oif a blend.
    /// </summary>
    internal class BlendSourceVirtualCamera : ICinemachineCamera
    {
        public BlendSourceVirtualCamera(CinemachineBlend blend, float deltaTime)
        {
            Blend = blend;
            UpdateCameraState(blend.CamA.State.ReferenceUp, deltaTime);
        }

        public CinemachineBlend Blend { get; private set; }

        public string Name { get { return Blend.Description; }}
        public int Priority { get; set; }
        public Transform LookAt { get; set; }
        public Transform Follow { get; set; }
        public CameraState State { get; private set; }
        public GameObject VirtualCameraGameObject { get { return null; } }
        public ICinemachineCamera LiveChildOrSelf { get { return Blend.CamB; } }
        public ICinemachineCamera ParentCamera { get { return null; } }
        public CameraState CalculateNewState(float deltaTime) { return State; }
        public void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            Blend.UpdateCameraState(worldUp, deltaTime);
            State = Blend.State;
        }

        public void OnTransitionFromCamera(ICinemachineCamera fromCam) {}
    }
}
