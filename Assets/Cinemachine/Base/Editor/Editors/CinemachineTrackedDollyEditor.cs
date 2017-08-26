using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTrackedDolly))]
    internal sealed class CinemachineTrackedDollyEditor : UnityEditor.Editor
    {
        private CinemachineTrackedDolly Target { get { return target as CinemachineTrackedDolly; } }

        public override void OnInspectorGUI()
        {
            string[] excludeFields;
            switch (Target.m_CameraUp)
            {
                default:
                    excludeFields = new string[] { "m_Script" };
                    break;
                case CinemachineTrackedDolly.CameraUpMode.PathNoRoll:
                case CinemachineTrackedDolly.CameraUpMode.FollowTargetNoRoll:
                    excludeFields = new string[]
                    {
                        "m_Script",
                        SerializedPropertyHelper.PropertyName(() => Target.m_RollDamping)
                    };
                    break;
                case CinemachineTrackedDolly.CameraUpMode.World:
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
            if (Target.m_Path == null)
                EditorGUILayout.HelpBox("A Path is required", MessageType.Error);
            if (Target.m_AutoDolly.m_Enabled && Target.VirtualCamera.Follow == null)
                EditorGUILayout.HelpBox("AutoDolly requires a Follow Target", MessageType.Info);
            DrawPropertiesExcluding(serializedObject, excludeFields);
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineTrackedDolly))]
        private static void DrawTrackeDollyGizmos(CinemachineTrackedDolly target, GizmoType selectionType)
        {
            if (target.IsValid)
            {
                CinemachinePath path = target.m_Path as CinemachinePath;
                if (path != null)
                    CinemachinePathEditor.DrawPathGizmos(path, selectionType);
            }
        }
    }
}
