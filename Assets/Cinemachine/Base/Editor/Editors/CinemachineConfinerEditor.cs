using UnityEngine;
using UnityEditor;
using System;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineConfiner))]
    public sealed class CinemachineConfinerEditor : UnityEditor.Editor
    {
        private CinemachineConfiner Target { get { return target as CinemachineConfiner; } }
        private static readonly string[] m_excludeFields = new string[] { "m_Script" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, m_excludeFields);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (Target.m_BoundingVolume == null)
                EditorGUILayout.HelpBox("A Bounding Volume is required.", MessageType.Error);
            else if (Target.m_BoundingVolume.GetType() != typeof(BoxCollider)
                && Target.m_BoundingVolume.GetType() != typeof(SphereCollider)
                && Target.m_BoundingVolume.GetType() != typeof(CapsuleCollider))
            {
                EditorGUILayout.HelpBox(
                    "Must be a BoxCollider, SphereCollider, or CapsuleCollider.", 
                    MessageType.Error);
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected, typeof(CinemachineConfiner))]
        private static void DrawColliderGizmos(CinemachineConfiner confiner, GizmoType type)
        {
            CinemachineVirtualCameraBase vcam = (confiner != null) ? confiner.VirtualCamera : null;
            if (vcam != null && confiner.enabled && confiner.m_BoundingVolume != null)
            {
                Color oldColor = Gizmos.color;
                Gizmos.color = Color.yellow;
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Transform t = confiner.m_BoundingVolume.transform;
                Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
                Type colliderType = confiner.m_BoundingVolume.GetType();
                if (colliderType == typeof(BoxCollider))
                {
                    BoxCollider c = confiner.m_BoundingVolume as BoxCollider;
                    Gizmos.DrawWireCube(c.center, c.size);
                }
                else if (colliderType == typeof(SphereCollider))
                {
                    SphereCollider c = confiner.m_BoundingVolume as SphereCollider;
                    Gizmos.DrawWireSphere(c.center, c.radius);
                }
                else if (colliderType == typeof(CapsuleCollider))
                {
                    CapsuleCollider c = confiner.m_BoundingVolume as CapsuleCollider;
                    Vector3 size = Vector3.one * c.radius * 2;
                    switch (c.direction)
                    {
                        case 0: size.x = c.height; break;
                        case 1: size.y = c.height; break;
                        case 2: size.z = c.height; break;
                    }
                    Gizmos.DrawWireCube(c.center, size);
                }
                else if (colliderType == typeof(MeshCollider))
                {
                    MeshCollider c = confiner.m_BoundingVolume as MeshCollider;
                    Gizmos.DrawWireMesh(c.sharedMesh);
                }
                else
                {
                    // Just draw an AABB - not very nice!
                    Gizmos.matrix = oldMatrix;
                    Bounds bounds = confiner.m_BoundingVolume.bounds;
                    Gizmos.DrawWireCube(t.position, bounds.extents * 2);
                }
                Gizmos.color = oldColor;
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}
