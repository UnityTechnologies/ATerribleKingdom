using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Cinemachine.Editor;

namespace Cinemachine.Blackboard.Editor
{
    [CustomEditor(typeof(Reactor))]
    public sealed class ReactorEditor : UnityEditor.Editor
    {
        private Reactor Target { get { return target as Reactor; } }
        //private static readonly string[] m_excludeFields = new string[] { "m_Script" };

        string[] mFieldNames;
        string[] mFieldDisplayNames;
        UnityEditorInternal.ReorderableList mMappingList;
        UnityEditorInternal.ReorderableList mExpressionList;
        int mExpressionListForMappingIndex = -1;

        private void OnEnable()
        {
            mMappingList = null;
            mExpressionList = null;
            mExpressionListForMappingIndex = -1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (mMappingList == null)
                SetupMappingList();
            if (mMappingList.count > 0 && mMappingList.index < 0)
                mMappingList.index = 0;

            UpdateFieldNames();

            GUIStyle helpboxStyle = new GUIStyle("HelpBox");
            helpboxStyle.richText = true;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            EditorGUILayout.TextArea(
                "Choose the fields you want to modify, and define the function in the second list below.", helpboxStyle);
            mMappingList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Target.InvalidateBindings();
            }

            EditorGUI.BeginChangeCheck();
            {
                int selectedMapping = mMappingList.index;
                if (selectedMapping >= 0)
                {
                    EditorGUILayout.Space();

                    // Get the field display name
                    int index = Array.FindIndex(mFieldNames,
                            (match) => match == Target.m_TargetMappings[selectedMapping].m_Field);
                    string fieldName = index >= 0 ? mFieldDisplayNames[index] : "(unknown field)";

                    string helpText = fieldName + "\nwill be ";
                    switch (Target.m_TargetMappings[selectedMapping].m_Operation)
                    {
                        case Reactor.CombineMode.Set: helpText += "<b>set</b> to"; break;
                        case Reactor.CombineMode.Add: helpText += "modified by <b>adding</b> "; break;
                        case Reactor.CombineMode.Subtract: helpText += "modified by <b>subtracting</b>"; break;
                        case Reactor.CombineMode.Multiply: helpText += "<b>multiplied</b> by"; break;
                        case Reactor.CombineMode.Divide: helpText += "<b>divided</b> by"; break;
                    }
                    helpText += " the result of the following expression:";
                    EditorGUILayout.TextArea(helpText, helpboxStyle);

                    if (selectedMapping != mExpressionListForMappingIndex)
                    {
                        SetupExpressionList(selectedMapping);
                        mExpressionListForMappingIndex = selectedMapping;
                    }
                    mExpressionList.DoLayoutList();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Target.InvalidateBindings();
            }
        }

        void UpdateFieldNames()
        {
            List<string> fieldNames = new List<string>();
            List<string> fieldDisplayNames = new List<string>();
            Reactor.GameObjectFieldScanner scanner = new Reactor.GameObjectFieldScanner();
            scanner.OnLeafField = (fullName, fieldInfo, rootFieldOwner, value) =>
                {
                    fieldNames.Add(fullName);
                    fieldDisplayNames.Add(NicifyName(fullName));
                    return true; // keep going
                };
            scanner.ScanFields(Target.gameObject);
            mFieldNames = fieldNames.ToArray();
            mFieldDisplayNames = fieldDisplayNames.ToArray();
        }

        string NicifyName(string name)
        {
            name = name.Replace(CinemachineVirtualCamera.PipelineName, string.Empty);
            name = name.Replace("Cinemachine", string.Empty);
            string[] path = name.Split('.');
            for (int i = 0; i < path.Length; ++i)
            {
                path[i] = ObjectNames.NicifyVariableName(path[i]);
                path[i] = path[i].Replace(" ", string.Empty);
            }
            name = string.Empty;
            for (int i = 0; i < path.Length; ++i)
            {
                if (path[i].Length > 0)
                {
                    if (name.Length > 0)
                        name += ".";
                    name += path[i];
                }
            }
            return name;
        }

        void SetupMappingList()
        {
            mMappingList = new UnityEditorInternal.ReorderableList(
                    serializedObject,
                    serializedObject.FindProperty(() => Target.m_TargetMappings),
                    true, true, true, true);

            // Needed for accessing field names as strings
            Reactor.TargetModifier def = new Reactor.TargetModifier();

            float vSpace = 2;
            float hSpace = 3;
            float opFieldWidth = EditorGUIUtility.singleLineHeight * 4f;
            mMappingList.drawHeaderCallback = (Rect rect) =>
                {
                    rect.position += new Vector2(EditorGUIUtility.singleLineHeight, 0);
                    rect.width -= opFieldWidth + hSpace + EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(rect, "Target Field");

                    rect.position += new Vector2(rect.width + hSpace, 0);
                    rect.width = opFieldWidth;
                    EditorGUI.LabelField(rect, "Operation");
                };

            mMappingList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element
                        = mMappingList.serializedProperty.GetArrayElementAtIndex(index);

                    rect.y += vSpace;
                    rect.height = EditorGUIUtility.singleLineHeight;

                    Rect r = rect;
                    r.width -= opFieldWidth + hSpace;
                    SerializedProperty field = element.FindPropertyRelative(() => def.m_Field);
                    int currentField = Array.FindIndex(mFieldNames, match => match == field.stringValue);
                    int fieldSelection = EditorGUI.Popup(r, currentField, mFieldDisplayNames);
                    if (currentField != fieldSelection)
                        field.stringValue = (fieldSelection < 0) ? "" : mFieldNames[fieldSelection];

                    r.position += new Vector2(r.width + hSpace, 0);
                    r.width = opFieldWidth;
                    EditorGUI.PropertyField(r, element.FindPropertyRelative(() => def.m_Operation), GUIContent.none);
                };
        }

        void SetupExpressionList(int selectedMapping)
        {
            SerializedProperty mappingsProp = serializedObject.FindProperty(() => Target.m_TargetMappings);
            SerializedProperty selectedMappingProp = mappingsProp.GetArrayElementAtIndex(selectedMapping);
            SerializedProperty expressionProp = selectedMappingProp.FindPropertyRelative("m_Expression");
            SerializedProperty expressionListProp = expressionProp.FindPropertyRelative("m_Lines");

            mExpressionList = new UnityEditorInternal.ReorderableList(
                    serializedObject, expressionListProp, true, true, true, true);

            // Needed for accessing field names as strings
            Reactor.BlackboardExpression.Line def = new Reactor.BlackboardExpression.Line();

            float vSpace = 2;
            float hSpace = 3;
            float opFieldWidth = EditorGUIUtility.singleLineHeight * 4f;
            mExpressionList.drawHeaderCallback = (Rect rect) =>
                {
                    rect.position += new Vector2(EditorGUIUtility.singleLineHeight, 0);
                    rect.width -= EditorGUIUtility.singleLineHeight;

                    Rect r = rect;
                    r.width = opFieldWidth;
                    EditorGUI.LabelField(r, "Operation");

                    r.position += new Vector2(r.width + hSpace, 0);
                    r.width = (rect.width - (r.width + hSpace * 2)) / 2;
                    EditorGUI.LabelField(r, "Blackboard Key");

                    r.position += new Vector2(r.width + hSpace, 0);
                    EditorGUI.LabelField(r, "Remap Curve");
                };

            mExpressionList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = mExpressionList.serializedProperty.GetArrayElementAtIndex(index);

                    rect.y += vSpace;
                    rect.height = EditorGUIUtility.singleLineHeight;

                    Rect r = rect;
                    r.width = opFieldWidth;
                    if (index == 0)
                        EditorGUI.LabelField(r, "Start with");
                    else
                        EditorGUI.PropertyField(r, element.FindPropertyRelative(() => def.m_Operation), GUIContent.none);

                    r.position += new Vector2(r.width + hSpace, 0);
                    r.width = (rect.width - (r.width + hSpace * 2)) / 2;
                    EditorGUI.PropertyField(r, element.FindPropertyRelative(() => def.m_BlackboardKey), GUIContent.none);

                    r.position += new Vector2(r.width + hSpace, 0);
                    EditorGUI.PropertyField(new Rect(r.position, new Vector2(rect.height, rect.height)),
                        element.FindPropertyRelative(() => def.m_Remap), GUIContent.none);

                    GUI.enabled = element.FindPropertyRelative(() => def.m_Remap).boolValue;
                    r.position += new Vector2(rect.height, 0); r.width -= rect.height;
                    EditorGUI.PropertyField(r, element.FindPropertyRelative(() => def.m_RemapCurve), GUIContent.none);
                    GUI.enabled = true;
                };
        }

        /// <summary>
        /// Register a callback with the SaveDuringPlay mechanism to call before hot-saving
        /// tweaks made during play-mode
        /// </summary>
        [InitializeOnLoad]
        class RegisterHotSave
        {
            static RegisterHotSave()
            {
                SaveDuringPlay.SaveDuringPlay.OnHotSave = OnHotSave;
            }

            // Before hot-save, we must restore all initial values touched by reactors
            static void OnHotSave()
            {
                Reactor[] reactors = SaveDuringPlay.ObjectTreeUtil.FindAllBehavioursInScene<Reactor>();
                if (reactors != null)
                    foreach (var r in reactors)
                        if (r != null)
                            r.InvalidateBindings();
            }
        }
    }
}
