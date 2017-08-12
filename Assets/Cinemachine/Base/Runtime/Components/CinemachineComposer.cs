using UnityEngine;
using System;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to aim the camera at the vcam's LookAt target object, with 
    /// configurable offsets, damping, and composition rules.
    /// 
    /// The composer does not change the camera's position.  It will only pan and tilt the 
    /// camera where it is, in order to get the desired framing.  To move the camera, you have
    /// to use the virtual camera's Body section.
    /// </summary>
    [DocumentationSorting(3, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode] // for OnGUI
    [AddComponentMenu("")] // Don't display in add component menu
    [RequireComponent(typeof(CinemachinePipeline))]
    [SaveDuringPlay]
    public class CinemachineComposer : MonoBehaviour, ICinemachineComponent
    {
        /// <summary>Used by the Inspector Editor to display on-screen guides.</summary>
        [NoSaveDuringPlay, HideInInspector]
        public Action OnGUICallback = null;

        /// <summary>This displays the Composer overlay in the Game window.
        /// You can adjust colours and opacity in Edit/Preferences/Cinemachine</summary>
        [Tooltip("This displays the Composer overlay in the Game window.  You can adjust colours and opacity in Edit/Preferences/Cinemachine.")]
        [NoSaveDuringPlay]
        public bool m_ShowGuides = true;

        /// <summary>Target offset from the object's center in LOCAL space which
        /// the Composer tracks. Use this to fine-tune the tracking target position
        /// when the desired area is not in the tracked object's center</summary>
        [Tooltip("Target offset from the target object's center in target-local space. Use this to fine-tune the tracking target position when the desired area is not the tracked object's center.")]
        public Vector3 m_TrackedObjectOffset = Vector3.zero;

        /// <summary>How aggressively the camera tries to follow the target in the screen-horizontal direction.
        /// Small numbers are more responsive, rapidly orienting the camera to keep the target in
        /// the dead zone. Larger numbers give a more heavy slowly responding camera.
        /// Using different vertical and horizontal settings can yield a wide range of camera behaviors.</summary>
        [Space]
        [Range(0f, 100f)]
        [Tooltip("How aggressively the camera tries to follow the target in the screen-horizontal direction. Small numbers are more responsive, rapidly orienting the camera to keep the target in the dead zone. Larger numbers give a more heavy slowly responding camera. Using different vertical and horizontal settings can yield a wide range of camera behaviors.")]
        public float m_HorizontalDamping = 0.5f;

        /// <summary>How aggressively the camera tries to follow the target in the screen-vertical direction. 
        /// Small numbers are more responsive, rapidly orienting the camera to keep the target in 
        /// the dead zone. Larger numbers give a more heavy slowly responding camera. Using different vertical 
        /// and horizontal settings can yield a wide range of camera behaviors.</summary>
        [Range(0f, 100f)]
        [Tooltip("How aggressively the camera tries to follow the target in the screen-vertical direction. Small numbers are more responsive, rapidly orienting the camera to keep the target in the dead zone. Larger numbers give a more heavy slowly responding camera. Using different vertical and horizontal settings can yield a wide range of camera behaviors.")]
        public float m_VerticalDamping = 0.5f;

        /// <summary>Horizontal screen position for target. The camera will rotate to the position the tracked object here</summary>
        [Space]
        [Range(0f, 1f)]
        [Tooltip("Horizontal screen position for target. The camera will rotate to position the tracked object here.")]
        public float m_ScreenX = 0.5f;

        /// <summary>Vertical screen position for target, The camera will rotate to to position the tracked object here</summary>
        [Range(0f, 1f)]
        [Tooltip("Vertical screen position for target, The camera will rotate to position the tracked object here.")]
        public float m_ScreenY = 0.5f;

        /// <summary>Camera will not rotate horizontally if the target is within this range of the position</summary>
        [Range(0f, 1f)]
        [Tooltip("Camera will not rotate horizontally if the target is within this range of the position.")]
        public float m_DeadZoneWidth = 0.1f;

        /// <summary>Camera will not rotate vertically if the target is within this range of the position</summary>
        [Range(0f, 1f)]
        [Tooltip("Camera will not rotate vertically if the target is within this range of the position.")]
        public float m_DeadZoneHeight = 0.1f;

        /// <summary>When target is within this region, camera will gradually move to re-align
        /// towards the desired position, depending onm the damping speed</summary>
        [Range(0f, 2f)]
        [Tooltip("When target is within this region, camera will gradually rotate horizontally to re-align towards the desired position, depending on the damping speed.")]
        public float m_SoftZoneWidth = 0.8f;

        /// <summary>When target is within this region, camera will gradually move to re-align
        /// towards the desired position, depending onm the damping speed</summary>
        [Range(0f, 2f)]
        [Tooltip("When target is within this region, camera will gradually rotate vertically to re-align towards the desired position, depending on the damping speed.")]
        public float m_SoftZoneHeight = 0.8f;

        /// <summary>A non-zero bias will move the targt position away from the center of the soft zone</summary>
        [Range(-0.5f, 0.5f)]
        [Tooltip("A non-zero bias will move the target position horizontally away from the center of the soft zone.")]
        public float m_BiasX = 0f;

        /// <summary>A non-zero bias will move the targt position away from the center of the soft zone</summary>
        [Range(-0.5f, 0.5f)]
        [Tooltip("A non-zero bias will move the target position vertically away from the center of the soft zone.")]
        public float m_BiasY = 0f;

        /// <summary>True if component is enabled and has a LookAt defined</summary>
        public virtual bool IsValid { get { return enabled && VirtualCamera.LookAt != null; } }

        /// <summary>Get the Cinemachine Virtual Camera affected by this component</summary>
        public ICinemachineCamera VirtualCamera
        { get { return gameObject.transform.parent.gameObject.GetComponent<ICinemachineCamera>(); } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Aim; } }

        /// <summary>Applies the composer rules and orients the camera accordingly</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="statePrevFrame">The camera state on the previous frame (unused)</param>
        /// <param name="deltaTime">Used for calculating damping.  If less than
        /// or equal to zero, then target will snap to the center of the dead zone.</param>
        /// <returns>curState with RawOrientation applied</returns>
        public virtual CameraState MutateCameraState(
            CameraState curState, CameraState statePrevFrame, float deltaTime)
        {
            if (!IsValid || !curState.HasLookAt)
                return curState;

            CameraState newState = curState;
            newState.ReferenceLookAt = GetTrackedPoint(newState.ReferenceLookAt);
            float targetDistance = (newState.ReferenceLookAt - newState.CorrectedPosition).magnitude;
            if (targetDistance < UnityVectorExtensions.Epsilon)
                return newState;  // navel-gazing, get outa here

            // Calculate effective fov - fake it for ortho based on target distance
            float fov, fovH;
            if (newState.Lens.Orthographic)
            {
                fov = Mathf.Rad2Deg * 2 * Mathf.Atan(newState.Lens.OrthographicSize / targetDistance);
                fovH = Mathf.Rad2Deg * 2 * Mathf.Atan(newState.Lens.Aspect * newState.Lens.OrthographicSize / targetDistance);
            }
            else
            {
                fov = newState.Lens.FieldOfView;
                double radHFOV = 2 * Math.Atan(Math.Tan(fov * Mathf.Deg2Rad / 2) * newState.Lens.Aspect);
                fovH = (float)(Mathf.Rad2Deg * radHFOV);
            }

            Rect softGuideFOV = ScreenToFOV(SoftGuideRect, fov, fovH, newState.Lens.Aspect);
            if (deltaTime <= 0)
            {
                // No damping, just snap to central bounds, skipping the soft zone
                Rect rect = new Rect(softGuideFOV.center, Vector2.zero); // Force to center
                newState.RawOrientation = PlaceWithinScreenBounds(
                        ref newState, rect, newState.RawOrientation, fov, fovH, 0);
            }
            else
            {
                // Start with previous frame's orientation (but with current up)
                Quaternion rigOrientation = Quaternion.LookRotation(
                        statePrevFrame.RawOrientation * Vector3.forward, newState.ReferenceUp);

                // First force the previous rotation into the hard bounds, no damping
                Rect hardGuideFOV = ScreenToFOV(HardGuideRect, fov, fovH, newState.Lens.Aspect);
                newState.RawOrientation = PlaceWithinScreenBounds(
                        ref newState, hardGuideFOV, rigOrientation, fov, fovH, 0);

                // Now move it through the soft zone, with damping
                newState.RawOrientation = PlaceWithinScreenBounds(
                        ref newState, softGuideFOV, newState.RawOrientation, fov, fovH, deltaTime);
            }
            newState.RawOrientation = UnityQuaternionExtensions.Normalized(newState.RawOrientation);
            return newState;
        }

        protected const float kHumanReadableDampingScale = 0.1f;

#if UNITY_EDITOR
        private void OnGUI() { if (OnGUICallback != null) OnGUICallback(); }
#endif
        private float HorizontalSoftTrackingSpeed
            { get { return m_HorizontalDamping * kHumanReadableDampingScale; } }
        private float VerticalSoftTrackingSpeed
            { get { return m_VerticalDamping * kHumanReadableDampingScale; } }

        /// <summary>Internal API for the inspector editor</summary>
        public Rect SoftGuideRect
        {
            get
            {
                return new Rect(
                    m_ScreenX - m_DeadZoneWidth / 2, m_ScreenY - m_DeadZoneHeight / 2,
                    m_DeadZoneWidth, m_DeadZoneHeight);
            }
            set
            {
                m_DeadZoneWidth = Mathf.Clamp01(value.width);
                m_DeadZoneHeight = Mathf.Clamp01(value.height);
                m_ScreenX = Mathf.Clamp01(value.x + m_DeadZoneWidth / 2);
                m_ScreenY = Mathf.Clamp01(value.y + m_DeadZoneHeight / 2);
                m_SoftZoneWidth = Mathf.Max(m_SoftZoneWidth, m_DeadZoneWidth);
                m_SoftZoneHeight = Mathf.Max(m_SoftZoneHeight, m_DeadZoneHeight);
            }
        }

        /// <summary>Internal API for the inspector editor</summary>
        public Rect HardGuideRect
        {
            get
            {
                Rect r = new Rect(
                        m_ScreenX - m_SoftZoneWidth / 2, m_ScreenY - m_SoftZoneHeight / 2,
                        m_SoftZoneWidth, m_SoftZoneHeight);
                r.position += new Vector2(
                        m_BiasX * (m_SoftZoneWidth - m_DeadZoneWidth),
                        m_BiasY * (m_SoftZoneHeight - m_DeadZoneHeight));
                return r;
            }
            set
            {
                m_SoftZoneWidth = Mathf.Clamp(value.width, 0, 2f);
                m_SoftZoneHeight = Mathf.Clamp(value.height, 0, 2f);
                m_DeadZoneWidth = Mathf.Min(m_DeadZoneWidth, m_SoftZoneWidth);
                m_DeadZoneHeight = Mathf.Min(m_DeadZoneHeight, m_SoftZoneHeight);

                Vector2 center = value.center;
                Vector2 bias = center - new Vector2(m_ScreenX, m_ScreenY);
                float biasWidth = Mathf.Max(0, m_SoftZoneWidth - m_DeadZoneWidth);
                float biasHeight = Mathf.Max(0, m_SoftZoneHeight - m_DeadZoneHeight);
                m_BiasX = biasWidth < UnityVectorExtensions.Epsilon ? 0 : Mathf.Clamp(bias.x / biasWidth, -0.5f, 0.5f);
                m_BiasY = biasHeight < UnityVectorExtensions.Epsilon ? 0 : Mathf.Clamp(bias.y / biasHeight, -0.5f, 0.5f);
            }
        }
        
        // Convert from screen coords to normalized FOV angular coords
        private Rect ScreenToFOV(Rect rScreen, float fov, float fovH, float aspect)
        {
            Rect r = new Rect(rScreen);
            Matrix4x4 persp = Matrix4x4.Perspective(fov, aspect, 0.01f, 10000f).inverse;

            Vector3 p = persp.MultiplyPoint(new Vector3(0, (r.yMin * 2f) - 1f, 0.1f)); p.z = -p.z;
            float angle = UnityVectorExtensions.SignedAngle(Vector3.forward, p, Vector3.left);
            r.yMin = ((fov / 2) + angle) / fov;

            p = persp.MultiplyPoint(new Vector3(0, (r.yMax * 2f) - 1f, 0.1f)); p.z = -p.z;
            angle = UnityVectorExtensions.SignedAngle(Vector3.forward, p, Vector3.left);
            r.yMax = ((fov / 2) + angle) / fov;

            p = persp.MultiplyPoint(new Vector3((r.xMin * 2f) - 1f, 0, 0.1f));  p.z = -p.z;
            angle = UnityVectorExtensions.SignedAngle(Vector3.forward, p, Vector3.up);
            r.xMin = ((fovH / 2) + angle) / fovH;

            p = persp.MultiplyPoint(new Vector3((r.xMax * 2f) - 1f, 0, 0.1f));  p.z = -p.z;
            angle = UnityVectorExtensions.SignedAngle(Vector3.forward, p, Vector3.up);
            r.xMax = ((fovH / 2) + angle) / fovH;
            return r;
        }

        /// <summary>Apply the target offsets to the target location.</summary>
        /// <param name="lookAt">The unoffset LookAt point</param>
        /// <returns>The LookAt point with the offset applied</returns>
        protected virtual Vector3 GetTrackedPoint(Vector3 lookAt)
        {
            Vector3 offset = Vector3.zero;
            if (VirtualCamera.LookAt != null)
                offset = VirtualCamera.LookAt.transform.rotation * m_TrackedObjectOffset;
            return lookAt + offset;
        }

        /// <summary>
        /// Adjust the rigOrientation to put the camera within the screen bounds.
        /// If deltaTime >= 0 then damping will be applied.
        /// Assumes that currentOrientation fwd is such that input rigOrientation's
        /// local up is NEVER NEVER NEVER pointing downwards, relative to
        /// state.ReferenceUp.  If this condition is violated
        /// then you will see crazy spinning.  That's the symptom.
        /// </summary>
        private Quaternion PlaceWithinScreenBounds(
            ref CameraState state, Rect screenRect,
            Quaternion rigOrientation, float fov, float fovH, float deltaTime)
        {
            Vector3 targetDir = state.ReferenceLookAt - state.CorrectedPosition;
            Vector2 rotToRect = rigOrientation.GetCameraRotationToTarget(targetDir, state.ReferenceUp);

            // Bring it to the edge of screenRect, if outside.  Leave it alone if inside.
            ClampVerticalBounds(ref screenRect, targetDir, state.ReferenceUp, fov);
            float min = (screenRect.yMin - 0.5f) * fov;
            float max = (screenRect.yMax - 0.5f) * fov;
            if (rotToRect.x < min)
                rotToRect.x -= min;
            else if (rotToRect.x > max)
                rotToRect.x -= max;
            else
                rotToRect.x = 0;

            min = (screenRect.xMin - 0.5f) * fovH;
            max = (screenRect.xMax - 0.5f) * fovH;
            if (rotToRect.y < min)
                rotToRect.y -= min;
            else if (rotToRect.y > max)
                rotToRect.y -= max;
            else
                rotToRect.y = 0;

            // Apply damping
            if (deltaTime > 0)
            {
                rotToRect.x *= deltaTime / Mathf.Max(VerticalSoftTrackingSpeed, deltaTime);
                rotToRect.y *= deltaTime / Mathf.Max(HorizontalSoftTrackingSpeed, deltaTime);
            }

            // Rotate
            return rigOrientation.ApplyCameraRotation(rotToRect, state.ReferenceUp);
        }

        /// <summary>
        /// Prevent upside-down camera situation.  This can happen if we have a high
        /// camera pitch combined with composer settings that cause the camera to tilt
        /// beyond the vertical in order to produce the desired framing.  We prevent this by
        /// clamping the composer's vertical settings so that this situation can't happen.
        /// </summary>
        private bool ClampVerticalBounds(ref Rect r, Vector3 dir, Vector3 up, float fov)
        {
            float angle = Vector3.Angle(dir, up);
            float halfFov = (fov / 2f) + 1; // give it a little extra to accommodate precision errors
            if (angle < halfFov)
            {
                // looking up
                float maxY = 1f - (halfFov - angle) / fov;
                if (r.yMax > maxY)
                {
                    r.yMin = Mathf.Min(r.yMin, maxY);
                    r.yMax = Mathf.Min(r.yMax, maxY);
                    return true;
                }
            }
            if (angle > (180 - halfFov))
            {
                // looking down
                float minY = (angle - (180 - halfFov)) / fov;
                if (minY > r.yMin)
                {
                    r.yMin = Mathf.Max(r.yMin, minY);
                    r.yMax = Mathf.Max(r.yMax, minY);
                    return true;
                }
            }
            return false;
        }
    }
}
