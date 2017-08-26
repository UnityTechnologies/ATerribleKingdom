using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTransposer))]
    internal sealed class CinemachineTransposerEditor : UnityEditor.Editor
    {
        private CinemachineTransposer Target { get { return target as CinemachineTransposer; } }

        public override void OnInspectorGUI()
        {
            string[] excludeFields;
            switch (Target.m_BindingMode)
            {
                default:
                case CinemachineTransposer.BindingMode.LockToTarget:
                    excludeFields = new string[] { "m_Script" };
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetNoRoll:
                    excludeFields = new string[]
                    {
                        "m_Script",
                        SerializedPropertyHelper.PropertyName(() => Target.m_RollDamping)
                    };
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetWithWorldUp:
                    excludeFields = new string[]
                    {
                        "m_Script",
                        SerializedPropertyHelper.PropertyName(() => Target.m_PitchDamping),
                        SerializedPropertyHelper.PropertyName(() => Target.m_RollDamping)
                    };
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetOnAssign:
                case CinemachineTransposer.BindingMode.WorldSpace:
                    excludeFields = new string[]
                    {
                        "m_Script",
                        SerializedPropertyHelper.PropertyName(() => Target.m_PitchDamping),
                        SerializedPropertyHelper.PropertyName(() => Target.m_YawDamping),
                        SerializedPropertyHelper.PropertyName(() => Target.m_RollDamping)
                    };
                    break;
            }
            serializedObject.Update();
            if (Target.VirtualCamera.Follow == null)
                EditorGUILayout.HelpBox(
                    "A Follow Target is required.  Change Body to Hard Constraint if you don't want a Follow target.",
                    MessageType.Error);
            DrawPropertiesExcluding(serializedObject, excludeFields);
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

                Vector3 up = Vector3.up;
                CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(target.VirtualCamera);
                if (brain != null)
                    up = brain.DefaultWorldUp;
                Vector3 targetPos = target.VirtualCamera.Follow.position;
                Vector3 desiredPos = target.GeTargetCameraPosition(up);
                Gizmos.DrawLine(targetPos, desiredPos);
                Gizmos.DrawWireSphere(desiredPos,
                    HandleUtility.GetHandleSize(desiredPos) / 20);
            }
        }
    }
}
