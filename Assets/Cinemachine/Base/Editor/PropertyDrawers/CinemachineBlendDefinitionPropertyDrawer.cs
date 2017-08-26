using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineBlendDefinitionPropertyAttribute))]
    public sealed  class CinemachineBlendDefinitionPropertyDrawer : PropertyDrawer
    {
        CinemachineBlendDefinition myClass = new CinemachineBlendDefinition(); // to access name strings
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float vSpace = 0;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            float hBigSpace = EditorGUIUtility.singleLineHeight * 2 / 3;

            GUIContent timeText = new GUIContent("sec");
            var textDimensions = GUI.skin.label.CalcSize(timeText);

            rect = EditorGUI.PrefixLabel(rect, label);

            rect.y += vSpace;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.width -= hBigSpace + floatFieldWidth + textDimensions.x;

            SerializedProperty styleProp = property.FindPropertyRelative(() => myClass.m_Style);
            EditorGUI.PropertyField(rect, styleProp, GUIContent.none);

            if (styleProp.intValue != (int)CinemachineBlendDefinition.Style.Cut)
            {
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = hBigSpace; 
                rect.x += rect.width; rect.width = floatFieldWidth + hBigSpace;
                SerializedProperty timeProp = property.FindPropertyRelative(() => myClass.m_Time);
                float v = EditorGUI.FloatField(rect, new GUIContent(" "), timeProp.floatValue);
                timeProp.floatValue = Mathf.Max(v, 0);
                EditorGUIUtility.labelWidth = oldWidth; 
                rect.x += rect.width; rect.width = textDimensions.x;
                EditorGUI.LabelField(rect, timeText);
            }
        }
    }
}
