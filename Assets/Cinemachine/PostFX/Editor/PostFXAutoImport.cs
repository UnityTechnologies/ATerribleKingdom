using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Cinemachine.PostFX
{
    /// This is needed in order to get the path to this script
    class PostFXAutoImport : ScriptableObject {}

    /// <summary>Test for the existance of a type.  If that type is present and if
    /// a file does not exist in the project or is outdated, import a package</summary>
    internal class AutoImporter
    {
        /// <summary>Perform the async auto-import</summary>
        /// <param name="pPackageName">The package to import - must be in same directory as this file</param>
        /// <param name="pPackageScript">A script in the imported package, used for date/time compare.  
        /// If this script is not present in the project, or is dated earlier
        /// than the package, import the package.  Path must be relative to this file</param>
        public AutoImporter(string packageName, string packageScript)
        {
            string path = GetScriptPath();
            m_pkgFile = path + "/" + packageName + ".unityPackage";
            m_scriptFile = path + packageScript;
            m_packageName = packageName;
            if (File.Exists(m_pkgFile) 
                && (!File.Exists(m_scriptFile)
                    || File.GetLastWriteTime(m_pkgFile) > File.GetLastWriteTime(m_scriptFile)))
            {
                sActiveExtractors.Add(this);
                Debug.Log("Auto-importing " + packageName);
                AssetDatabase.importPackageCompleted += AssetDatabase_importPackageCompleted;
                AssetDatabase.importPackageFailed += AssetDatabase_importPackageFailed;
                AssetDatabase.importPackageCancelled += RemovePackageImportCallbacks;
                AssetDatabase.ImportPackage(m_pkgFile, false);
            }
        }

        /// <summary>Tests for the existence of a type</summary>
        /// <param name="fullname">The full name of the type (including namespaces)</param>
        public static bool TypeIsDefined(string fullname)
        {
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.FullName == fullname
                select type).Count() > 0;
        }

        string m_pkgFile;
        string m_scriptFile;
        string m_packageName;
        private void AssetDatabase_importPackageCompleted(string packageName)
        {
            if (packageName == m_packageName)
            {
                File.SetLastWriteTime(m_scriptFile, File.GetLastWriteTime(m_pkgFile));
                RemovePackageImportCallbacks(packageName);
            }
        }

        private void AssetDatabase_importPackageFailed(string packageName, string errorMessage)
        {
            if (packageName == m_packageName)
            {
                Debug.LogError("Failed to import " + packageName + ": " + errorMessage);
                RemovePackageImportCallbacks(packageName);
            }
        }

        private void RemovePackageImportCallbacks(string packageName)
        {
            AssetDatabase.importPackageCompleted -= AssetDatabase_importPackageCompleted;
            AssetDatabase.importPackageCancelled -= RemovePackageImportCallbacks;
            AssetDatabase.importPackageFailed -= AssetDatabase_importPackageFailed;
            if (sActiveExtractors.Contains(this))
                sActiveExtractors.Remove(this);
        }

        /// Keep the extractors alive until async extraction is completed
        static List<AutoImporter> sActiveExtractors = new List<AutoImporter>();

        /// Get the path of this script
        static string GetScriptPath()
        {
            ScriptableObject dummy = ScriptableObject.CreateInstance<PostFXAutoImport>();
            string path = Application.dataPath + AssetDatabase.GetAssetPath(
                    MonoScript.FromScriptableObject(dummy)).Substring("Assets".Length);
            return path.Substring(0, path.LastIndexOf('/'));
        }
    }

    [InitializeOnLoad]
    class AutoImportPostFX
    {
        static AutoImportPostFX()
        {
            if (AutoImporter.TypeIsDefined("UnityEngine.PostProcess.PostProcessLayer"))
                new AutoImporter("CinemachinePostFXV2", "/../CinemachinePostFX.cs");
            else if (AutoImporter.TypeIsDefined("UnityEngine.PostProcessing.PostProcessingBehaviour"))
                new AutoImporter("CinemachinePostFX", "/../CinemachinePostFX.cs");
        }
    }
}
