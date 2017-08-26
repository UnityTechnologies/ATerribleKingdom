using UnityEngine;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that post-processes
    /// the final position of the virtual camera. It will confine the virtual
    /// camera's position to the volume specified in the Bounding Volume field.
    /// </summary>
    [DocumentationSorting(22, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode]
    [AddComponentMenu("Cinemachine/CinemachineConfiner")]
    [SaveDuringPlay]
    public class CinemachineConfiner : MonoBehaviour
    {
        /// <summary>The volume within which the camera is to be contained.</summary>
        [Tooltip("The volume within which the camera is to be contained")]
        public Collider m_BoundingVolume;

        /// <summary>How gradually to return the camera to the bounding volume if it goes beyond the borders</summary>
        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  Higher numbers are more gradual.")]
        [Range(0, 10)]
        public float m_Damping = 0;

        /// <summary>Get the associated CinemachineVirtualCameraBase.</summary>
        public CinemachineVirtualCameraBase VirtualCamera { get; private set; }

        /// <summary>See whether the virtual camera has been moved by the confiner</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the confiner, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been repositioned</returns>
        public bool CameraWasDisplaced(CinemachineVirtualCameraBase vcam)
        {
            return GetExtraState(vcam).confinerDisplacement > 0;
        }
        
        private void OnValidate()
        {
            m_Damping = Mathf.Max(0, m_Damping);
        }

        private void Start()
        {
            OnEnable();
        }

        private void OnEnable()
        {
            VirtualCamera = GetComponent<CinemachineVirtualCameraBase>();
            if (VirtualCamera == null)
            {
                Debug.LogError("CinemachineConfiner requires a Cinemachine Virtual Camera component");
                enabled = false;
            }
            else
            {
                VirtualCamera.AddPostPipelineStageHook(PostPipelineStageCallback);
                enabled = true;
            }
            mExtraState = null;
        }

        private void OnDestroy()
        {
            if (VirtualCamera != null)
                VirtualCamera.RemovePostPipelineStageHook(PostPipelineStageCallback);
        }

        private void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (enabled && m_BoundingVolume != null)
            {
                // Move the body before the Aim is calculated
                if (stage == CinemachineCore.Stage.Body)
                {
                    Vector3 camPos = state.CorrectedPosition;
                    Vector3 closest = m_BoundingVolume.ClosestPoint(camPos);
                    Vector3 dir = closest - camPos;
                    float distance = dir.magnitude;
                    if (distance > Epsilon)
                        dir /= distance;
                    Vector3 displacement = distance * dir;

                    VcamExtraState extra = GetExtraState(vcam);
                    if (m_Damping > 0 && deltaTime > 0)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        if (Mathf.Abs(delta.magnitude) > Epsilon)
                            delta *= deltaTime / Mathf.Max(m_Damping * kDampingScale, deltaTime);
                        displacement = extra.m_previousDisplacement + delta;
                    }
                    extra.m_previousDisplacement = displacement;
                    state.PositionCorrection += displacement;
                    extra.confinerDisplacement = displacement.magnitude;
                }
            }
        }

        const float kDampingScale = 0.1f;
        const float Epsilon = UnityVectorExtensions.Epsilon;
        const float ExtraCamMargin = 0.01f;

        class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
        };

        private Dictionary<ICinemachineCamera, VcamExtraState> mExtraState;
        VcamExtraState GetExtraState(ICinemachineCamera vcam)
        {
            if (mExtraState == null)
                mExtraState = new Dictionary<ICinemachineCamera, VcamExtraState>();
            VcamExtraState extra = null;
            if (!mExtraState.TryGetValue(vcam, out extra))
                extra = mExtraState[vcam] = new VcamExtraState();
            return extra;
        }
    }
}
