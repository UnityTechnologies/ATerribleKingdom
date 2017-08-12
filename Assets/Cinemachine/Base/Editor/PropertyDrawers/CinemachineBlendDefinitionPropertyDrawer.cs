using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineBlendDefinitionPropertyAttribute))]
    public sealed  class CinemachineBlendDefinitionPropertyyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float vSpace = 0;
            float hSpace = 3;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            GUIContent timeText = new GUIContent("sec");
            var textDimensions = GUI.skin.label.CalcSize(timeText);

            rect = EditorGUI.PrefixLabel(rect, label);

            rect.y += vSpace;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.width -= hSpace + floatFieldWidth + textDimensions.x;

            CinemachineBlendDefinition myClass = new CinemachineBlendDefinition(); // to access name strings
            SerializedProperty styleProp = property.FindPropertyRelative(() => myClass.m_Style);
            EditorGUI.PropertyField(rect, styleProp, GUIContent.none);

            if (styleProp.intValue != (int)CinemachineBlendDefinition.Style.Cut)
            {
                rect.x += rect.width + hSpace; rect.width = floatFieldWidth;
                SerializedProperty timeProp = property.FindPropertyRelative(() => myClass.m_Time);
                EditorGUI.PropertyField(rect, timeProp, GUIContent.none);
                rect.x += floatFieldWidth; rect.width = textDimensions.x;
                EditorGUI.LabelField(rect, timeText);
            }
        }
    }
}
