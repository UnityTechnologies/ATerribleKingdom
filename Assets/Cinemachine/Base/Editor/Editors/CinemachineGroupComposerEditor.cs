using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineGroupComposer))]
    internal class CinemachineGroupComposerEditor : CinemachineComposerEditor
    {
        private CinemachineGroupComposer Target { get { return target as CinemachineGroupComposer; } }

        protected override string[] GetExcludedFields()
        {
            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
            bool ortho = brain != null ? brain.OutputCamera.orthographic : false;
            if (ortho)
            {
                return new string[]
                {
                    "m_Script",
                    SerializedPropertyHelper.PropertyName(() => Target.m_AdjustmentMode),
                    SerializedPropertyHelper.PropertyName(() => Target.m_MinimumFOV),
                    SerializedPropertyHelper.PropertyName(() => Target.m_MaximumFOV),
                    SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyIn),
                    SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyOut),
                    SerializedPropertyHelper.PropertyName(() => Target.m_MinimumDistance),
                    SerializedPropertyHelper.PropertyName(() => Target.m_MaximumDistance)
                };
            }
            switch (Target.m_AdjustmentMode)
            {
                case CinemachineGroupComposer.AdjustmentMode.DollyOnly:
                    return new string[]
                    {
                        "m_Script",
                        SerializedPropertyHelper.PropertyName(() => Target.m_MinimumFOV),
                        SerializedPropertyHelper.PropertyName(() => Target.m_MaximumFOV)
                    };
                case CinemachineGroupComposer.AdjustmentMode.ZoomOnly:
                    return new string[]
                    {
                        "m_Script",
                        SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyIn),
                        SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyOut),
                        SerializedPropertyHelper.PropertyName(() => Target.m_MinimumDistance),
                        SerializedPropertyHelper.PropertyName(() => Target.m_MaximumDistance)
                    };
                default:
                    return new string[] { "m_Script" };
            }
        }

        public override void OnInspectorGUI()
        {
            if (Target.IsValid && Target.TargetGroup == null)
                EditorGUILayout.HelpBox(
                    "The Framing settings will be ignored because the LookAt target is not a kind of CinemachineTargetGroup", 
                    MessageType.Info);

            base.OnInspectorGUI();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupComposer))]
        private static void DrawGroupComposerGizmos(CinemachineGroupComposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.TargetGroup != null)
            {
                Matrix4x4 m = Gizmos.matrix;
                Bounds b = target.m_LastBounds;
                Gizmos.matrix = target.m_lastBoundsMatrix;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(b.center, b.size);
                Gizmos.matrix = m;
            }
        }
    }
}
