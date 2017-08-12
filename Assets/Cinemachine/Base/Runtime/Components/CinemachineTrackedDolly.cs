using UnityEngine;
using System;

namespace Cinemachine
{
    /// <summary>
    /// A Cinemachine Virtual Camera Body component that constrains camera motion
    /// to a CinemachinePath.  The camera can move along the path.
    /// 
    /// This behaviour can operate in two modes: manual positioning, and Auto-Dolly positioning.  
    /// In Manual mode, the camera's position is specified by animating the Path Position field.  
    /// In Auto-Dolly mode, the Path Position field is animated automatically every frame by finding
    /// the position on the path that's closest to the virtual camera's Follow target.
    /// </summary>
    [DocumentationSorting(7, DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [RequireComponent(typeof(CinemachinePipeline))]
    [SaveDuringPlay]
    public class CinemachineTrackedDolly : MonoBehaviour, ICinemachineComponent
    {
        /// <summary>The path to which the camera will be constrained.  This must be non-null.</summary>
        [Tooltip("The path to which the camera will be constrained.  This must be non-null.")]
        public CinemachinePathBase m_Path;

        /// <summary>The position along the path at which the camera will be placed.
        /// This can be animated directly, or set automatically by the Auto-Dolly feature
        /// to get as close as possible to the Follow target.</summary>
        [Tooltip("The position along the path at which the camera will be placed.  This can be animated directly, or set automatically by the Auto-Dolly feature to get as close as possible to the Follow target.  Values are as follows: 0 represents the first waypoint on the path, 1 is the second, and so on.  Values in-between are points on the path in between the waypoints.")]
        public float m_PathPosition;

        /// <summary>Where to put the camera realtive to the path postion.  X is perpendicular to the path, Y is up, and Z is parallel to the path.</summary>
        [Tooltip("Where to put the camera relative to the path position.  X is perpendicular to the path, Y is up, and Z is parallel to the path.  This allows the camera to be offset from the path itself (as if on a tripod, for example).")]
        public Vector3 m_PathOffset = Vector3.zero;

        /// <summary>How aggressively the camera tries to maintain the offset perpendicular to the path.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// x-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain its position in a direction perpendicular to the path.  Small numbers are more responsive, rapidly translating the camera to keep the target's x-axis offset.  Larger numbers give a more heavy slowly responding camera. Using different settings per axis can yield a wide range of camera behaviors.")]
        public float m_XDamping = 0f;

        /// <summary>How aggressively the camera tries to maintain the offset in the path-local up direction.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// y-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain its position in the path-local up direction.  Small numbers are more responsive, rapidly translating the camera to keep the target's y-axis offset.  Larger numbers give a more heavy slowly responding camera. Using different settings per axis can yield a wide range of camera behaviors.")]
        public float m_YDamping = 0f;

        /// <summary>How aggressively the camera tries to maintain the offset parallel to the path.
        /// Small numbers are more responsive, rapidly translating the camera to keep the
        /// target's z-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain its position in a direction parallel to the path.  Small numbers are more responsive, rapidly translating the camera to keep the target's z-axis offset.  Larger numbers give a more heavy slowly responding camera. Using different settings per axis can yield a wide range of camera behaviors.")]
        public float m_ZDamping = 1f;

        /// <summary>Different ways to set the camera's up vector</summary>
        [DocumentationSorting(7.1f, DocumentationSortingAttribute.Level.UserRef)]
        public enum CameraUpMode
        {
            /// <summary>Leave the camera's up vector alone</summary>
            Default,
            /// <summary>Take the up vector from the path's up vector at the current point</summary>
            Path,
            /// <summary>Take the up vector from the path's up vector at the current point, but with the roll zeroed out</summary>
            PathNoRoll,
            /// <summary>Take the up vector from the Follow target's up vector</summary>
            FollowTarget,
            /// <summary>Take the up vector from the Follow target's up vector, but with the roll zeroed out</summary>
            FollowTargetNoRoll,
        };
        /// <summary>How to set the virtual camera's Up vector.  This will affect the screen composition.</summary>
        [Tooltip("How to set the virtual camera's Up vector.  This will affect the screen composition, because the camera Aim behaviours will always try to respect the Up direction.")]
        public CameraUpMode m_CameraUp = CameraUpMode.Default;

        /// <summary>Controls how automatic dollying occurs</summary>
        [DocumentationSorting(7.2f, DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct AutoDolly
        {
            /// <summary>If checked, will enable automatic dolly, which chooses a path position
            /// that is as close as possible to the Follow target.</summary>
            [Tooltip("If checked, will enable automatic dolly, which chooses a path position that is as close as possible to the Follow target.  Note: this can have significant performance impact")]
            public bool m_Enabled;

            /// <summary>How many segments on either side of the current segment.  Use 0 for Entire path</summary>
            [Tooltip("How many segments on either side of the current segment.  Use 0 for Entire path.")]
            public int m_SearchRadius;

            /// <summary>We search a segment by dividing it into this many straight pieces.
            /// The higher the number, the more accurate the result, but performance is
            /// proportionally slower for higher numbers</summary>
            [Tooltip("We search a segment by dividing it into this many straight pieces.  The higher the number, the more accurate the result, but performance is proportionally slower for higher numbers")]
            public int m_StepsPerSegment;

            /// <summary>Constructor with specific field values</summary>
            public AutoDolly(
                bool enabled, int searchRadius, int stepsPerSegment,
                float waitTime,  float maxSpeed,
                float accelTime, float decelTime)
            {
                m_Enabled = enabled;
                m_SearchRadius = searchRadius;
                m_StepsPerSegment = stepsPerSegment;
            }
        };

        /// <summary>Controls how automatic dollying occurs</summary>
        [Tooltip("Controls how automatic dollying occurs.  A Follow target is necessary to use this feature.")]
        public AutoDolly m_AutoDolly = new AutoDolly(false, 2, 5, 0, 2f, 1, 1);

        /// <summary>True if component is enabled and has a path</summary>
        public bool IsValid { get { return enabled && m_Path != null; } }

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
        public CameraState MutateCameraState(
            CameraState curState, CameraState statePrevFrame, float deltaTime)
        {
            if (!IsValid)
                return curState;

            if (deltaTime <= 0)
                m_PreviousPathPosition = m_PathPosition;

            CameraState newState = curState;

            // Get the new ideal path base position
            if (m_AutoDolly.m_Enabled)
                m_PathPosition = PerformAutoDolly(m_PreviousPathPosition, deltaTime);
            float newPathPosition = m_PathPosition;

            if (deltaTime > 0)
            {
                // Normalize previous position to find the shortest path
                if (m_Path.MaxPos > 0)
                {
                    float prev = m_Path.NormalizePos(m_PreviousPathPosition);
                    float next = m_Path.NormalizePos(newPathPosition);
                    if (m_Path.Looped && Mathf.Abs(next - prev) > m_Path.MaxPos / 2)
                    {
                        if (next > prev)
                            prev += m_Path.MaxPos;
                        else
                            prev -= m_Path.MaxPos;
                    }
                    m_PreviousPathPosition = prev;
                    newPathPosition = next;
                }

                // Apply damping along the path direction
                float offset = m_PreviousPathPosition - newPathPosition;
                offset *= deltaTime / Mathf.Max(m_ZDamping * kDampingScale, deltaTime);
                newPathPosition = m_PreviousPathPosition - offset;
            }
            m_PreviousPathPosition = newPathPosition;
            Quaternion newPathOrientation = m_Path.EvaluateOrientation(newPathPosition);

            // Apply the offset to get the new camera position
            Vector3 newCameraPos = m_Path.EvaluatePosition(newPathPosition);
            Vector3[] offsetDir = new Vector3[3];
            offsetDir[2] = newPathOrientation * Vector3.forward;
            offsetDir[1] = newPathOrientation * Vector3.up;
            offsetDir[0] = Vector3.Cross(offsetDir[1], offsetDir[2]);
            for (int i = 0; i < 3; ++i)
                newCameraPos += m_PathOffset[i] * offsetDir[i];

            // Apply damping to the remaining directions
            if (deltaTime > 0)
            {
                Vector3 currentCameraPos = statePrevFrame.RawPosition;
                Vector3 delta = (currentCameraPos - newCameraPos);
                Vector3 delta1 = Vector3.Dot(delta, offsetDir[1]) * offsetDir[1];
                Vector3 delta0 = delta - delta1;
                delta = delta0 * deltaTime / Mathf.Max(m_XDamping * kDampingScale, deltaTime)
                    + delta1 * deltaTime / Mathf.Max(m_YDamping * kDampingScale, deltaTime);
                newCameraPos = currentCameraPos - delta;
            }
            newState.RawPosition = newCameraPos;

            // Set the up
            switch (m_CameraUp)
            {
                default:
                case CameraUpMode.Default:
                    break;
                case CameraUpMode.Path:
                    newState.ReferenceUp = newPathOrientation * Vector3.up;
                    newState.RawOrientation = newPathOrientation;
                    break;
                case CameraUpMode.PathNoRoll:
                    newState.RawOrientation = Quaternion.LookRotation(
                            newPathOrientation * Vector3.forward, Vector3.up);
                    newState.ReferenceUp = newState.RawOrientation * Vector3.up;
                    break;
                case CameraUpMode.FollowTarget:
                    if (VirtualCamera.Follow != null)
                    {
                        newState.RawOrientation = VirtualCamera.Follow.rotation;
                        newState.ReferenceUp = newState.RawOrientation * Vector3.up;
                    }
                    break;
                case CameraUpMode.FollowTargetNoRoll:
                    if (VirtualCamera.Follow != null)
                    {
                        newState.RawOrientation = Quaternion.LookRotation(
                                VirtualCamera.Follow.rotation * Vector3.forward, Vector3.up);
                        newState.ReferenceUp = newState.RawOrientation * Vector3.up;
                    }
                    break;
            }
            return newState;
        }

        private const float kDampingScale = 0.1f;
        private float m_PreviousPathPosition = 0;

        float PerformAutoDolly(float currentPos, float deltaTime)
        {
            if (m_AutoDolly.m_Enabled && VirtualCamera.Follow != null)
            {
                float pos = m_Path.FindClosestPoint(
                        VirtualCamera.Follow.transform.position,
                        Mathf.FloorToInt(currentPos),
                        (deltaTime <= 0 || m_AutoDolly.m_SearchRadius <= 0) ? -1 : m_AutoDolly.m_SearchRadius,
                        m_AutoDolly.m_StepsPerSegment);
                return pos;
            }
            return m_PathPosition;
        }
    }
}
