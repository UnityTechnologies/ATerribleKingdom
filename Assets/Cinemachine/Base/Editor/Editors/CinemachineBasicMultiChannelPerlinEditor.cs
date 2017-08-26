using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBasicMultiChannelPerlin))]
    internal sealed class CinemachineBasicMultiChannelPerlinEditor : UnityEditor.Editor
    {
        private CinemachineBasicMultiChannelPerlin Target { get { return target as CinemachineBasicMultiChannelPerlin; } }
        private static readonly string[] m_excludeFields = new string[] { "m_Script" };
        EmbeddeAssetEditor<NoiseSettings> m_SettingsEditor;

        private void OnEnable()
        {
            m_SettingsEditor = new EmbeddeAssetEditor<NoiseSettings>(
                    SerializedPropertyHelper.PropertyName(() => Target.m_Definition), this);
            m_SettingsEditor.OnChanged = (NoiseSettings noise) =>
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                };
        }

        private void OnDisable()
        {
            if (m_SettingsEditor != null)
                m_SettingsEditor.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (Target.m_Definition == null)
                EditorGUILayout.HelpBox("A Noise Definition is required", MessageType.Error);

            DrawPropertiesExcluding(serializedObject, m_excludeFields);
            serializedObject.ApplyModifiedProperties();

            m_SettingsEditor.DrawEditorCombo(
                "Create New Noise Asset",
                Target.gameObject.name + " Noise Settings", "asset", string.Empty,
                "Noise Settings", true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
