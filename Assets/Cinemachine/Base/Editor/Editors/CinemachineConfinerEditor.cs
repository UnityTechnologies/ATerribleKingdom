using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineConfiner))]
    public sealed class CinemachineConfinerEditor : BaseEditor<CinemachineConfiner>
    {
        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target.VirtualCamera);
            bool ortho = brain != null ? brain.OutputCamera.orthographic : false;
            if (!ortho)
                excluded.Add(FieldPath(x => x.m_ConfineScreenEdges));
            return excluded;
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (Target.m_BoundingVolume == null)
                EditorGUILayout.HelpBox("A Bounding Volume is required.", MessageType.Warning);
            else if (Target.m_BoundingVolume.GetType() != typeof(BoxCollider)
                && Target.m_BoundingVolume.GetType() != typeof(SphereCollider)
                && Target.m_BoundingVolume.GetType() != typeof(CapsuleCollider))
            {
                EditorGUILayout.HelpBox(
                    "Must be a BoxCollider, SphereCollider, or CapsuleCollider.", 
                    MessageType.Warning);
            }
            DrawRemainingPropertiesInInspector();
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
