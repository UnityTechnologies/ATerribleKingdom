using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineCollider))]
    public sealed class CinemachineColliderEditor : UnityEditor.Editor
    {
        private CinemachineCollider Target { get { return target as CinemachineCollider; } }
        private static readonly string[] m_excludeFields = new string[] { "m_Script" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, m_excludeFields);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineCollider))]
        private static void DrawColliderGizmos(CinemachineCollider collider, GizmoType type)
        {
            CinemachineVirtualCameraBase vcam = (collider != null) ? collider.VirtualCamera : null;
            if (vcam != null && collider.enabled)
            {
                Color oldColor = Gizmos.color;
                bool isLive = CinemachineCore.Instance.IsLive(vcam);
                Color feelerColor = isLive
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;
                Color hitColour = isLive ? Color.white : Color.grey;

                Vector3 pos = vcam.State.FinalPosition;
                if (collider.m_PreserveLineOfSight && vcam.State.HasLookAt)
                {
                    Vector3 forwardFeelerVector = (vcam.State.ReferenceLookAt - pos).normalized;
                    float distance = collider.m_LineOfSightFeelerDistance;
                    Gizmos.color = collider.IsTargetObscured(vcam.LiveChildOrSelf) ? hitColour : feelerColor;
                    Gizmos.DrawLine(pos, pos + forwardFeelerVector * distance);
                }

                if (collider.m_UseCurbFeelers)
                {
                    Quaternion orientation = vcam.State.FinalOrientation;
                    var feelers = collider.GetFeelers(vcam.LiveChildOrSelf);
                    foreach (CinemachineCollider.CompiledCurbFeeler feeler in feelers)
                    {
                        Vector3 worldDirection = orientation * feeler.LocalVector;
                        Gizmos.color = feeler.IsHit ? hitColour : feelerColor;
                        Gizmos.DrawLine(pos, pos + worldDirection * feeler.RayDistance);
                    }
                }
                Gizmos.color = oldColor;
            }
        }
    }
}
