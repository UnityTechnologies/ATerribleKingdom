using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using System.Reflection;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Base class for virtual camera editors.
    /// Handles drawing the header and the basic properties.
    /// </summary>
    public class CinemachineVirtualCameraBaseEditor : UnityEditor.Editor
    {
        private CinemachineVirtualCameraBase Target { get { return target as CinemachineVirtualCameraBase; } }

        protected virtual List<string> GetExcludedPropertiesInInspector()
        {
            return Target.m_ExcludedPropertiesInInspector == null
                ? new List<string>() : new List<string>(Target.m_ExcludedPropertiesInInspector);
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            if (CinemachineBrain.SoloCamera == (ICinemachineCamera)Target)
            {
                CinemachineBrain.SoloCamera = null;
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        public override void OnInspectorGUI()
        {
            if (!Target.m_HideHeaderInInspector)
            {
                // Active status and Solo button
                Rect rect = EditorGUILayout.GetControlRect(true);
                Rect rectLabel = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                rect.width -= rectLabel.width;
                rect.x += rectLabel.width;

                Color color = GUI.color;
                bool isSolo = (CinemachineBrain.SoloCamera == (ICinemachineCamera)Target);
                if (isSolo)
                    GUI.color = CinemachineBrain.GetSoloGUIColor();

                bool isLive = CinemachineCore.Instance.IsLive(Target);
                GUI.enabled = isLive;
                GUI.Label(rectLabel, isLive ? "Status: Live"
                    : (Target.isActiveAndEnabled ? "Status: Standby" : "Status: Disabled"));
                GUI.enabled = true;
                if (GUI.Button(rect, "Solo", "Button"))
                {
                    isSolo = !isSolo;
                    CinemachineBrain.SoloCamera = isSolo ? Target : null;
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
                GUI.color = color;
                if (isSolo)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            List<string> excluded = GetExcludedPropertiesInInspector();
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, excluded.ToArray());
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineVirtualCameraBase))]
        internal static void DrawVirtualCameraBaseGizmos(CinemachineVirtualCameraBase vcam, GizmoType selectionType)
        {
            // Don't draw gizmos on hidden stuff
            if ((vcam.VirtualCameraGameObject.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
                return;

            CameraState state = vcam.State;
            Gizmos.DrawIcon(state.FinalPosition, "Cinemachine/cm_logo_lg.png", true);

            CinemachineBrainEditor.DrawCameraFrustumGizmo(
                CinemachineCore.Instance.FindPotentialTargetBrain(vcam),
                state.Lens,
                Matrix4x4.TRS(
                    state.FinalPosition,
                    UnityQuaternionExtensions.Normalized(state.FinalOrientation), Vector3.one),
                CinemachineCore.Instance.IsLive(vcam)
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour);
        }
    }


    [CustomEditor(typeof(CinemachineVirtualCamera))]
    internal sealed class CinemachineVirtualCameraEditor : CinemachineVirtualCameraBaseEditor
    {
        private CinemachineVirtualCamera Target { get { return target as CinemachineVirtualCamera; } }

        // Static state and caches - Call UpdateStaticData() to refresh this
        struct StageData
        {
            public bool isExpanded;
            public Type[] types;   // first entry is null
            public string[] PopupOptions;
        }
        static StageData[] sStageData = null;

        // Instance data - call UpdateInstanceData() to refresh this
        int[] m_stageState = null;
        ICinemachineComponent[] m_components;
        UnityEditor.Editor[] m_componentEditors;

        protected override void OnEnable()
        {
            // Build static menu arrays via reflection
            base.OnEnable();
            UpdateStaticData();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Must destroy editors or we get exceptions
            if (m_componentEditors != null)
                foreach (UnityEditor.Editor e in m_componentEditors)
                    if (e != null)
                        UnityEngine.Object.DestroyImmediate(e);
        }

        private void OnSceneGUI()
        {
            if (Selection.Contains(Target.gameObject) && Tools.current == Tool.Move
                && Event.current.type == EventType.MouseDrag)
            {
                // User might be dragging our position handle
                Target.SuppressOrientationUpdate = true;
            }
            else if (GUIUtility.hotControl == 0 && Target.SuppressOrientationUpdate)
            {
                // We're not dragging anything now, but we were
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                Target.SuppressOrientationUpdate = false;
            }
        }

        public override void OnInspectorGUI()
        {
            // Ordinary properties
            base.OnInspectorGUI();

            // Pipeline - call this first
            UpdateInstanceData();

            // Here are the pipeline stages
            CinemachineCore.Stage[] sections = new CinemachineCore.Stage[]
            {
                CinemachineCore.Stage.Lens,
                CinemachineCore.Stage.Aim,
                CinemachineCore.Stage.Body,
                CinemachineCore.Stage.Noise
            };
            for (int i = 0; i < sections.Length; ++i)
            {
                CinemachineCore.Stage stage = sections[i];
                int index = (int)stage;

                // Skip pipeline stages that have no implementations
                if (sStageData[index].PopupOptions.Length <= 1)
                    continue;

                GUIStyle stageBoxStyle = GUI.skin.box;
                stageBoxStyle.margin.left = 16;
                EditorGUILayout.BeginVertical(stageBoxStyle);

                Rect rect = EditorGUILayout.GetControlRect(true);
                rect.height = EditorGUIUtility.singleLineHeight;

                GUI.enabled = !StageIsLocked(stage);
                int newSelection = EditorGUI.Popup(rect,
                        NicifyName(stage.ToString()), m_stageState[index],
                        sStageData[index].PopupOptions);
                GUI.enabled = true;
                Type type = sStageData[index].types[newSelection];
                if (newSelection != m_stageState[index])
                {
                    SetPipelineStage(stage, type);
                    if (newSelection != 0)
                        sStageData[index].isExpanded = true;
                    UpdateInstanceData(); // because we changed it
                    return;
                }
                if (type != null)
                {
                    int indentOffset = 6;
                    Rect stageRect = new Rect(
                        rect.x - indentOffset, rect.y, rect.width + indentOffset, rect.height);
                    sStageData[index].isExpanded = EditorGUI.Foldout(
                            stageRect, sStageData[index].isExpanded, GUIContent.none);
                    if (sStageData[index].isExpanded)
                    {
                        // Make the editor for that stage
                        UnityEditor.Editor e = GetEditorForPipelineStage(stage);
                        if (e != null)
                        {
                            ++EditorGUI.indentLevel;
                            EditorGUILayout.Separator();
                            e.OnInspectorGUI();
                            EditorGUILayout.Separator();
                            --EditorGUI.indentLevel;
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        bool StageIsLocked(CinemachineCore.Stage stage)
        {
            CinemachineCore.Stage[] locked = Target.m_LockStageInInspector;
            if (locked != null)
                for (int i = 0; i < locked.Length; ++i)
                    if (locked[i] == stage)
                        return true;
            return false;
        }

        UnityEditor.Editor GetEditorForPipelineStage(CinemachineCore.Stage stage)
        {
            foreach (UnityEditor.Editor e in m_componentEditors)
            {
                if (e != null)
                {
                    ICinemachineComponent c = e.target as ICinemachineComponent;
                    if (c != null && c.Stage == stage)
                        return e;
                }
            }
            return null;
        }

        /// <summary>
        /// Register with CinemachineVirtualCamera to create the pipeline in an undo-friendly manner
        /// </summary>
        [InitializeOnLoad]
        class CreatePipelineWithUndo
        {
            static CreatePipelineWithUndo()
            {
                CinemachineVirtualCamera.CreatePipelineOverride =
                    (CinemachineVirtualCamera vcam, string name, ICinemachineComponent[] copyFrom) =>
                    {
                        // Delete all existing pipeline childen
                        List<Transform> list = new List<Transform>();
                        foreach (Transform child in vcam.transform)
                            if (child.GetComponent<CinemachinePipeline>() != null || child.gameObject.name == name)
                                list.Add(child);
                        foreach (Transform child in list)
                            Undo.DestroyObjectImmediate(child.gameObject);

                        // Create a new pipeline
                        GameObject go =  new GameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created pipeline");
                        Undo.SetTransformParent(go.transform, vcam.transform, "parenting pipeline");
                        Undo.AddComponent<CinemachinePipeline>(go);

                        // If copying, transfer the components
                        if (copyFrom != null)
                        {
                            foreach (Component c in copyFrom)
                            {
                                Component copy = Undo.AddComponent(go, c.GetType());
                                Undo.RecordObject(copy, "copying pipeline");
                                ReflectionHelpers.CopyFields(c, copy);
                            }
                        }
                        return go.transform;
                    };
                CinemachineVirtualCamera.DestroyPipelineOverride = (GameObject pipeline) =>
                    {
                        Undo.DestroyObjectImmediate(pipeline);
                    };
            }
        }

        void SetPipelineStage(CinemachineCore.Stage stage, Type type)
        {
            Undo.SetCurrentGroupName("Cinemachine pipeline change");

            // Get the existing components
            Transform owner = Target.GetComponentOwner();

            ICinemachineComponent[] components = owner.GetComponents<ICinemachineComponent>();
            if (components == null)
                components = new ICinemachineComponent[0];

            // Find an appropriate insertion point
            int numComponents = components.Length;
            int insertPoint = 0;
            for (insertPoint = 0; insertPoint < numComponents; ++insertPoint)
                if (components[insertPoint].Stage >= stage)
                    break;

            // Remove the existing components at that stage
            for (int i = numComponents - 1; i >= 0; --i)
            {
                if (components[i].Stage == stage)
                {
                    Undo.DestroyObjectImmediate(components[i] as MonoBehaviour);
                    components[i] = null;
                    --numComponents;
                    if (i < insertPoint)
                        --insertPoint;
                }
            }

            // Add the new stage
            if (type != null)
            {
                MonoBehaviour b = Undo.AddComponent(owner.gameObject, type) as MonoBehaviour;
                while (numComponents-- > insertPoint)
                    UnityEditorInternal.ComponentUtility.MoveComponentDown(b);
            }
        }

        // This code dynamically discovers eligible classes and builds the menu
        // data for the various component pipeline stages.
        void UpdateStaticData()
        {
            if (sStageData != null)
                return;
            sStageData = new StageData[System.Enum.GetValues(typeof(CinemachineCore.Stage)).Length];

            var stageTypes = new List<Type>[System.Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            for (int i = 0; i < stageTypes.Length; ++i)
                stageTypes[i] = new List<Type>();

            // Get all ICinemachineComponents
            var allTypes
                = Cinemachine.Utility.ReflectionHelpers.GetTypesInAllLoadedAssemblies(
                        (Type t) => Array.Exists(t.GetInterfaces(),
                            (i) => i == typeof(ICinemachineComponent)));

            // Create a temp game object so we can instance behaviours
            GameObject go = new GameObject("Cinemachine Temp Object");
            go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            foreach (Type t in allTypes)
            {
                MonoBehaviour b = go.AddComponent(t) as MonoBehaviour;
                ICinemachineComponent c = b != null ? (ICinemachineComponent)b : null;
                if (c != null)
                {
                    CinemachineCore.Stage stage = c.Stage;
                    stageTypes[(int)stage].Add(t);
                }
            }
            GameObject.DestroyImmediate(go);

            // Create the static lists
            for (int i = 0; i < stageTypes.Length; ++i)
            {
                stageTypes[i].Insert(0, null);  // first item is "none"
                sStageData[i].types = stageTypes[i].ToArray();
                string[] names = new string[sStageData[i].types.Length];
                for (int n = 0; n < names.Length; ++n)
                {
                    if (n == 0)
                    {
                        bool useSimple
                            = (i == (int)CinemachineCore.Stage.Aim)
                                || (i == (int)CinemachineCore.Stage.Body);
                        names[n] = (useSimple) ? "Hard constraint" : "none";
                    }
                    else
                        names[n] = NicifyName(sStageData[i].types[n].Name);
                }
                sStageData[i].PopupOptions = names;
            }
        }

        string NicifyName(string name)
        {
            if (name.StartsWith("Cinemachine"))
                name = name.Substring(11); // Trim the prefix
            return ObjectNames.NicifyVariableName(name);
        }

        void UpdateInstanceData()
        {
            // Invalidate the target's cache - this is to support Undo
            Target.InvalidateComponentPipeline();
            UpdateComponentEditors();
            UpdateStageState(m_components);
        }

        // This code dynamically builds editors for the pipeline components.
        // Expansion state is cached statically to preserve foldout state.
        void UpdateComponentEditors()
        {
            ICinemachineComponent[] components = Target.GetComponentPipeline();
            int numComponents = components != null ? components.Length : 0;
            if (m_components == null || m_components.Length != numComponents)
                m_components = new ICinemachineComponent[numComponents];
            bool dirty = (numComponents == 0);
            for (int i = 0; i < numComponents; ++i)
            {
                if (components[i] != m_components[i])
                {
                    dirty = true;
                    m_components[i] = components[i];
                }
            }
            if (dirty)
            {
                // Destroy the subeditors
                if (m_componentEditors != null)
                    foreach (UnityEditor.Editor e in m_componentEditors)
                        if (e != null)
                            UnityEngine.Object.DestroyImmediate(e);

                // Create new editors
                m_componentEditors = new UnityEditor.Editor[numComponents];
                for (int i = 0; i < numComponents; ++i)
                {
                    MonoBehaviour b = components[i] as MonoBehaviour;
                    if (b != null)
                        CreateCachedEditor(b, null, ref m_componentEditors[i]);
                }
            }
        }

        void UpdateStageState(ICinemachineComponent[] components)
        {
            if (m_stageState == null)
                m_stageState = new int[System.Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            for (int i = 0; i < m_stageState.Length; ++i)
                m_stageState[i] = 0;
            foreach (var c in components)
            {
                CinemachineCore.Stage stage = c.Stage;
                int index = 0;
                for (index = sStageData[(int)stage].types.Length - 1; index > 0; --index)
                    if (sStageData[(int)stage].types[index] == c.GetType())
                        break;
                m_stageState[(int)stage] = index;
            }
        }

        // Because the cinemachine components are attached to hidden objects, their
        // gizmos don't get drawn by default.  We have to do it explicitly.
        [InitializeOnLoad]
        static class CollectGizmoDrawers
        {
            static CollectGizmoDrawers()
            {
                m_GizmoDrawers = new Dictionary<Type, MethodInfo>();
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        bool added = false;
                        foreach (var method in type.GetMethods(
                                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                        {
                            if (added)
                                break;
                            if (!method.IsStatic)
                                continue;
                            var attributes = method.GetCustomAttributes(typeof(DrawGizmo), true) as DrawGizmo[];
                            foreach (var a in attributes)
                            {
                                if (typeof(ICinemachineComponent).IsAssignableFrom(a.drawnType))
                                {
                                    m_GizmoDrawers.Add(a.drawnType, method);
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            public static Dictionary<Type, MethodInfo> m_GizmoDrawers;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineVirtualCamera))]
        internal static void DrawVirtualCameraGizmos(CinemachineVirtualCamera vcam, GizmoType selectionType)
        {
            var pipeline = vcam.GetComponentPipeline();
            foreach (var c in pipeline)
            {
                MethodInfo method;
                if (CollectGizmoDrawers.m_GizmoDrawers.TryGetValue(c.GetType(), out method))
                    method.Invoke(null, new object[] { c, selectionType });
            }
        }
    }
}
