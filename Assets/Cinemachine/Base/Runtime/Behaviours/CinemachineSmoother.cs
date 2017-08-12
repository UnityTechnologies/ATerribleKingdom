using UnityEngine;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera which post-processes
    /// the final position and  orientation of the virtual camera, as a kind of low-pass filter.
    /// </summary>
    [DocumentationSorting(17, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode]
    [AddComponentMenu("Cinemachine/CinemachineSmoother")]
    [SaveDuringPlay]
    public class CinemachineSmoother : MonoBehaviour
    {
        /// <summary>
        /// The strength of the smoothing for position.  This is applied after the vcam cas calculated its state.
        /// </summary>
        [Range(0f, 10f)]
        [Tooltip("The strength of the smoothing for position.  Higher numbers smooth more but reduce performance and introduce lag.")]
        public float m_PositionSmoothing = 1;

        /// <summary>
        /// The strength of the smoothing for the LookAt target.  This is applied after the vcam cas calculated its state.
        /// </summary>
        [Range(0f, 10f)]
        [Tooltip("The strength of the smoothing for the LookAt target.  Higher numbers smooth more but reduce performance and introduce lag.")]
        public float m_LookAtSmoothing = 1;

        /// <summary>
        /// The strength of the smoothing for rotation.  This is applied after the vcam cas calculated its state.
        /// </summary>
        [Range(0f, 10f)]
        [Tooltip("The strength of the smoothing for rotation.  Higher numbers smooth more but reduce performance and introduce lag.")]
        public float m_RotationSmoothing = 1;

        private void Start()
        {
            OnEnable();
        }

        private void OnEnable()
        {
            VirtualCamera = GetComponent<CinemachineVirtualCameraBase>();
            if (VirtualCamera == null)
            {
                Debug.LogError("CinemachineSmoother requires a Cinemachine Virtual Camera component");
                enabled = false;
            }
            else
            {
                VirtualCamera.AddPostPipelineStageHook(PostPipelineStageCallback);
                enabled = true;
            }
            mSmoothingFilter = null;
            mSmoothingFilterRotation = null;
        }

        private void OnDestroy()
        {
            if (VirtualCamera != null)
                VirtualCamera.RemovePostPipelineStageHook(PostPipelineStageCallback);
        }

        /// <summary>Get the associated CinemachineVirtualCameraBase.</summary>
        public CinemachineVirtualCameraBase VirtualCamera { get; private set; }

        private void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, CameraState previousState, float deltaTime)
        {
            if (enabled)
            {
                if (stage == CinemachineCore.Stage.Body)
                {
                    if (m_PositionSmoothing > 0)
                    {
                        if (deltaTime <= 0)
                            mSmoothingFilter = null; // reset the filter
                        state.PositionCorrection
                            += ApplySmoothing(vcam, state.CorrectedPosition) - state.CorrectedPosition;
                    }
                    if (m_LookAtSmoothing > 0 && state.HasLookAt)
                    {
                        if (deltaTime <= 0)
                            mSmoothingFilterLookAt = null; // reset the filter
                        state.ReferenceLookAt = ApplySmoothingLookAt(vcam, state.ReferenceLookAt);
                    }
                }
                if (stage == CinemachineCore.Stage.Aim)
                {
                    if (m_RotationSmoothing > 0)
                    {
                        if (deltaTime <= 0)
                            mSmoothingFilterRotation = null; // reset the filter
                        Quaternion q = Quaternion.Inverse(state.CorrectedOrientation)
                            * ApplySmoothing(vcam, state.CorrectedOrientation, state.ReferenceUp);
                        state.OrientationCorrection = state.OrientationCorrection * q;
                    }
                }
            }
        }

        private Dictionary<CinemachineVirtualCameraBase, GaussianWindow1D_Vector3> mSmoothingFilter;
        private Vector3 ApplySmoothing(CinemachineVirtualCameraBase vcam, Vector3 pos)
        {
            if (mSmoothingFilter == null)
                mSmoothingFilter = new Dictionary<CinemachineVirtualCameraBase, GaussianWindow1D_Vector3>();
            GaussianWindow1D_Vector3 filter = null;
            if (!mSmoothingFilter.TryGetValue(vcam, out filter) || filter.Sigma != m_PositionSmoothing)
                mSmoothingFilter[vcam] = filter = new GaussianWindow1D_Vector3(m_PositionSmoothing);
            return filter.Filter(pos);
        }

        private Dictionary<CinemachineVirtualCameraBase, GaussianWindow1D_Vector3> mSmoothingFilterLookAt;
        private Vector3 ApplySmoothingLookAt(CinemachineVirtualCameraBase vcam, Vector3 pos)
        {
            if (mSmoothingFilterLookAt == null)
                mSmoothingFilterLookAt = new Dictionary<CinemachineVirtualCameraBase, GaussianWindow1D_Vector3>();
            GaussianWindow1D_Vector3 filter = null;
            if (!mSmoothingFilterLookAt.TryGetValue(vcam, out filter) || filter.Sigma != m_LookAtSmoothing)
                mSmoothingFilterLookAt[vcam] = filter = new GaussianWindow1D_Vector3(m_LookAtSmoothing);
            return filter.Filter(pos);
        }

        private Dictionary<CinemachineVirtualCameraBase, GaussianWindow1D_CameraRotation> mSmoothingFilterRotation;
        private Quaternion ApplySmoothing(CinemachineVirtualCameraBase vcam, Quaternion rot, Vector3 up)
        {
            if (mSmoothingFilterRotation == null)
                mSmoothingFilterRotation = new Dictionary<CinemachineVirtualCameraBase, GaussianWindow1D_CameraRotation>();
            GaussianWindow1D_CameraRotation filter = null;
            if (!mSmoothingFilterRotation.TryGetValue(vcam, out filter) || filter.Sigma != m_RotationSmoothing)
                mSmoothingFilterRotation[vcam] = filter = new GaussianWindow1D_CameraRotation(m_RotationSmoothing);

            Vector3 camRot = Quaternion.identity.GetCameraRotationToTarget(rot * Vector3.forward, up);
            return Quaternion.identity.ApplyCameraRotation(filter.Filter(camRot), up);
        }
    }
}
