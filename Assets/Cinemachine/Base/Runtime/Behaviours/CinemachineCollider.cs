using UnityEngine;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that post-processes
    /// the final position of the virtual camera. Based on the supplied settings,
    /// the CinemachineCollider will attempt to preserve the line of sight
    /// with the compositional target of the Cinemachine Virtual Camera and/or
    /// keep a certain distance away from objects around the Cinemachine Virtual Camera.
    /// 
    /// Additionally, the Collider can be used to assess the shot quality and 
    /// report this as a field in the camera State.
    /// </summary>
    [DocumentationSorting(15, DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode]
    [AddComponentMenu("Cinemachine/CinemachineCollider")]
    [SaveDuringPlay]
    public class CinemachineCollider : MonoBehaviour
    {
        /// <summary>
        /// The Unity layer mask which the collider will raycast against.
        /// </summary>
        [Tooltip("The Unity layer mask by which the collider will raycast against")]
        public LayerMask m_CollideAgainst = 1;

        /// <summary>
        /// When <b>TRUE</b>, will move the camera in front of anything which intersects the ray
        /// based on the supplied layer mask and within the line of sight feeler distance
        /// </summary>
        [Tooltip("When enabled, will move the camera in front of anything which intersects the Line of Sight feeler")]
        public bool m_PreserveLineOfSight = true;

        /// <summary>
        /// The raycast distance to test for when checking if the line of sight to this camera's target is clear.
        /// </summary>
        [Tooltip("The raycast distance to test for when checking if the line of sight to this camera's target is clear.  If the setting is 0 or less, the current actual distance to target will be used.")]
        [Min(0)]
        public float m_LineOfSightFeelerDistance = 0f;

        /// <summary>
        /// Never get closer to the target than this.
        /// </summary>
        [Tooltip("Never get closer to the target than this.")]
        [Min(0)]
        public float m_MinimumDistanceFromTarget = 2f;

        /// <summary>
        /// Ignore obstacles that are closer to the camera than this.
        /// </summary>
        [Tooltip("Ignore obstacles that are closer to the camera than this.")]
        [Min(0)]
        public float m_MinimumDistanceFromCamera = 0;

        /// <summary>
        /// When <b>TRUE</b>, will push the camera away from any feeler which raycasts against an
        /// object within the feeler ray distance
        /// </summary>
        [Tooltip("When enabled, will push the camera away from any object touching a curb feeler")]
        public bool m_UseCurbFeelers = true;

        /// <summary>
        /// The raycast distance used to check if the camera is colliding against objects in the world.
        /// </summary>
        [Tooltip("The raycast distance used to check if the camera is colliding against objects in the world.")]
        [Min(0)]
        public float m_CurbFeelerDistance = 2f;

        /// <summary>
        /// The firmness by which the camera collider will push back against any object it is colliding with
        /// </summary>
        [Range(1f, MaxCurbResistance)]
        [Tooltip("The firmness with which the collider will push back against any object")]
        public float m_CurbResistance = 1f;

        /// <summary>
        /// For reducing jitter, we apply a simple position filter to the effect of the collider.
        /// This duplicates the functionality of CinemachineSmoother
        /// </summary>
        [Range(0f, 10f)]
        [Tooltip("The strength of the jitter reduction for position.  Higher numbers smooth more but reduce performance and introduce lag.")]
        public float m_PositionSmoothing = 0;

        /// <summary>If greater than zero, a hight score will be given to shots when the target is closer to
        /// this distance.  Set this to zero to disable this feature</summary>
        [Header("Shot Evaluation")]
        [Tooltip("If greater than zero, a higher score will be given to shots when the target is closer to this distance.  Set this to zero to disable this feature.")]
        public float m_OptimalTargetDistance = 0;

        /// <summary>Get the associated CinemachineVirtualCameraBase.</summary>
        public CinemachineVirtualCameraBase VirtualCamera { get; private set; }

        /// <summary>API for the Editor to draw gizmos.</summary>
        [DocumentationSorting(15.1f, DocumentationSortingAttribute.Level.Undoc)]
        public struct CompiledCurbFeeler
        {
            public readonly Vector3 LocalVector;
            public readonly float RayDistance;
            public readonly float DampingConstant;
            public bool IsHit;
            public float HitDistance;

            public CompiledCurbFeeler(Vector3 localDirection, float rayDistance, float dampingConstant)
            {
                LocalVector = localDirection;
                RayDistance = rayDistance;
                DampingConstant = dampingConstant;
                IsHit = false;
                HitDistance = float.MaxValue;
            }
        }

        /// <summary>API for the Editor to draw gizmos.</summary>
        public IEnumerable<CompiledCurbFeeler> GetFeelers(ICinemachineCamera vcam)
        {
            return GetExtraState(vcam).curbFeelers;
        }

        /// <summary>See wheter an object is blocking the camera's view of the target</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the collider, in the event that the camera has children</param>
        /// <returns>True if something is blocking the view</returns>
        public bool IsTargetObscured(ICinemachineCamera vcam)
        {
            return GetExtraState(vcam).targetObscured;
        }

        /// <summary>See whether the virtual camera has been moved nby the collider</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the collider, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been displaced due to collision or
        /// target obstruction</returns>
        public bool CameraWasDisplaced(CinemachineVirtualCameraBase vcam)
        {
            return GetExtraState(vcam).colliderDisplacementDecay > 0;
        }

        private static readonly Vector3 kLocalUpRight = (Vector3.right + Vector3.up + Vector3.back).normalized;
        private static readonly Vector3 kLocalUpLeft = (Vector3.left + Vector3.up + Vector3.back).normalized;
        private static readonly Vector3 kLocalDownRight = (Vector3.right + Vector3.down + Vector3.back).normalized;
        private static readonly Vector3 kLocalDownLeft = (Vector3.left + Vector3.down + Vector3.back).normalized;

        private float MinCurbDistance { get { return m_CurbFeelerDistance / 20f; } }
        private const float MaxCurbResistance = 10f;

        private void Awake()
        {
            if (m_UseCurbFeelers && (m_CurbResistance < 1f))
                m_CurbResistance = 1f;
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
                Debug.LogError("CinemachineCollider requires a Cinemachine Virtual Camera component");
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
            CinemachineCore.Stage stage, ref CameraState state, CameraState previousState, float deltaTime)
        {
            VcamExtraState extra = null;
            if (stage == CinemachineCore.Stage.Body)
            {
                extra = GetExtraState(vcam);
                extra.targetObscured = false;
                extra.colliderDisplacement = 0;
                if (extra.colliderDisplacementDecay > 0)
                    --extra.colliderDisplacementDecay; // decay the displacement to accommodate the filter
            }
            if (enabled)
            {
                // Move the body before the Aim is calculated
                if (stage == CinemachineCore.Stage.Body)
                {
                    if (m_PreserveLineOfSight)
                        PreserveLignOfSight(ref state, extra);
                    if (m_UseCurbFeelers && m_CurbFeelerDistance > UnityVectorExtensions.Epsilon)
                        ApplyCurbFeelers(ref state, extra);
                    if (extra.colliderDisplacement > 0.1f)
                        extra.colliderDisplacementDecay = extra.filter.KernelSize + 1;

                    // Apply the smoothing filter
                    Vector3 pos = state.CorrectedPosition;
                    if (m_PositionSmoothing > 0)
                        state.PositionCorrection += extra.filter.Filter(pos) - pos;
                }
                // Rate the shot after the aim was set
                if (stage == CinemachineCore.Stage.Aim)
                {
                    extra = GetExtraState(vcam);
                    extra.targetObscured = CheckForTargetObstructions(state);

                    // GML these values are an initial arbitrary attempt at rating quality
                    if (extra.targetObscured)
                        state.ShotQuality *= 0.2f;
                    if (extra.colliderDisplacementDecay > 0)
                        state.ShotQuality *= 0.8f;

                    float nearnessBoost = 0;
                    const float kMaxNearBoost = 0.2f;
                    if (m_OptimalTargetDistance > 0 && state.HasLookAt)
                    {
                        float distance = Vector3.Magnitude(state.ReferenceLookAt - state.FinalPosition);
                        if (distance <= m_OptimalTargetDistance)
                        {
                            float threshold = m_OptimalTargetDistance / 2;
                            if (distance >= threshold)
                                nearnessBoost = kMaxNearBoost * (distance - threshold)
                                    / (m_OptimalTargetDistance - threshold);
                        }
                        else
                        {
                            distance -= m_OptimalTargetDistance;
                            float threshold = m_OptimalTargetDistance * 3;
                            if (distance < threshold)
                                nearnessBoost = kMaxNearBoost * (1f - (distance / threshold));
                        }
                        state.ShotQuality *= (1f + nearnessBoost);
                    }
                }
            }
        }

        class VcamExtraState
        {
            public GaussianWindow1D_Vector3 filter;
            public int colliderDisplacementDecay;
            public float colliderDisplacement;
            public bool targetObscured;
            public float curbResistance;
            public float feelerDistance;
            public CompiledCurbFeeler[] curbFeelers;

            public void RebuildCurbFeelers(float feelerDamping, float m_CurbFeelerDistance)
            {
                List<CompiledCurbFeeler> feelers = new List<CompiledCurbFeeler>(9);
                Vector3 localRight = Vector3.right;
                Vector3 localLeft = Vector3.left;
                Vector3 localBack = Vector3.back;
                Vector3 localUp = Vector3.up;
                Vector3 localDown = Vector3.down;

                feelers.Add(new CompiledCurbFeeler(localBack, m_CurbFeelerDistance, feelerDamping));
                feelers.Add(new CompiledCurbFeeler(localRight, m_CurbFeelerDistance, feelerDamping));
                feelers.Add(new CompiledCurbFeeler(localLeft, m_CurbFeelerDistance, feelerDamping));

                feelers.Add(new CompiledCurbFeeler(localUp, m_CurbFeelerDistance, feelerDamping));
                feelers.Add(new CompiledCurbFeeler(localDown, m_CurbFeelerDistance, feelerDamping));

                feelers.Add(new CompiledCurbFeeler(kLocalUpRight, m_CurbFeelerDistance, feelerDamping));
                feelers.Add(new CompiledCurbFeeler(kLocalUpLeft, m_CurbFeelerDistance, feelerDamping));
                feelers.Add(new CompiledCurbFeeler(kLocalDownRight, m_CurbFeelerDistance, feelerDamping));
                feelers.Add(new CompiledCurbFeeler(kLocalDownLeft, m_CurbFeelerDistance, feelerDamping));

                curbFeelers = feelers.ToArray();
                curbResistance = feelerDamping;
                feelerDistance = m_CurbFeelerDistance;
            }
        };

        private Dictionary<ICinemachineCamera, VcamExtraState> mExtraState;
        VcamExtraState GetExtraState(ICinemachineCamera vcam)
        {
            if (mExtraState == null)
                mExtraState = new Dictionary<ICinemachineCamera, VcamExtraState>();
            VcamExtraState extra = null;
            if (!mExtraState.TryGetValue(vcam, out extra))
                extra = mExtraState[vcam] = new VcamExtraState();
            if (extra.filter == null || extra.filter.Sigma != m_PositionSmoothing)
                extra.filter = new GaussianWindow1D_Vector3(m_PositionSmoothing);
            if (!m_UseCurbFeelers)
                extra.curbFeelers = null;
            else if (extra.curbFeelers == null || extra.curbFeelers.Length != 9
                     || extra.curbResistance != m_CurbResistance
                     || extra.feelerDistance != m_CurbFeelerDistance)
            {
                extra.RebuildCurbFeelers(m_CurbResistance, m_CurbFeelerDistance);
            }
            return extra;
        }

        private bool PreserveLignOfSight(ref CameraState state, VcamExtraState extra)
        {
            bool displaced = false;
            if (state.HasLookAt)
            {
                Vector3 lookAtPos = state.ReferenceLookAt;
                Vector3 pos = state.CorrectedPosition;
                Vector3 dir = lookAtPos - pos;
                float targetDistance = dir.magnitude;
                float minDistanceFromTarget = Mathf.Max(m_MinimumDistanceFromTarget, UnityVectorExtensions.Epsilon);
                if (targetDistance > minDistanceFromTarget)
                {
                    dir.Normalize();
                    float rayFar = targetDistance - minDistanceFromTarget;
                    if (m_LineOfSightFeelerDistance > UnityVectorExtensions.Epsilon)
                        rayFar = Mathf.Min(m_LineOfSightFeelerDistance, rayFar);

                    // Make a ray that looks towards the camera, to get the most distant obstruction
                    Ray ray = new Ray(pos + rayFar * dir, -dir);
                    int raycastLayerMask = m_CollideAgainst.value;
                    float rayLength = rayFar - Mathf.Max(0, m_MinimumDistanceFromCamera);
                    if (rayLength > Mathf.Epsilon)
                    {
                        RaycastHit hitInfo;
                        if (Physics.Raycast(ray, out hitInfo, rayLength, raycastLayerMask))
                        {
                            float adjustment = hitInfo.distance;
                            if (m_UseCurbFeelers)
                                adjustment -= MinCurbDistance;
                            pos = ray.GetPoint(adjustment);
                            Vector3 displacement = pos - state.CorrectedPosition;
                            state.PositionCorrection += displacement;
                            extra.colliderDisplacement += displacement.magnitude;
                            displaced = true;
                        }
                    }
                }
            }
            return displaced;
        }

        private bool ApplyCurbFeelers(ref CameraState state, VcamExtraState extra)
        {
            bool displaced = false;
            Vector3 pos = state.CorrectedPosition;
            Quaternion orientation = state.CorrectedOrientation;
            RaycastHit hitInfo;
            int raycastLayerMask = m_CollideAgainst.value;

            Ray feelerRay = new Ray();
            int numHits = 0;
            Vector3 resultingPosition = Vector3.zero;
            for (int i = 0; i < extra.curbFeelers.Length; ++i)
            {
                CompiledCurbFeeler feeler = extra.curbFeelers[i];
                feelerRay.origin = pos;
                feelerRay.direction = orientation * feeler.LocalVector;
                if (Physics.Raycast(feelerRay, out hitInfo, feeler.RayDistance, raycastLayerMask))
                {
                    float compressionPercent = Mathf.Clamp01((feeler.RayDistance - hitInfo.distance) / feeler.RayDistance);
                    compressionPercent = 1f - Mathf.Pow(compressionPercent, feeler.DampingConstant);
                    resultingPosition += hitInfo.point - feelerRay.direction * (compressionPercent * feeler.RayDistance);
                    feeler.IsHit = true;
                    feeler.HitDistance = hitInfo.distance;
                    numHits++;
                }
                else
                {
                    feeler.IsHit = false;
                    feeler.HitDistance = float.MaxValue;
                }
                extra.curbFeelers[i] = feeler;
            }

            // Average the resulting positions if feelers hit anything
            if (numHits > 0)
            {
                Vector3 displacement = (resultingPosition / (float)numHits) - state.CorrectedPosition;
                extra.colliderDisplacement += displacement.magnitude;
                state.PositionCorrection += displacement;
                displaced = true;
            }
            return displaced;
        }

        private bool CheckForTargetObstructions(CameraState state)
        {
            if (state.HasLookAt)
            {
                Vector3 lookAtPos = state.ReferenceLookAt;
                Vector3 pos = state.CorrectedPosition;
                Vector3 dir = lookAtPos - pos;
                float distance = dir.magnitude;
                if (distance < Mathf.Max(m_MinimumDistanceFromTarget, UnityVectorExtensions.Epsilon))
                    return true;
                Ray ray = new Ray(pos, dir.normalized);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo,
                        distance - m_MinimumDistanceFromTarget, m_CollideAgainst.value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
