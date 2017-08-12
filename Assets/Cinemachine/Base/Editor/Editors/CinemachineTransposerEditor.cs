using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTransposer))]
    internal sealed class CinemachineTransposerEditor : UnityEditor.Editor
    {
        private CinemachineTransposer Target { get { return target as CinemachineTransposer; } }
        private static readonly string[] m_excludeFields = new string[] { "m_Script" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, m_excludeFields);
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineTransposer))]
        static void DrawTransposerGizmos(CinemachineTransposer target, GizmoType selectionType)
        {
            if (target.IsValid)
            {
                Color originalGizmoColour = Gizmos.color;
                Gizmos.color = CinemachineCore.Instance.IsLive(target.VirtualCamera)
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

                Vector3 targetPos = target.VirtualCamera.Follow.position;
                Vector3 desiredPos = target.GetDesiredTargetPosition();
                Gizmos.DrawLine(targetPos, desiredPos);
                Gizmos.DrawWireSphere(desiredPos,
                    HandleUtility.GetHandleSize(desiredPos) / 20);
            }
        }
    }
}
