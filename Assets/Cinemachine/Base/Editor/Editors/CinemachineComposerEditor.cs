using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineComposer))]
    internal class CinemachineComposerEditor : UnityEditor.Editor
    {
        private CinemachineComposer Target { get { return target as CinemachineComposer; } }
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

        protected virtual string[] GetExcludedFields() { return new string[] { "m_Script" }; }

        public override void OnInspectorGUI()
        {
            if (Target.VirtualCamera.LookAt == null)
                EditorGUILayout.HelpBox("A LookAt target is required.  Change Aim to Hard Constraint if you don't want a LookAt target.", MessageType.Error);

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
            if (Target.VirtualCamera.LookAt != null && isLive)
            {
                Vector2 targetScreenPosition = brain.OutputCamera.WorldToScreenPoint(
                        Target.VirtualCamera.State.ReferenceLookAt);
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
    }
}
