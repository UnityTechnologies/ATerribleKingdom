using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to aim the camera at a target object, with configurable offsets, damping, 
    /// and composition rules.
    /// 
    /// In addition, if the target is a CinemachineTargetGroup, the behaviour
    /// will adjust the FOV and the camera distance to ensure that the entire group of targets
    /// is framed properly.
    /// </summary>
    [DocumentationSorting(4, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode] // for OnGUI
    [AddComponentMenu("")] // Don't display in add component menu
    [RequireComponent(typeof(CinemachinePipeline))]
    [SaveDuringPlay]
    public class CinemachineGroupComposer : CinemachineComposer
    {
        /// <summary>How much of the screen to fill with the bounding box of the targets.</summary>
        [Space]
        [Tooltip("The bounding box of the targets should occupy this amount of the screen space.  1 means fill the whole screen.  0.5 means fill half the screen, etc.")]
        public float m_GroupFramingSize = 0.8f;

        /// <summary>What screen dimensions to consider when framing</summary>
        [DocumentationSorting(4.01f, DocumentationSortingAttribute.Level.UserRef)]
        public enum FramingMode
        {
            /// <summary>Consider only the horizontal dimension.  Vertical framing is ignored.</summary>
            Horizontal,
            /// <summary>Consider only the vertical dimension.  Horizontal framing is ignored.</summary>
            Vertical,
            /// <summary>The larger of the horizontal and vertical dimensions will dominate, to get the best fit.</summary>
            HorizontalAndVertical
        };

        /// <summary>What screen dimensions to consider when framing</summary>
        [Tooltip("What screen dimensions to consider when framing.  Can be Horizontal, Vertical, or both")]
        public FramingMode m_FramingMode = FramingMode.HorizontalAndVertical;

        /// <summary>How aggressively the camera tries to frame the group.
        /// Small numbers are more responsive</summary>
        [Range(0, 20)]
        [Tooltip("How aggressively the camera tries to frame the group. Small numbers are more responsive, rapidly adjusting the camera to keep the group in the frame.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_FrameDamping = 2f;

        /// <summary>How to adjust the camera to get the desired framing</summary>
        public enum AdjustmentMode
        {
            /// <summary>Do not move the camera, only adjust the FOV.</summary>
            ZoomOnly,
            /// <summary>Just move the camera, don't change the FOV.</summary>
            DollyOnly,
            /// <summary>Move the camera as much as permitted by the ranges, then
            /// adjust the FOV if necessary to make the shot.</summary>
            DollyThenZoom
        };

        /// <summary>How to adjust the camera to get the desired framing</summary>
        [Tooltip("How to adjust the camera to get the desired framing.  You can zoom, dolly in/out, or do both.")]
        public AdjustmentMode m_AdjustmentMode = AdjustmentMode.DollyThenZoom;

        /// <summary>How much closer to the target can the camera go?</summary>
        [Tooltip("The maximum distance toward the target that this behaviour is allowed to move the camera.")]
        public float m_MaxDollyIn = 5000f;

        /// <summary>How much farther from the target can the camera go?</summary>
        [Tooltip("The maximum distance away the target that this behaviour is allowed to move the camera.")]
        public float m_MaxDollyOut = 5000f;

        /// <summary>Set this to limit how close to the target the camera can get</summary>
        [Tooltip("Set this to limit how close to the target the camera can get.")]
        public float m_MinimumDistance = 0;

        /// <summary>Set this to limit how far from the taregt the camera can get</summary>
        [Tooltip("Set this to limit how far from the target the camera can get.")]
        public float m_MaximumDistance = 5000f;

        /// <summary>If adjusting FOV, will not set the FOV lower than this</summary>
        [Range(1, 179)]
        [Tooltip("If adjusting FOV, will not set the FOV lower than this.")]
        public float m_MinimumFOV = 3;

        /// <summary>If adjusting FOV, will not set the FOV higher than this</summary>
        [Range(1, 179)]
        [Tooltip("If adjusting FOV, will not set the FOV higher than this.")]
        public float m_MaximumFOV = 60;

        private void OnValidate()
        {
            m_GroupFramingSize = Mathf.Max(UnityVectorExtensions.Epsilon, m_GroupFramingSize);
            m_MaxDollyIn = Mathf.Max(0, m_MaxDollyIn);
            m_MaxDollyOut = Mathf.Max(0, m_MaxDollyOut);
            m_MinimumDistance = Mathf.Max(0, m_MinimumDistance);
            m_MaximumDistance = Mathf.Max(m_MinimumDistance, m_MaximumDistance);
            m_MinimumFOV = Mathf.Max(1, m_MinimumFOV);
            m_MaximumFOV = Mathf.Clamp(m_MaximumFOV, m_MinimumFOV, 179);
        }

        /// <summary>Applies the composer rules and orients the camera accordingly</summary>
        /// <param name="state">The current camera state</param>
        /// <param name="statePrevFrame">The camera state on the previous frame (unused)</param>
        /// <param name="deltaTime">Used for calculating damping.  If less than
        /// or equal to zero, then target will snap to the center of the dead zone.</param>
        /// <returns>curState with RawOrientation applied</returns>
        public override CameraState MutateCameraState(
            CameraState state, CameraState statePrevFrame, float deltaTime)
        {
            if (!IsValid || !state.HasLookAt)
                return state;

            // Can't do anything without a group to look at
            CinemachineTargetGroup group = VirtualCamera.LookAt.GetComponent<CinemachineTargetGroup>();
            if (group == null)
                return base.MutateCameraState(state, statePrevFrame, deltaTime);

            group.Update(); // Make sure the group's position is fully updated. 
            state.ReferenceLookAt = group.transform.position;
            Vector3 lookAtPosition = GetTrackedPoint(state.ReferenceLookAt);
            Vector3 currentOffset = lookAtPosition - state.CorrectedPosition;
            float currentDistance = currentOffset.magnitude;
            if (currentDistance < UnityVectorExtensions.Epsilon)
                return state;  // navel-gazing, get outa here

            // Get the camera axis
            Vector3 fwd = currentOffset.AlmostZero() ? Vector3.forward : currentOffset.normalized;

            // Get the bounding box from that POV in view space, and find its width
            Bounds bounds = group.BoundingBox;
            m_lastBoundsMatrix = Matrix4x4.TRS(
                    bounds.center - (fwd * bounds.extents.magnitude),
                    Quaternion.LookRotation(fwd, state.ReferenceUp), Vector3.one);
            m_LastBounds = group.GetViewSpaceBoundingBox(m_lastBoundsMatrix);
            float targetHeight = GetTargetHeight(m_LastBounds);
            Vector3 targetPos = m_lastBoundsMatrix.MultiplyPoint3x4(m_LastBounds.center);

            // Apply damping
            if (deltaTime > 0 && m_FrameDamping > 0)
            {
                float delta = targetHeight - m_prevTargetHeight;
                delta *= deltaTime / Mathf.Max(m_FrameDamping * kHumanReadableDampingScale, deltaTime);
                targetHeight = m_prevTargetHeight + delta;
            }
            m_prevTargetHeight = targetHeight;

            // Move the camera
            if (!state.Lens.Orthographic && m_AdjustmentMode != AdjustmentMode.ZoomOnly)
            {
                // What distance would be needed to get the target height, at the current FOV
                float currentFOV = state.Lens.FieldOfView;
                float targetDistance = targetHeight / (2f * Mathf.Tan(currentFOV * Mathf.Deg2Rad / 2f));

                // target the near surface of the bounding box
                float cameraDistance = targetDistance + m_LastBounds.extents.z;

                // Clamp to respect min/max distance settings
                cameraDistance = Mathf.Clamp(
                        cameraDistance, currentDistance - m_MaxDollyIn, currentDistance + m_MaxDollyOut);
                cameraDistance = Mathf.Clamp(cameraDistance, m_MinimumDistance, m_MaximumDistance);

                // Apply
                state.PositionCorrection += targetPos - fwd * cameraDistance - state.CorrectedPosition;
            }

            // Apply zoom
            if (state.Lens.Orthographic || m_AdjustmentMode != AdjustmentMode.DollyOnly)
            {
                float nearBoundsDistance = (lookAtPosition - state.CorrectedPosition).magnitude
                    - m_LastBounds.extents.z;
                float currentFOV = 179;
                if (nearBoundsDistance > UnityVectorExtensions.Epsilon)
                    currentFOV = 2f * Mathf.Atan(targetHeight / (2 * nearBoundsDistance)) * Mathf.Rad2Deg;

                LensSettings lens = state.Lens;
                lens.FieldOfView = Mathf.Clamp(currentFOV, m_MinimumFOV, m_MaximumFOV);
                lens.OrthographicSize = targetHeight / 2;
                state.Lens = lens;
            }

            // Now compose normally
            return base.MutateCameraState(state, statePrevFrame, deltaTime);
        }

        float m_prevTargetHeight; // State for damping

        /// <summary>For editor visulaization of the calculated bounding box of the group</summary>
        public Bounds m_LastBounds { get; private set; }

        /// <summary>For editor visulaization of the calculated bounding box of the group</summary>
        public Matrix4x4 m_lastBoundsMatrix { get; private set; }

        float GetTargetHeight(Bounds b)
        {
            float framingSize = Mathf.Max(UnityVectorExtensions.Epsilon, m_GroupFramingSize);
            switch (m_FramingMode)
            {
                case FramingMode.Horizontal:
                    return b.size.x / (framingSize * VirtualCamera.State.Lens.Aspect);
                case FramingMode.Vertical:
                    return b.size.y / framingSize;
                default:
                case FramingMode.HorizontalAndVertical:
                    return Mathf.Max(
                        b.size.x / (framingSize * VirtualCamera.State.Lens.Aspect), 
                        b.size.y / framingSize);
            }
        }
    }
}
