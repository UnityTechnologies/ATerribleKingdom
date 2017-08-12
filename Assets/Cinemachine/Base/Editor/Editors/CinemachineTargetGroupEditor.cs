using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTargetGroup))]
    internal sealed class CinemachineTargetGroupEditor : UnityEditor.Editor
    {
        private CinemachineTargetGroup Target { get { return target as CinemachineTargetGroup; } }
        private static readonly string[] m_excludeFields = new string[] { "m_Script", "m_Targets" };

        private UnityEditorInternal.ReorderableList mTargetList;

        void OnEnable()
        {
            mTargetList = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, m_excludeFields);
            serializedObject.ApplyModifiedProperties();

            if (mTargetList == null)
                SetupTargetList();
            EditorGUI.BeginChangeCheck();
            mTargetList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void SetupTargetList()
        {
            float vSpace = 2;
            float hSpace = 3;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 3f;

            mTargetList = new UnityEditorInternal.ReorderableList(serializedObject,
                    serializedObject.FindProperty(() => Target.m_Targets),
                    true, true, true, true);

            // Needed for accessing field names as strings
            CinemachineTargetGroup.Target def = new CinemachineTargetGroup.Target();

            mTargetList.drawHeaderCallback = (Rect rect) =>
                {
                    rect.width -= (EditorGUIUtility.singleLineHeight + 2 * hSpace);
                    rect.width -= 2 * floatFieldWidth;
                    Vector2 pos = rect.position; pos.x += EditorGUIUtility.singleLineHeight;
                    rect.position = pos;
                    EditorGUI.LabelField(rect, "Target");

                    pos.x += rect.width + hSpace; rect.width = floatFieldWidth; rect.position = pos;
                    EditorGUI.LabelField(rect, "Weight");

                    pos.x += rect.width + hSpace; rect.position = pos;
                    EditorGUI.LabelField(rect, "Radius");
                };

            mTargetList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty elemProp = mTargetList.serializedProperty.GetArrayElementAtIndex(index);

                    rect.y += vSpace;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    Vector2 pos = rect.position;
                    rect.width -= 3 * hSpace;
                    rect.width -= 2 * floatFieldWidth;
                    EditorGUI.PropertyField(rect, elemProp.FindPropertyRelative(() => def.target), GUIContent.none);

                    pos.x += rect.width + hSpace; rect.width = floatFieldWidth; rect.position = pos;
                    EditorGUI.PropertyField(rect, elemProp.FindPropertyRelative(() => def.weight), GUIContent.none);

                    pos.x += rect.width + hSpace; rect.position = pos;
                    EditorGUI.PropertyField(rect, elemProp.FindPropertyRelative(() => def.radius), GUIContent.none);
                };

            mTargetList.onAddCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    var index = l.serializedProperty.arraySize;
                    ++l.serializedProperty.arraySize;
                    SerializedProperty elemProp = mTargetList.serializedProperty.GetArrayElementAtIndex(index);
                    elemProp.FindPropertyRelative(() => def.weight).floatValue = 1;
                };
        }
    }
}
