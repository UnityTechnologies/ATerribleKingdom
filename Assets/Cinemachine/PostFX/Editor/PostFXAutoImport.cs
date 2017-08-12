using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

namespace Cinemachine.PostFX
{
    class PostFXAutoImport : ScriptableObject {}

    [InitializeOnLoad]
    class AutoExtractPostFX
    {
        static AutoExtractPostFX()
        {
            bool havePostProcessing
                = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                   from type in assembly.GetTypes()
                   where type.Name == "PostProcessingProfile"
                   select type).Count() > 0;
            if (havePostProcessing)
            {
                string path = GetScriptPath();
                pkgFile = path + "/CinemachinePostFX.unityPackage";
                scriptFile = path + "/../CinemachinePostFX.cs";
                if (File.Exists(pkgFile) && (!File.Exists(scriptFile)
                                             || File.GetLastWriteTime(pkgFile) > File.GetLastWriteTime(scriptFile)))
                {
                    Debug.Log("PostProcessing asset detected - importing CinemachinePostFX");
                    AssetDatabase.importPackageCompleted += AssetDatabase_importPackageCompleted;
                    AssetDatabase.importPackageFailed += AssetDatabase_importPackageFailed;
                    AssetDatabase.importPackageCancelled += RemovePackageImportCallbacks;
                    AssetDatabase.ImportPackage(pkgFile, false);
                }
            }
        }

        static string pkgFile;
        static string scriptFile;
        private static void AssetDatabase_importPackageCompleted(string packageName)
        {
            if (packageName == "CinemachinePostFX")
            {
                File.SetLastWriteTime(scriptFile, File.GetLastWriteTime(pkgFile));
                RemovePackageImportCallbacks(packageName);
            }
        }

        private static void AssetDatabase_importPackageFailed(string packageName, string errorMessage)
        {
            if (packageName == "CinemachinePostFX")
            {
                Debug.LogError("Failed to import " + packageName + ": " + errorMessage);
                RemovePackageImportCallbacks(packageName);
            }
        }

        private static void RemovePackageImportCallbacks(string packageName)
        {
            AssetDatabase.importPackageCompleted -= AssetDatabase_importPackageCompleted;
            AssetDatabase.importPackageCancelled -= RemovePackageImportCallbacks;
            AssetDatabase.importPackageFailed -= AssetDatabase_importPackageFailed;
        }

        static string GetScriptPath()
        {
            ScriptableObject dummy = ScriptableObject.CreateInstance<PostFXAutoImport>();
            string path = Application.dataPath + AssetDatabase.GetAssetPath(
                    MonoScript.FromScriptableObject(dummy)).Substring("Assets".Length);
            return path.Substring(0, path.LastIndexOf('/'));
        }
    }
}
