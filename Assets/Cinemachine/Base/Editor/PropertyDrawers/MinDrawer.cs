using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(MinAttribute))]
    sealed class MinDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MinAttribute attribute = (MinAttribute)base.attribute;

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int v = EditorGUI.IntField(position, label, property.intValue);
                property.intValue = (int)Mathf.Max(v, attribute.min);
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                float v = EditorGUI.FloatField(position, label, property.floatValue);
                property.floatValue = Mathf.Max(v, attribute.min);
            }
            else if (property.propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 v = EditorGUI.Vector2Field(position, label, property.vector2Value);
                property.vector2Value = new Vector2(
                        Mathf.Max(v.x, attribute.min),
                        Mathf.Max(v.y, attribute.min));
            }
            else if (property.propertyType == SerializedPropertyType.Vector3)
            {
                Vector3 v = EditorGUI.Vector2Field(position, label, property.vector3Value);
                property.vector3Value = new Vector3(
                        Mathf.Max(v.x, attribute.min),
                        Mathf.Max(v.y, attribute.min),
                        Mathf.Max(v.z, attribute.min));
            }
            else if (property.propertyType == SerializedPropertyType.Vector4)
            {
                Vector4 v = EditorGUI.Vector2Field(position, label, property.vector4Value);
                property.vector4Value = new Vector4(
                        Mathf.Max(v.x, attribute.min),
                        Mathf.Max(v.y, attribute.min),
                        Mathf.Max(v.z, attribute.min),
                        Mathf.Max(v.w, attribute.min));
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use Min with int, float, or vector2/3/4.");
            }
        }
    }
}
