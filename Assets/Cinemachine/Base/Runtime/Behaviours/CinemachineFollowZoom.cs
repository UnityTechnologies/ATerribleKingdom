using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that adjusts
    /// the FOV of the lens to keep the target object at a constant size on the screen,
    /// regardless of camera and target position.
    /// </summary>
    [DocumentationSorting(16, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode]
    [AddComponentMenu("Cinemachine/CinemachineFollowZoom")]
    [SaveDuringPlay]
    public class CinemachineFollowZoom : MonoBehaviour
    {
        /// <summary>The shot width to maintain, in world units, at target distance.
        /// FOV will be adusted as far as possible to maintain this width at the
        /// target distance from the camera.</summary>
        [Tooltip("The shot width to maintain, in world units, at target distance.")]
        public float m_Width = 2f;

        /// <summary>Increase this value to soften the aggressiveness of the follow-zoom.
        /// Small numbers are more responsive, larger numbers give a more heavy slowly responding camera. </summary>
        [Range(0f, 20f)]
        [Tooltip("Increase this value to soften the aggressiveness of the follow-zoom.  Small numbers are more responsive, larger numbers give a more heavy slowly responding camera.")]
        public float m_Damping = 1f;

        /// <summary>Will not generate an FOV smaller than this.</summary>
        [Range(1f, 179f)]
        [Tooltip("Lower limit for the FOV that this behaviour will generate.")]
        public float m_MinFOV = 3f;

        /// <summary>Will not generate an FOV larget than this.</summary>
        [Range(1f, 179f)]
        [Tooltip("Upper limit for the FOV that this behaviour will generate.")]
        public float m_MaxFOV = 60f;

        private void OnValidate()
        {
            m_Width = Mathf.Max(0, m_Width);
            m_MaxFOV = Mathf.Clamp(m_MaxFOV, 1, 179);
            m_MinFOV = Mathf.Clamp(m_MinFOV, 1, m_MaxFOV);
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
                Debug.LogError("CinemachineFollowZoom requires a Cinemachine Virtual Camera component");
                enabled = false;
            }
            else
            {
                VirtualCamera.AddPostPipelineStageHook(PostPipelineStageCallback);
                enabled = true;
            }
        }

        private void OnDestroy()
        {
            if (VirtualCamera != null)
                VirtualCamera.RemovePostPipelineStageHook(PostPipelineStageCallback);
        }

        /// <summary>Cache of the CinemachineVirtualCameraBase component</summary>
        public CinemachineVirtualCameraBase VirtualCamera { get; private set; }

        private const float kHumanReadableDampingScalar = 0.1f;
        private float m_previousFrameZoom = 0;

        private void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (!enabled || deltaTime == 0)
                m_previousFrameZoom = state.Lens.FieldOfView;
            if (enabled)
            {
                // Set the zoom after the body has been positioned, but before the aim,
                // so that composer can compose using the updated fov.
                if (stage == CinemachineCore.Stage.Body)
                {
                    // Try to reproduce the target width
                    float targetWidth = Mathf.Max(m_Width, 0);
                    float fov = 179f;
                    float d = Vector3.Distance(state.CorrectedPosition, state.ReferenceLookAt);
                    if (d > UnityVectorExtensions.Epsilon)
                    {
                        // Apply damping
                        if (deltaTime > 0 && m_Damping > 0)
                        {
                            // Clamp targetWidth to FOV min/max
                            float minW = d * 2f * Mathf.Tan(m_MinFOV * Mathf.Deg2Rad / 2f);
                            float maxW = d * 2f * Mathf.Tan(m_MaxFOV * Mathf.Deg2Rad / 2f);
                            targetWidth = Mathf.Clamp(targetWidth, minW, maxW);

                            float currentWidth = d * 2f * Mathf.Tan(m_previousFrameZoom * Mathf.Deg2Rad / 2f);
                            float delta = targetWidth - currentWidth;
                            delta *= deltaTime / Mathf.Max(m_Damping * kHumanReadableDampingScalar, deltaTime);
                            targetWidth = currentWidth + delta;
                        }
                        fov = 2f * Mathf.Atan(targetWidth / (2 * d)) * Mathf.Rad2Deg;
                    }
                    LensSettings lens = state.Lens;
                    lens.FieldOfView = m_previousFrameZoom = Mathf.Clamp(fov, m_MinFOV, m_MaxFOV);
                    state.Lens = lens;
                }
            }
        }
    }
}
