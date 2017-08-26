using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(GetSetAttribute))]
    sealed class GetSetDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = (GetSetAttribute)base.attribute;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);

            if (EditorGUI.EndChangeCheck())
            {
                attribute.dirty = true;
            }
            else if (attribute.dirty)
            {
                var parent = ReflectionHelpers.GetParentObject(
                        property.propertyPath, property.serializedObject.targetObject);

                var type = parent.GetType();
                var info = type.GetProperty(attribute.name);

                if (info == null)
                    Debug.LogError("Invalid property name \"" + attribute.name + "\"");
                else
                    info.SetValue(parent, fieldInfo.GetValue(parent), null);

                attribute.dirty = false;
            }
        }
    }
}
