using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOrbitalTransposer))]
    internal sealed class CinemachineOrbitalTransposerEditor : UnityEditor.Editor
    {
        private CinemachineOrbitalTransposer Target { get { return target as CinemachineOrbitalTransposer; } }
        private static string[] m_excludeFields = new string[] { "m_Script" };
        private static string[] m_excludeFieldsSlaveMode;

        public override void OnInspectorGUI()
        {
            if (m_excludeFieldsSlaveMode == null)
                m_excludeFieldsSlaveMode = new string[]
                {
                    "m_Script",
                    SerializedPropertyHelper.PropertyName(() => Target.m_DampingStyle),
                    SerializedPropertyHelper.PropertyName(() => Target.m_HeadingBias),
                    SerializedPropertyHelper.PropertyName(() => Target.m_XAxis),
                    SerializedPropertyHelper.PropertyName(() => Target.m_RecenterToTargetHeading)
                };
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject,
                Target.m_HeadingIsSlave ? m_excludeFieldsSlaveMode : m_excludeFields);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
