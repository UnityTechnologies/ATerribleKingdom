using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTrackedDolly))]
    internal sealed class CinemachineTrackedDollyEditor : UnityEditor.Editor
    {
        private CinemachineTrackedDolly Target { get { return target as CinemachineTrackedDolly; } }
        private static readonly string[] m_excludeFields = new string[] { "m_Script" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, m_excludeFields);
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
