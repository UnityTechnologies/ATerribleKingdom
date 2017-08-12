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
        /// Selects the coordinate space for the <c>Transposer</c> to use for its offsets
        /// </summary>
        [DocumentationSorting(5.01f, DocumentationSortingAttribute.Level.UserRef)]
        public enum TransposerOffsetType
        {
            /// <summary>
            /// Camera offset from target will be computed using a frame of reference consisting
            /// of the target's local frame at the moment when the virtual camera was enabled,
            /// or when the target was assigned.
            /// </summary>
            LockToTargetOnAssign = 0,
            /// <summary>
            /// Camera offset from target will be computed using a frame of reference consisting
            /// of the target's local frame, with the tilt and roll zeroed out.
            /// </summary>
            LockToTargetWithWorldUp = 1,
            /// <summary>
            /// Camera offset from target will be computed using a frame of reference consisting
            /// of the target's local frame, with the roll zeroed out.
            /// </summary>
            LockToTargetNoRoll = 2,
            /// <summary>
            /// Camera offset from target will be computed using the target's local frame.
            /// </summary>
            LockToTarget = 3,
            /// <summary>
            /// Camera offset from target will be computed using world space.
            /// </summary>
            WorldSpace = 4
        }
        /// <summary>The coordinate space to use when interpreting the offset from the target</summary>
        [Tooltip("The coordinate space to use when interpreting the offset from the target.  This is also used to set the camera's Up vector, which will be maintained when aiming the camera.")]
        public TransposerOffsetType m_BindingMode = TransposerOffsetType.LockToTargetWithWorldUp;

        /// <summary>True if component is enabled and has a valid Follow target</summary>
        public bool IsValid
        { get { return enabled && VirtualCamera.Follow != null; } }

        /// <summary>Get the Cinemachine Virtual Camera affected by this component</summary>
        public ICinemachineCamera VirtualCamera
        { get { return gameObject.transform.parent.gameObject.GetComponent<ICinemachineCamera>(); } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Body; } }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="statePrevFrame">The camera state on the previous frame (unused)</param>
        /// <param name="deltaTime">Used for damping.  If 0 or less, no damping is done.</param>
        /// <returns>curState with new RawPosition</returns>
        public virtual CameraState MutateCameraState(
            CameraState curState, CameraState statePrevFrame, float deltaTime)
        {
            if (!IsValid)
                return curState;

            if (m_previousTarget != VirtualCamera.Follow || deltaTime <= 0)
            {
                m_targetOrientationOnAssign = VirtualCamera.Follow.rotation;
                m_previousTarget = VirtualCamera.Follow;
            }

            // Where to put the camera
            Vector3 targetPosition = GetDesiredTargetPosition();
            Vector3 worldOffset = statePrevFrame.RawPosition - targetPosition;

            // Adjust for damping, which is done in local coords
            if (deltaTime > 0)
            {
                Quaternion localToWorldTransform = GetReferenceOrientation();
                Vector3 localOffset = Quaternion.Inverse(localToWorldTransform) * worldOffset;
                Vector3 damping = Damping;
                for (int i = 0; i < 3; ++i)
                    localOffset[i] *= deltaTime / Mathf.Max(damping[i], deltaTime);
                worldOffset = localToWorldTransform * localOffset;
            }
            CameraState newState = curState;
            newState.RawPosition = statePrevFrame.RawPosition - worldOffset;

            if (m_BindingMode == TransposerOffsetType.LockToTarget)
                newState.ReferenceUp = GetReferenceOrientation() * Vector3.up;

            return newState;
        }

        /// <summary>
        /// Damping speeds for each of the 3 axes of the offset from target
        /// </summary>
        protected Vector3 Damping
        { get { return new Vector3(m_XDamping, m_YDamping, m_ZDamping) * kHumanReadableDampingScalar; } }
        const float kHumanReadableDampingScalar = 0.1f;

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        public Vector3 GetDesiredTargetPosition()
        {
            if (!IsValid)
                return Vector3.zero;
            return VirtualCamera.Follow.position
                + GetReferenceOrientation() * m_FollowOffset;
        }

        Quaternion m_targetOrientationOnAssign = Quaternion.identity;
        Transform m_previousTarget = null;
        Quaternion GetReferenceOrientation()
        {
            Quaternion targetOrientation = VirtualCamera.Follow.rotation;
            switch (m_BindingMode)
            {
                case TransposerOffsetType.LockToTargetOnAssign:
                    return m_targetOrientationOnAssign;
                case TransposerOffsetType.LockToTargetWithWorldUp:
                    return Quaternion.AngleAxis(targetOrientation.eulerAngles.y, Vector3.up);
                case TransposerOffsetType.LockToTargetNoRoll:
                    return Quaternion.LookRotation(targetOrientation * Vector3.forward, Vector3.up);
                case TransposerOffsetType.LockToTarget:
                    return targetOrientation;
            }
            return Quaternion.identity; // world space
        }
    }
}
