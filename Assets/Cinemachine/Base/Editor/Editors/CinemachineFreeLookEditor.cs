using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineFreeLook))]
    internal sealed class CinemachineFreeLookEditor : CinemachineVirtualCameraBaseEditor
    {
        private CinemachineFreeLook Target { get { return (CinemachineFreeLook)target; } }

        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            if (!Target.m_UseCommonLensSetting)
                excluded.Add(SerializedPropertyHelper.PropertyName(() => Target.m_Lens));
            return excluded;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Must destroy child editors or we get exceptions
            if (m_editors != null)
                foreach (UnityEditor.Editor e in m_editors)
                    if (e != null)
                        UnityEngine.Object.DestroyImmediate(e);
        }

        public override void OnInspectorGUI()
        {
            // Ordinary properties
            base.OnInspectorGUI();

            // Rigs
            UpdateRigEditors();
            for (int i = 0; i < m_editors.Length; ++i)
            {
                if (m_editors[i] == null)
                    continue;
                EditorGUILayout.Separator();
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(RigNames[i], EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                m_editors[i].OnInspectorGUI();
                --EditorGUI.indentLevel;
                EditorGUILayout.EndVertical();
            }
        }

        string[] RigNames;
        CinemachineVirtualCameraBase[] m_rigs;
        UnityEditor.Editor[] m_editors;
        void UpdateRigEditors()
        {
            RigNames = CinemachineFreeLook.RigNames;
            if (m_rigs == null)
                m_rigs = new CinemachineVirtualCameraBase[RigNames.Length];
            if (m_editors == null)
                m_editors = new UnityEditor.Editor[RigNames.Length];
            for (int i = 0; i < RigNames.Length; ++i)
            {
                CinemachineVirtualCamera rig = Target.GetRig(i);
                if (rig == null || rig != m_rigs[i])
                {
                    m_rigs[i] = rig;
                    if (m_editors[i] != null)
                        UnityEngine.Object.DestroyImmediate(m_editors[i]);
                    m_editors[i] = null;
                    if (rig != null)
                        CreateCachedEditor(rig, null, ref m_editors[i]);
                }
            }
        }

        /// <summary>
        /// Register with CinemachineFreeLook to create the pipeline in an undo-friendly manner
        /// </summary>
        [InitializeOnLoad]
        class CreateRigWithUndo
        {
            static CreateRigWithUndo()
            {
                CinemachineFreeLook.CreateRigOverride
                    = (CinemachineFreeLook vcam, string name, CinemachineVirtualCamera copyFrom) =>
                    {
                        // If there is an existing rig with this name, delete it
                        List<Transform> list = new List<Transform>();
                        foreach (Transform child in vcam.transform)
                            if (child.GetComponent<CinemachineVirtualCamera>() != null
                                && child.gameObject.name == name)
                                list.Add(child);
                        foreach (Transform child in list)
                            Undo.DestroyObjectImmediate(child.gameObject);

                        // Create a new rig with default components
                        GameObject go = new GameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created rig");
                        Undo.SetTransformParent(go.transform, vcam.transform, "parenting rig");
                        CinemachineVirtualCamera rig = Undo.AddComponent<CinemachineVirtualCamera>(go);
                        Undo.RecordObject(rig, "creating rig");
                        if (copyFrom != null)
                            ReflectionHelpers.CopyFields(copyFrom, rig);
                        else
                        {
                            go = rig.GetComponentOwner().gameObject;
                            Undo.RecordObject(Undo.AddComponent<CinemachineOrbitalTransposer>(go), "creating rig");
                            Undo.RecordObject(Undo.AddComponent<CinemachineComposer>(go), "creating rig");
                        }
                        return rig;
                    };
                CinemachineFreeLook.DestroyRigOverride = (GameObject rig) =>
                    {
                        Undo.DestroyObjectImmediate(rig);
                    };
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineFreeLook))]
        private static void DrawFreeLookGizmos(CinemachineFreeLook vcam, GizmoType selectionType)
        {
            // Standard frustum and logo
            CinemachineVirtualCameraBaseEditor.DrawVirtualCameraBaseGizmos(vcam, selectionType);

            Color originalGizmoColour = Gizmos.color;
            bool isActiveVirtualCam = CinemachineCore.Instance.IsLive(vcam);
            Gizmos.color = isActiveVirtualCam
                ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

            if (vcam.Follow != null)
            {
                Vector3 pos = vcam.Follow.position;
                var TopRig = vcam.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>();
                var MiddleRig = vcam.GetRig(1).GetCinemachineComponent<CinemachineOrbitalTransposer>();
                var BottomRig = vcam.GetRig(2).GetCinemachineComponent<CinemachineOrbitalTransposer>();
                DrawCircleAtPointWithRadius(pos + Vector3.up * TopRig.m_HeightOffset, TopRig.m_Radius, vcam);
                DrawCircleAtPointWithRadius(pos + Vector3.up * MiddleRig.m_HeightOffset, MiddleRig.m_Radius, vcam);
                DrawCircleAtPointWithRadius(pos + Vector3.up * BottomRig.m_HeightOffset, BottomRig.m_Radius, vcam);
                DrawCameraPath(pos, vcam);
            }

            Gizmos.color = originalGizmoColour;
        }

        private static void DrawCameraPath(Vector3 atPos, CinemachineFreeLook vcam)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Matrix4x4 localToWorld = Matrix4x4.TRS(
                    atPos, Quaternion.AngleAxis(vcam.m_XAxis.Value, Vector3.up), Vector3.one);
            Gizmos.matrix = localToWorld;

            const int kNumStepsPerPair = 30;
            Vector3 currPos = vcam.GetLocalPositionForCameraFromInput(0f);
            for (int i = 1; i < kNumStepsPerPair + 1; ++i)
            {
                float t = (float)i / (float)kNumStepsPerPair;
                Vector3 nextPos = vcam.GetLocalPositionForCameraFromInput(t);
                Gizmos.DrawLine(currPos, nextPos);
                Gizmos.DrawWireSphere(nextPos, 0.02f);
                currPos = nextPos;
            }
            Gizmos.matrix = prevMatrix;
        }

        private static void DrawCircleAtPointWithRadius(Vector3 point, float radius, CinemachineFreeLook vcam)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(point, Quaternion.identity, radius * Vector3.one);
            Color prevGizmosColour = Gizmos.color;

            const int kNumPoints = 25;
            Vector3 currPoint = Vector3.forward;
            Quaternion rot = Quaternion.AngleAxis(360f / (float)kNumPoints, Vector3.up);
            for (int i = 0; i < kNumPoints + 1; ++i)
            {
                Vector3 nextPoint = rot * currPoint;
                Gizmos.DrawLine(currPoint, nextPoint);
                currPoint = nextPoint;
            }

            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevGizmosColour;
        }
    }
}
