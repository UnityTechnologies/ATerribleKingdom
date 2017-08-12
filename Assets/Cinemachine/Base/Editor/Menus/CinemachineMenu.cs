using UnityEngine;
using UnityEditor;
using System;

namespace Cinemachine.Editor
{
    public static class CinemachineMenu
    {
        public const string kCinemachineRootMenu = "Assets/Create/Cinemachine/";
        [MenuItem(kCinemachineRootMenu + "Blender/Settings")]
        private static void CreateBlenderSettingAsset()
        {
            ScriptableObjectUtility.Create<CinemachineBlenderSettings>();
        }

        [MenuItem(kCinemachineRootMenu + "Noise/Settings")]
        private static void CreateNoiseSettingAsset()
        {
            ScriptableObjectUtility.Create<NoiseSettings>();
        }

        /// <summary>
        /// Create a default Virtual Camera, with standard components
        /// </summary>
        [MenuItem("Cinemachine/Create Virtual Camera", false, 1)]
        public static CinemachineVirtualCamera CreateDefaultVirtualCamera()
        {
            return CreateVirtualCamera(
                "CM vcam", typeof(CinemachineComposer), typeof(CinemachineTransposer));
        }

        [MenuItem("Cinemachine/Create FreeLook Camera", false, 1)]
        private static void CreateFreeLookCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = new GameObject(
                    GenerateUniqueObjectName(typeof(CinemachineFreeLook), "CM FreeLook"));
            Undo.RegisterCreatedObjectUndo(go, "create FreeLook");
            Undo.AddComponent<CinemachineFreeLook>(go);
        }

        [MenuItem("Cinemachine/Create State-driven Camera", false, 1)]
        private static void CreateStateDivenCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = new GameObject(
                    GenerateUniqueObjectName(typeof(CinemachineStateDrivenCamera), "CM StateDrivenCamera"));
            Undo.RegisterCreatedObjectUndo(go, "create state driven camera");
            Undo.AddComponent<CinemachineStateDrivenCamera>(go);
            // Give it a child
            Undo.SetTransformParent(CreateVirtualCamera(
                    "CM vcam", typeof(CinemachineComposer), typeof(CinemachineTransposer)).transform,
                go.transform, "create state driven camera");
        }

        [MenuItem("Cinemachine/Create ClearShot Virtual Camera", false, 1)]
        private static void CreateClearShotVirtualCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = new GameObject(
                    GenerateUniqueObjectName(typeof(CinemachineClearShot), "CM ClearShot"));
            Undo.RegisterCreatedObjectUndo(go, "create ClearShot camera");
            Undo.AddComponent<CinemachineClearShot>(go);
            // Give it a child
            Undo.SetTransformParent(CreateVirtualCamera(
                    "CM vcam", typeof(CinemachineComposer), typeof(CinemachineTransposer)).transform,
                go.transform, "create ClearShot camera");
        }

        [MenuItem("Cinemachine/Create Dolly Camera with Track", false, 1)]
        private static void CreateDollyCameraWithPath()
        {
            CinemachineVirtualCamera vcam = CreateVirtualCamera(
                    "CM vcam", typeof(CinemachineComposer), typeof(CinemachineTrackedDolly));
            GameObject go = new GameObject(
                    GenerateUniqueObjectName(typeof(CinemachinePath), "DollyTrack"));
            Undo.RegisterCreatedObjectUndo(go, "create track");
            CinemachinePath path = Undo.AddComponent<CinemachinePath>(go);
            vcam.GetCinemachineComponent<CinemachineTrackedDolly>().m_Path = path;
        }

        [MenuItem("Cinemachine/Create Group Target Camera", false, 1)]
        private static void CreateGroupTargetCamera()
        {
            CinemachineVirtualCamera vcam = CreateVirtualCamera(
                    "CM vcam", typeof(CinemachineGroupComposer), typeof(CinemachineTransposer));
            GameObject go = new GameObject(
                    GenerateUniqueObjectName(typeof(CinemachineTargetGroup), "TargetGroup"),
                    typeof(CinemachineTargetGroup));
            Undo.RegisterCreatedObjectUndo(go, "create target group");
            vcam.LookAt = go.transform;
            vcam.Follow = go.transform;
        }

        /// <summary>
        /// Create a Virtual Camera, with components
        /// </summary>
        public static CinemachineVirtualCamera CreateVirtualCamera(
            string name, params Type[] components)
        {
            // Create a new virtual camera
            CreateCameraBrainIfAbsent();
            GameObject go = new GameObject(
                    GenerateUniqueObjectName(typeof(CinemachineVirtualCamera), name));
            Undo.RegisterCreatedObjectUndo(go, "create " + name);
            CinemachineVirtualCamera vcam = Undo.AddComponent<CinemachineVirtualCamera>(go);
            GameObject componentOwner = vcam.GetComponentOwner().gameObject;
            foreach (Type t in components)
                Undo.AddComponent(componentOwner, t);
            vcam.InvalidateComponentPipeline();
            return vcam;
        }

        /// <summary>
        /// If there is no CinemachineBrain in the scene, try to create one on the main camera
        /// </summary>
        public static void CreateCameraBrainIfAbsent()
        {
            CinemachineBrain[] brains = UnityEngine.Object.FindObjectsOfType(
                    typeof(CinemachineBrain)) as CinemachineBrain[];
            if (brains == null || brains.Length == 0)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    Camera[] cams = UnityEngine.Object.FindObjectsOfType(
                            typeof(Camera)) as Camera[];
                    if (cams != null && cams.Length > 0)
                        cam = cams[0];
                }
                if (cam != null)
                {
                    Undo.AddComponent<CinemachineBrain>(cam.gameObject);
                }
            }
        }

        /// <summary>
        /// Generate a unique name with the given prefix by adding a suffix to it
        /// </summary>
        public static string GenerateUniqueObjectName(Type type, string prefix)
        {
            int count = 0;
            UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(type);
            foreach (UnityEngine.Object o in all)
            {
                if (o != null && o.name.StartsWith(prefix))
                {
                    string suffix = o.name.Substring(prefix.Length);
                    int i;
                    if (Int32.TryParse(suffix, out i) && i > count)
                        count = i;
                }
            }
            return prefix + (count + 1);
        }
    }
}
