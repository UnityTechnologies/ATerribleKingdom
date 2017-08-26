using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Body section of the component pipeline. 
    /// Its job is to position the camera in a fixed relationship to the vcam's Follow 
    /// target object, with offsets and damping.
    /// 
    /// The Tansposer will only change the camera's position in space.  It will not 
    /// re-orient or otherwise aim the camera.  To to that, you need to instruct 
    /// the vcam in the Aim section of its pipeline.
    /// </summary>
    [DocumentationSorting(5, DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [RequireComponent(typeof(CinemachinePipeline))]
    [SaveDuringPlay]
    public class CinemachineTransposer : MonoBehaviour, ICinemachineComponent
    {
        /// <summary>The distance which the transposer will attempt to maintain from the transposer subject</summary>
        [Tooltip("The distance vector that the transposer will attempt to maintain from the Follow target")]
        public Vector3 m_FollowOffset = Vector3.back * 10f;

        /// <summary>How aggressively the camera tries to maintain the offset in the X-axis.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// x-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain the offset in the X-axis.  Small numbers are more responsive, rapidly translating the camera to keep the target's x-axis offset.  Larger numbers give a more heavy slowly responding camera. Using different settings per axis can yield a wide range of camera behaviors.")]
        public float m_XDamping = 1f;

        /// <summary>How aggressively the camera tries to maintain the offset in the Y-axis.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// y-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain the offset in the Y-axis.  Small numbers are more responsive, rapidly translating the camera to keep the target's y-axis offset.  Larger numbers give a more heavy slowly responding camera. Using different settings per axis can yield a wide range of camera behaviors.")]
        public float m_YDamping = 1f;

        /// <summary>How aggressively the camera tries to maintain the offset in the Z-axis.
        /// Small numbers are more responsive, rapidly translating the camera to keep the
        /// target's z-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain the offset in the Z-axis.  Small numbers are more responsive, rapidly translating the camera to keep the target's z-axis offset.  Larger numbers give a more heavy slowly responding camera. Using different settings per axis can yield a wide range of camera behaviors.")]
        public float m_ZDamping = 1f;

        /// <summary>
        /// The coordinate space to use when interpreting the offset from the target
        /// </summary>
        [DocumentationSorting(5.01f, DocumentationSortingAttribute.Level.UserRef)]
        public enum BindingMode
        {
            /// <summary>
            /// Camera will be bound to the Follow target using a frame of reference consisting
            /// of the target's local frame at the moment when the virtual camera was enabled,
            /// or when the target was assigned.
            /// </summary>
            LockToTargetOnAssign = 0,
            /// <summary>
            /// Camera will be bound to the Follow target using a frame of reference consisting
            /// of the target's local frame, with the tilt and roll zeroed out.
            /// </summary>
            LockToTargetWithWorldUp = 1,
            /// <summary>
            /// Camera will be bound to the Follow target using a frame of reference consisting
            /// of the target's local frame, with the roll zeroed out.
            /// </summary>
            LockToTargetNoRoll = 2,
            /// <summary>
            /// Camera will be bound to the Follow target using the target's local frame.
            /// </summary>
            LockToTarget = 3,
            /// <summary>
            /// Camera will be bound to the Follow target using a world space offset.
            /// </summary>
            WorldSpace = 4
        }
        /// <summary>The coordinate space to use when interpreting the offset from the target</summary>
        [Space]
        [Tooltip("The coordinate space to use when interpreting the offset from the target.  This is also used to set the camera's Up vector, which will be maintained when aiming the camera.")]
        public BindingMode m_BindingMode = BindingMode.LockToTargetWithWorldUp;

        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's X angle.  Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_PitchDamping = 0;

        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's Y angle.  Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_YawDamping = 0;

        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's Z angle.  Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_RollDamping = 0f;

        /// <summary>True if component is enabled and has a valid Follow target</summary>
        public bool IsValid
        { 
            get { return enabled && VirtualCamera.Follow != null; } 
        }

        /// <summary>Get the Cinemachine Virtual Camera affected by this component</summary>
        public ICinemachineCamera VirtualCamera
        { 
            get { return gameObject.transform.parent.gameObject.GetComponent<ICinemachineCamera>(); } 
        }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Body; } }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If 0 or less, no damping is done.</param>
        public virtual void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            InitPrevFrameStateInfo(ref curState, deltaTime);
            if (IsValid)
                DoTracking(ref curState, deltaTime, 
                    GetReferenceOrientation(curState.ReferenceUp), m_FollowOffset);
        }

        /// <summary>Initializes the state for previous frame if appropriate.</summary>
        protected void InitPrevFrameStateInfo(
            ref CameraState curState, float deltaTime)
        {
            if (m_previousTarget != VirtualCamera.Follow || deltaTime <= 0)
            {
                m_previousTarget = VirtualCamera.Follow;
                m_targetOrientationOnAssign 
                    = (m_previousTarget == null) ? Quaternion.identity : VirtualCamera.Follow.rotation;
            }
            if (deltaTime <= 0)
            {
                m_PreviousCameraPosition = curState.RawPosition;
                m_ReferenceOrientationPrevFrame = GetReferenceOrientation(curState.ReferenceUp);
            }
        }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If 0 or less, no damping is done.</param>
        /// <param name="rotation">Rotattion about the reverece orientation up axis</param>
        /// <param name="offset">Local positional camera offset relative to target</param>
        protected void DoTracking(
            ref CameraState curState, float deltaTime,
            Quaternion targetOrientation, Vector3 followOffset)
        {
            Quaternion dampedOrientation = targetOrientation;
            if (deltaTime > 0)
            {
                Vector3 relative = (Quaternion.Inverse(m_ReferenceOrientationPrevFrame) 
                    * targetOrientation).eulerAngles;
                Vector3 damping = AngularDamping;
                for (int i = 0; i < 3; ++i)
                {
                    if (relative[i] > 180)
                        relative[i] -= 360;
                    if (Mathf.Abs(relative[i]) > UnityVectorExtensions.Epsilon)
                        relative[i] *= deltaTime / Mathf.Max(damping[i], deltaTime);
                }
                dampedOrientation = m_ReferenceOrientationPrevFrame * Quaternion.Euler(relative);
            }
            Quaternion orientationDelta 
                = dampedOrientation * Quaternion.Inverse(m_ReferenceOrientationPrevFrame);
            m_ReferenceOrientationPrevFrame = dampedOrientation;

            Vector3 targetPosition = VirtualCamera.Follow.position;
            Vector3 currentPosition 
                = (orientationDelta * (m_PreviousCameraPosition - targetPosition)) + targetPosition;
            Vector3 newPosition = targetPosition + (dampedOrientation * followOffset);
            Vector3 worldOffset = newPosition - currentPosition;

            // Adjust for damping, which is done in target-local coords
            if (deltaTime > 0)
            {
                Vector3 localOffset = Quaternion.Inverse(targetOrientation) * worldOffset;
                Vector3 damping = Damping;
                for (int i = 0; i < 3; ++i)
                    if (Mathf.Abs(localOffset[i]) > UnityVectorExtensions.Epsilon)
                        localOffset[i] *= deltaTime / Mathf.Max(damping[i], deltaTime);
                worldOffset = targetOrientation * localOffset;
            }
            curState.RawPosition = m_PreviousCameraPosition = currentPosition + worldOffset;
            curState.ReferenceUp = dampedOrientation * Vector3.up;
        }

        /// <summary>
        /// Damping speeds for each of the 3 axes of the offset from target
        /// </summary>
        protected Vector3 Damping
        {
            get { return new Vector3(m_XDamping, m_YDamping, m_ZDamping) * kDampingScale; } 
        }

        /// <summary>
        /// Damping speeds for each of the 3 axes of the target's rotation
        /// </summary>
        protected Vector3 AngularDamping
        {
            get 
            { 
                switch (m_BindingMode)
                {
                    case BindingMode.LockToTargetNoRoll:
                        return new Vector3(m_PitchDamping, m_YawDamping, 0) * kDampingScale; 
                    case BindingMode.LockToTargetWithWorldUp:
                        return new Vector3(0, m_YawDamping, 0) * kDampingScale; 
                    case BindingMode.LockToTargetOnAssign:
                    case BindingMode.WorldSpace:
                        return Vector3.zero;
                    default:
                        return new Vector3(m_PitchDamping, m_YawDamping, m_RollDamping) * kDampingScale; 
                }
            } 
        }

        const float kDampingScale = 0.1f;

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        public Vector3 GeTargetCameraPosition(Vector3 worldUp)
        {
            if (!IsValid)
                return Vector3.zero;
            return VirtualCamera.Follow.position + GetReferenceOrientation(worldUp) * m_FollowOffset;
        }

        /// <summary>State information for damping</summary>
        Vector3 m_PreviousCameraPosition = Vector3.zero;
        Quaternion m_ReferenceOrientationPrevFrame = Quaternion.identity;
        Quaternion m_targetOrientationOnAssign = Quaternion.identity;
        Transform m_previousTarget = null;

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        public Quaternion GetReferenceOrientation(Vector3 worldUp)
        {
            if (VirtualCamera.Follow != null)
            {
                Quaternion targetOrientation = VirtualCamera.Follow.rotation;
                switch (m_BindingMode)
                {
                    case BindingMode.LockToTargetOnAssign:
                        return m_targetOrientationOnAssign;
                    case BindingMode.LockToTargetWithWorldUp:
                        return Quaternion.AngleAxis(targetOrientation.eulerAngles.y, worldUp);
                    case BindingMode.LockToTargetNoRoll:
                        return Quaternion.LookRotation(targetOrientation * Vector3.forward, worldUp);
                    case BindingMode.LockToTarget:
                        return targetOrientation;
                }
            }
            return Quaternion.identity; // world space
        }
    }
}
