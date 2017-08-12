using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineClearShot))]
    internal sealed class CinemachineClearShotEditor : CinemachineVirtualCameraBaseEditor
    {
        private CinemachineClearShot Target { get { return target as CinemachineClearShot; } }
        EmbeddeAssetEditor<CinemachineBlenderSettings> m_BlendsEditor;

        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = new List<string>();
            excluded.AddRange(Target.m_ExcludedPropertiesInInspector);
            excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_ChildCameras));
            return excluded;
        }

        private UnityEditorInternal.ReorderableList mChildList;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_BlendsEditor = new EmbeddeAssetEditor<CinemachineBlenderSettings>(
                    SerializedPropertyHelper.PropertyName(() => Target.m_CustomBlends), this);
            m_BlendsEditor.OnChanged = (CinemachineBlenderSettings b) =>
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                };
            m_BlendsEditor.OnCreateEditor = (UnityEditor.Editor ed) =>
                {
                    CinemachineBlenderSettingsEditor editor = ed as CinemachineBlenderSettingsEditor;
                    if (editor != null)
                        editor.GetAllVirtualCameras = () => { return Target.ChildCameras; };
                };
            mChildList = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_BlendsEditor != null)
                m_BlendsEditor.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            if (mChildList == null)
                SetupChildList();

            // Ordinary properties
            base.OnInspectorGUI();

            // Blends
            m_BlendsEditor.DrawEditorCombo(
                "Create New Blender Asset",
                Target.gameObject.name + " Blends", "asset", string.Empty,
                "Custom Blends", false);

            // vcam children
            EditorGUILayout.Separator();
            EditorGUI.BeginChangeCheck();
            mChildList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void SetupChildList()
        {
            float vSpace = 2;
            float hSpace = 3;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            mChildList = new UnityEditorInternal.ReorderableList(serializedObject,
                    serializedObject.FindProperty(() => Target.m_ChildCameras),
                    true, true, true, true);

            mChildList.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Virtual Camera Children");
                    GUIContent priorityText = new GUIContent("Priority");
                    var textDimensions = GUI.skin.label.CalcSize(priorityText);
                    rect.x += rect.width - textDimensions.x;
                    rect.width = textDimensions.x;
                    EditorGUI.LabelField(rect, priorityText);
                };
            mChildList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.y += vSpace;
                    Vector2 pos = rect.position;
                    rect.width -= floatFieldWidth + hSpace;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    SerializedProperty element
                        = mChildList.serializedProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, GUIContent.none);

                    SerializedObject obj = new SerializedObject(element.objectReferenceValue);
                    pos.x += rect.width + hSpace; rect.position = pos;
                    rect.width -= floatFieldWidth + hSpace;
                    rect.width = floatFieldWidth;
                    SerializedProperty priorityProp = obj.FindProperty(() => Target.m_Priority);
                    EditorGUI.PropertyField(rect, priorityProp, GUIContent.none);
                    obj.ApplyModifiedProperties();
                };
            mChildList.onChangedCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    if (l.index < 0 || l.index >= l.serializedProperty.arraySize)
                        return;
                    Object o = l.serializedProperty.GetArrayElementAtIndex(
                            l.index).objectReferenceValue;
                    CinemachineVirtualCameraBase vcam = (o != null)
                        ? (o as CinemachineVirtualCameraBase) : null;
                    if (vcam != null)
                        vcam.transform.SetSiblingIndex(l.index);
                };
            mChildList.onAddCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    var index = l.serializedProperty.arraySize;
                    var vcam = CinemachineMenu.CreateDefaultVirtualCamera();
                    Undo.SetTransformParent(vcam.transform, Target.transform, "");
                    vcam.transform.SetSiblingIndex(index);
                };
            mChildList.onRemoveCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    Object o = l.serializedProperty.GetArrayElementAtIndex(
                            l.index).objectReferenceValue;
                    CinemachineVirtualCameraBase vcam = (o != null)
                        ? (o as CinemachineVirtualCameraBase) : null;
                    if (vcam != null)
                        Undo.DestroyObjectImmediate(vcam.gameObject);
                };
        }
    }
}
