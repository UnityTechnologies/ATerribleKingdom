using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOrbitalTransposer))]
    internal class CinemachineOrbitalTransposerEditor : UnityEditor.Editor
    {
        private CinemachineOrbitalTransposer Target { get { return target as CinemachineOrbitalTransposer; } }
        private static string[] m_excludeFields = new string[] { "m_Script" };
        private static string[] m_excludeFieldsSlaveMode;

        public override void OnInspectorGUI()
        {
            if (m_excludeFieldsSlaveMode == null)
                m_excludeFieldsSlaveMode = new string[]
                {
                    "m_Script",
                    SerializedPropertyHelper.PropertyName(() => Target.m_FollowOffset),
                    SerializedPropertyHelper.PropertyName(() => Target.m_BindingMode),
                    SerializedPropertyHelper.PropertyName(() => Target.m_Heading),
                    SerializedPropertyHelper.PropertyName(() => Target.m_XAxis),
                    SerializedPropertyHelper.PropertyName(() => Target.m_RecenterToTargetHeading)
                };
            if (Target.VirtualCamera.Follow == null)
                EditorGUILayout.HelpBox(
                    "A Follow target is required.  Change Body to Hard Constraint if you don't want a Follow target.", 
                    MessageType.Error);
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject,
                Target.m_HeadingIsSlave ? m_excludeFieldsSlaveMode : m_excludeFields);
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineOrbitalTransposer))]
        static void DrawTransposerGizmos(CinemachineOrbitalTransposer target, GizmoType selectionType)
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
                Vector3 pos = target.VirtualCamera.Follow.position;

                Quaternion orient = target.GetReferenceOrientation(up);
                up = orient * Vector3.up;
                DrawCircleAtPointWithRadius
                    (pos + up * target.m_FollowOffset.y, orient, target.m_FollowOffset.z);

                Gizmos.color = originalGizmoColour;
            }
        }

        internal static void DrawCircleAtPointWithRadius(Vector3 point, Quaternion orient, float radius)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(point, orient, radius * Vector3.one);

            const int kNumPoints = 25;
            Vector3 currPoint = Vector3.forward;
            Quaternion rot = Quaternion.AngleAxis(360f / (float)kNumPoints, Vector3.up);
            for (int i = 0; i < kNumPoints + 1; ++i)
            {
                Vector3 nextPoint = rot * currPoint;
                Gizmos.DrawLine(currPoint, nextPoint);
                currPoint = nextPoint;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
