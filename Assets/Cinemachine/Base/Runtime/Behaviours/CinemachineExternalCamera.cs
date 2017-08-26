using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// This component will expose a non-cinemachine camera to the cinemachine system,
    /// allowing it to participate in blends.
    /// Just add it as a component alongside an existing Unity Camera component.
    /// </summary>
    [DocumentationSorting(14, DocumentationSortingAttribute.Level.UserRef)]
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent, ExecuteInEditMode]
    [AddComponentMenu("Cinemachine/CinemachineExternalCamera")]
    public class CinemachineExternalCamera : CinemachineVirtualCameraBase
    {
        private Camera m_Camera;
        private CameraState m_State;

        /// <summary>Caches the camera component</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            m_Camera = GetComponent<Camera>();
        }

        /// <summary>Get the CameraState, as we are able to construct one from the Unity Camera</summary>
        public override CameraState State { get { return m_State; } }

        /// <summary>This vcam defines no targets</summary>
        override public Transform LookAt { get; set; }

        /// <summary>This vcam defines no targets</summary>
        override public Transform Follow { get; set; }

        /// <summary>Construct a CameraState object from the Unity Camera</summary>
        public override void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            // Get the state from the camera
            m_State = CameraState.Default;
            m_State.ReferenceUp = worldUp;
            m_State.RawPosition = transform.position;
            m_State.RawOrientation = transform.rotation;
            if (m_Camera != null)
                m_State.Lens = new LensSettings(m_Camera);
        }
    }
}
