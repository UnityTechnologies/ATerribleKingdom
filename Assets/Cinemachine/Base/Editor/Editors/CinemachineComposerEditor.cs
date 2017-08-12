using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineComposer))]
    internal class CinemachineComposerEditor : UnityEditor.Editor
    {
        private CinemachineComposer Target { get { return target as CinemachineComposer; } }
        private const float kGuideBarWidthPx = 3f;

        protected virtual void OnEnable()
        {
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
            serializedObject.Update();

            // First snapshot some settings
            Rect oldHard = Target.HardGuideRect;
            Rect oldSoft = Target.SoftGuideRect;

            // Draw the properties
            DrawPropertiesExcluding(serializedObject, GetExcludedFields());
            serializedObject.ApplyModifiedProperties();

            // Enforce some rules
            Rect newHard = Target.HardGuideRect;
            Rect newSoft = Target.SoftGuideRect;
            SetNewBounds(oldHard, oldSoft, Target.HardGuideRect, Target.SoftGuideRect);
        }

        void SetNewBounds(Rect oldHard, Rect oldSoft, Rect newHard, Rect newSoft)
        {
            if ((oldSoft != newSoft) || (oldHard != newHard))
            {
                Undo.RecordObject(Target, "Composer Bounds");
                if (oldSoft != newSoft)
                    Target.SoftGuideRect = newSoft;
                if (oldHard != newHard)
                    Target.HardGuideRect = newHard;
                serializedObject.ApplyModifiedProperties();
            }
        }

        // For dragging the bars - order defines precedence
        private enum DragBar
        {
            Center,
            SoftBarLineLeft, SoftBarLineTop, SoftBarLineRight, SoftBarLineBottom,
            HardBarLineLeft, HardBarLineTop, HardBarLineRight, HardBarLineBottom,
            NONE
        };
        private DragBar mDragging = DragBar.NONE;
        private Rect[] mDragBars = new Rect[9];

        private void OnGuiHandleBarDragging(float screenWidth, float screenHeight)
        {
            if (Event.current.type == EventType.MouseUp)
                mDragging = DragBar.NONE;
            if (Event.current.type == EventType.MouseDown)
            {
                mDragging = DragBar.NONE;
                for (DragBar i = DragBar.Center; i < DragBar.NONE && mDragging == DragBar.NONE; ++i)
                {
                    Vector2 slop = new Vector2(5f, 5f);
                    if (i == DragBar.Center)
                    {
                        if (mDragBars[(int)i].width > 3f * slop.x)
                            slop.x = -slop.x;
                        if (mDragBars[(int)i].height > 3f * slop.y)
                            slop.y = -slop.y;
                    }
                    Rect r = mDragBars[(int)i].Inflated(slop);
                    if (r.Contains(Event.current.mousePosition))
                        mDragging = i;
                }
            }

            if (mDragging != DragBar.NONE && Event.current.type == EventType.MouseDrag)
            {
                Vector2 d = new Vector2(
                        Event.current.delta.x / screenWidth,
                        Event.current.delta.y / screenHeight);

                // First snapshot some settings
                Rect newHard = Target.HardGuideRect;
                Rect newSoft = Target.SoftGuideRect;
                Vector2 changed = Vector2.zero;
                switch (mDragging)
                {
                    case DragBar.Center: newSoft.position += d; break;
                    case DragBar.SoftBarLineLeft: newSoft = newSoft.Inflated(new Vector2(-d.x, 0)); break;
                    case DragBar.SoftBarLineRight: newSoft = newSoft.Inflated(new Vector2(d.x, 0)); break;
                    case DragBar.SoftBarLineTop: newSoft = newSoft.Inflated(new Vector2(0, -d.y)); break;
                    case DragBar.SoftBarLineBottom: newSoft = newSoft.Inflated(new Vector2(0, d.y)); break;
                    case DragBar.HardBarLineLeft: newHard = newHard.Inflated(new Vector2(-d.x, 0)); break;
                    case DragBar.HardBarLineRight: newHard = newHard.Inflated(new Vector2(d.x, 0)); break;
                    case DragBar.HardBarLineBottom: newHard = newHard.Inflated(new Vector2(0, d.y)); break;
                    case DragBar.HardBarLineTop: newHard = newHard.Inflated(new Vector2(0, -d.y)); break;
                }

                // Apply the changes, enforcing the bounds
                SetNewBounds(Target.HardGuideRect, Target.SoftGuideRect, newHard, newSoft);
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (!Target.m_ShowGuides)
                return;

            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
            if (brain == null || brain.OutputCamera.activeTexture != null)
                return;

            bool isLive = CinemachineCore.Instance.IsLive(Target.VirtualCamera);

            Rect cameraRect = brain.OutputCamera.pixelRect;
            float screenWidth = cameraRect.width;
            float screenHeight = cameraRect.height;
            cameraRect.yMax = Screen.height - cameraRect.yMin;
            cameraRect.yMin = cameraRect.yMax - screenHeight;

            // Rotate the guides along with the dutch
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Translate(cameraRect.min);
            GUIUtility.RotateAroundPivot(Target.VirtualCamera.State.Lens.Dutch, cameraRect.center);

            Color hardBarsColour = CinemachineSettings.ComposerSettings.HardBoundsOverlayColour;
            Color softBarsColour = CinemachineSettings.ComposerSettings.SoftBoundsOverlayColour;
            if (!isLive)
            {
                softBarsColour = CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;
                hardBarsColour = Color.Lerp(softBarsColour, Color.black, 0.5f);
            }
            float overlayOpacity = CinemachineSettings.ComposerSettings.OverlayOpacity;
            Vector4 overlayOpacityScalar = new Vector4(1f, 1f, 1f, overlayOpacity);
            hardBarsColour *= overlayOpacityScalar;
            softBarsColour *= overlayOpacityScalar;

            Rect r = Target.HardGuideRect;
            float hardEdgeLeft = r.xMin * screenWidth;
            float hardEdgeTop = r.yMin * screenHeight;
            float hardEdgeRight = r.xMax * screenWidth;
            float hardEdgeBottom = r.yMax * screenHeight;

            mDragBars[(int)DragBar.HardBarLineLeft] = new Rect(hardEdgeLeft - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.HardBarLineTop] = new Rect(0f, hardEdgeTop - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);
            mDragBars[(int)DragBar.HardBarLineRight] = new Rect(hardEdgeRight - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.HardBarLineBottom] = new Rect(0f, hardEdgeBottom - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);

            r = Target.SoftGuideRect;
            float softEdgeLeft = r.xMin * screenWidth;
            float softEdgeTop = r.yMin * screenHeight;
            float softEdgeRight = r.xMax * screenWidth;
            float softEdgeBottom = r.yMax * screenHeight;

            mDragBars[(int)DragBar.SoftBarLineLeft] = new Rect(softEdgeLeft - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.SoftBarLineTop] = new Rect(0f, softEdgeTop - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);
            mDragBars[(int)DragBar.SoftBarLineRight] = new Rect(softEdgeRight - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.SoftBarLineBottom] = new Rect(0f, softEdgeBottom - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);

            mDragBars[(int)DragBar.Center] = new Rect(softEdgeLeft, softEdgeTop, softEdgeRight - softEdgeLeft, softEdgeBottom - softEdgeTop);

            // Handle dragging bars
            OnGuiHandleBarDragging(screenWidth, screenHeight);

            // Draw the masks
            GUI.color = hardBarsColour;
            Rect hardBarLeft = new Rect(0, hardEdgeTop, Mathf.Max(0, hardEdgeLeft), hardEdgeBottom - hardEdgeTop);
            Rect hardBarRight = new Rect(hardEdgeRight, hardEdgeTop,
                    Mathf.Max(0, screenWidth - hardEdgeRight), hardEdgeBottom - hardEdgeTop);
            Rect hardBarTop = new Rect(Mathf.Min(0, hardEdgeLeft), 0,
                    Mathf.Max(screenWidth, hardEdgeRight) - Mathf.Min(0, hardEdgeLeft), Mathf.Max(0, hardEdgeTop));
            Rect hardBarBottom = new Rect(Mathf.Min(0, hardEdgeLeft), hardEdgeBottom,
                    Mathf.Max(screenWidth, hardEdgeRight) - Mathf.Min(0, hardEdgeLeft),
                    Mathf.Max(0, screenHeight - hardEdgeBottom));
            GUI.DrawTexture(hardBarLeft, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(hardBarTop, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(hardBarRight, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(hardBarBottom, Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.color = softBarsColour;
            Rect softBarLeft = new Rect(hardEdgeLeft, softEdgeTop, softEdgeLeft - hardEdgeLeft, softEdgeBottom - softEdgeTop);
            Rect softBarTop = new Rect(hardEdgeLeft, hardEdgeTop, hardEdgeRight - hardEdgeLeft, softEdgeTop - hardEdgeTop);
            Rect softBarRight = new Rect(softEdgeRight, softEdgeTop, hardEdgeRight - softEdgeRight, softEdgeBottom - softEdgeTop);
            Rect softBarBottom = new Rect(hardEdgeLeft, softEdgeBottom, hardEdgeRight - hardEdgeLeft, hardEdgeBottom - softEdgeBottom);
            GUI.DrawTexture(softBarLeft, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(softBarTop, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(softBarRight, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(softBarBottom, Texture2D.whiteTexture, ScaleMode.StretchToFill);

            // Draw the drag bars
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineLeft], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineTop], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineRight], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineBottom], Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.color = hardBarsColour;
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineLeft], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineTop], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineRight], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineBottom], Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.matrix = oldMatrix;

            // Draw an on-screen gizmo for the target
            if (Target.VirtualCamera.LookAt != null && isLive)
            {
                Vector2 targetScreenPosition = brain.OutputCamera.WorldToScreenPoint(
                        Target.VirtualCamera.State.ReferenceLookAt);
                targetScreenPosition.y = Screen.height - targetScreenPosition.y;

                GUI.color = CinemachineSettings.ComposerSettings.TargetColour;
                r = new Rect(targetScreenPosition, Vector2.zero);
                float size = (CinemachineSettings.ComposerSettings.TargetSize + kGuideBarWidthPx) / 2;
                GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                size -= kGuideBarWidthPx;
                if (size > 0)
                {
                    GUI.color = Color.black * overlayOpacityScalar;
                    GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                }
            }
        }
    }
}
