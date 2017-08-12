using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBrain))]
    internal sealed class CinemachineBrainEditor : UnityEditor.Editor
    {
        private CinemachineBrain Target { get { return target as CinemachineBrain; } }
        private static string[] m_excludeFields;
        EmbeddeAssetEditor<CinemachineBlenderSettings> m_SettingsEditor;

        bool mEventsExpanded = false;

        private void OnEnable()
        {
            m_SettingsEditor = new EmbeddeAssetEditor<CinemachineBlenderSettings>(
                    SerializedPropertyHelper.PropertyName(() => Target.m_CustomBlends), this);
            m_SettingsEditor.OnChanged = (CinemachineBlenderSettings b) =>
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

            // Show the active camera and blend
            GUI.enabled = false;
            ICinemachineCamera vcam = Target.ActiveVirtualCamera;
            Transform activeCam = (vcam != null && vcam.VirtualCameraGameObject != null)
                ? vcam.VirtualCameraGameObject.transform : null;
            EditorGUILayout.ObjectField("Live Camera", activeCam, typeof(Transform), true);
            EditorGUILayout.DelayedTextField(
                "Live Blend", Target.ActiveBlend != null
                ? Target.ActiveBlend.Description : string.Empty);
            GUI.enabled = true;

            // Normal properties
            if (m_excludeFields == null)
                m_excludeFields = new string[]
                {
                    "m_Script",
                    SerializedPropertyHelper.PropertyName(() => Target.m_CameraCutEvent),
                    SerializedPropertyHelper.PropertyName(() => Target.m_CameraActivatedEvent)
                };
            DrawPropertiesExcluding(serializedObject, m_excludeFields);

            m_SettingsEditor.DrawEditorCombo(
                "Create New Blender Asset",
                Target.gameObject.name + " Blends", "asset", string.Empty,
                "Custom Blends", false);

            mEventsExpanded = EditorGUILayout.Foldout(mEventsExpanded, "Events");
            if (mEventsExpanded)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.m_CameraCutEvent));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.m_CameraActivatedEvent));
            }
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected, typeof(CinemachineBrain))]
        private static void DrawBrainGizmos(CinemachineBrain brain, GizmoType drawType)
        {
            if (brain.OutputCamera != null && brain.m_ShowCameraFrustum)
            {
                DrawCameraFrustumGizmo(
                    brain, new LensSettings(brain.OutputCamera), 
                    brain.OutputCamera.transform.localToWorldMatrix, 
                    Color.white); // GML why is this color hardcoded?
            }
        }

        internal static void DrawCameraFrustumGizmo(
            CinemachineBrain brain, LensSettings lens, 
            Matrix4x4 transform, Color color)
        {
            float aspect = 1;
            bool ortho = false;
            if (brain != null)
            {
                aspect = brain.OutputCamera.aspect;
                ortho = brain.OutputCamera.orthographic;
            }

            Matrix4x4 originalMatrix = Gizmos.matrix;
            Color originalGizmoColour = Gizmos.color;
            Gizmos.color = color;
            Gizmos.matrix = transform;
            if (ortho)
            {
                Vector3 size = new Vector3(
                        aspect * lens.OrthographicSize * 2, 
                        lens.OrthographicSize * 2, 
                        lens.NearClipPlane + lens.FarClipPlane);
                Gizmos.DrawWireCube(
                    new Vector3(0, 0, (size.z / 2) + lens.NearClipPlane), size);
            }
            else
            {
                Gizmos.DrawFrustum(
                        Vector3.zero, lens.FieldOfView,
                        lens.FarClipPlane, lens.NearClipPlane, aspect);
            }
            Gizmos.matrix = originalMatrix;
            Gizmos.color = originalGizmoColour;
        }
    }
}
