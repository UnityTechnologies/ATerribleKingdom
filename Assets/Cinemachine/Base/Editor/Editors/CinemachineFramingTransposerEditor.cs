using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFramingTransposer))]
    internal class CinemachineFramingTransposerEditor : UnityEditor.Editor
    {
        private CinemachineFramingTransposer Target { get { return target as CinemachineFramingTransposer; } }
        CinemachineScreenComposerGuides mScreenGuideEditor;

        protected virtual void OnEnable()
        {
            mScreenGuideEditor = new CinemachineScreenComposerGuides();
            mScreenGuideEditor.GetHardGuide = () => { return Target.HardGuideRect; };
            mScreenGuideEditor.GetSoftGuide = () => { return Target.SoftGuideRect; };
            mScreenGuideEditor.SetHardGuide = (Rect r) => { Target.HardGuideRect = r; };
            mScreenGuideEditor.SetSoftGuide = (Rect r) => { Target.SoftGuideRect = r; };
            mScreenGuideEditor.Target = () => { return serializedObject; };

            Target.OnGUICallback += OnGUI;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        protected virtual void OnDisable()
        {
            if (Target != null)
                Target.OnGUICallback -= OnGUI;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        protected virtual string[] GetExcludedFields()
        {
            List<string> excluded = new List<string> { "m_Script" };
            CinemachineTargetGroup group = Target.TargetGroup;
            if (group == null)
            {
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_GroupFramingSize));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_FramingMode));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_FrameDamping));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_AdjustmentMode));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyIn));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyOut));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MinimumDistance));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaximumDistance));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MinimumFOV));
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaximumFOV));
            }
            else 
            {
                CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
                bool ortho = brain != null ? brain.OutputCamera.orthographic : false;
                if (ortho)
                {
                    excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_AdjustmentMode));
                    excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyIn));
                    excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyOut));
                    excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MinimumDistance));
                    excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaximumDistance));
                    excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MinimumFOV));
                    excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaximumFOV));
                }
                else switch (Target.m_AdjustmentMode)
                {
                    case CinemachineFramingTransposer.AdjustmentMode.DollyOnly:
                        excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MinimumFOV));
                        excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaximumFOV));
                        break;
                    case CinemachineFramingTransposer.AdjustmentMode.ZoomOnly:
                        excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyIn));
                        excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaxDollyOut));
                        excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MinimumDistance));
                        excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_MaximumDistance));
                        break;
                    default:
                        break;
                }
            }
            return excluded.ToArray();
        }
        
        public override void OnInspectorGUI()
        {
            if (Target.VirtualCamera.Follow == null)
                EditorGUILayout.HelpBox(
                    "A Follow target is required.  Change Body to Hard Constraint if you don't want a Follow target.", 
                    MessageType.Error);
            if (Target.VirtualCamera.LookAt != null)
                EditorGUILayout.HelpBox(
                    "The LookAt target must be null.  The Follow target will be used in place of the LookAt target.",
                    MessageType.Error);

            serializedObject.Update();

            // First snapshot some settings
            Rect oldHard = Target.HardGuideRect;
            Rect oldSoft = Target.SoftGuideRect;

            // Draw the properties
            DrawPropertiesExcluding(serializedObject, GetExcludedFields());
            serializedObject.ApplyModifiedProperties();
            mScreenGuideEditor.SetNewBounds(oldHard, oldSoft, Target.HardGuideRect, Target.SoftGuideRect);
        }

        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (!Target.IsValid || !CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides)
                return;

            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
            if (brain == null || brain.OutputCamera.activeTexture != null)
                return;

            bool isLive = CinemachineCore.Instance.IsLive(Target.VirtualCamera);

            // Screen guides
            mScreenGuideEditor.OnGUI_DrawGuides(isLive, brain.OutputCamera, Target.VirtualCamera.State.Lens);

            // Draw an on-screen gizmo for the target
            if (Target.VirtualCamera.Follow != null && isLive)
            {
                Vector2 targetScreenPosition = brain.OutputCamera.WorldToScreenPoint(
                        Target.VirtualCamera.Follow.position);
                targetScreenPosition.y = Screen.height - targetScreenPosition.y;

                GUI.color = CinemachineSettings.ComposerSettings.TargetColour;
                Rect r = new Rect(targetScreenPosition, Vector2.zero);
                float size = (CinemachineSettings.ComposerSettings.TargetSize 
                    + CinemachineScreenComposerGuides.kGuideBarWidthPx) / 2;
                GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                size -= CinemachineScreenComposerGuides.kGuideBarWidthPx;
                if (size > 0)
                {
                    Vector4 overlayOpacityScalar 
                        = new Vector4(1f, 1f, 1f, CinemachineSettings.ComposerSettings.OverlayOpacity);
                    GUI.color = Color.black * overlayOpacityScalar;
                    GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                }
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineFramingTransposer))]
        private static void DrawGroupComposerGizmos(CinemachineFramingTransposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            CinemachineTargetGroup group = target.TargetGroup;
            if (group != null)
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
