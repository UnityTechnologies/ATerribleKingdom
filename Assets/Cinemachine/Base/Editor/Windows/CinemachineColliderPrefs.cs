using System;
using UnityEngine;
using UnityEditor;

using Cinemachine.Editor;

namespace Cinemachine.GameRig
{
    [InitializeOnLoad]
    internal static class CinemachineColliderPrefs
    {
        private static bool SettingsFoldedOut
        {
            get { return EditorPrefs.GetBool(kColliderSettingsFoldoutKey, false); }
            set
            {
                if (value != SettingsFoldedOut)
                {
                    EditorPrefs.SetBool(kColliderSettingsFoldoutKey, value);
                }
            }
        }

        public static Color FeelerActiveColor
        {
            get
            {
                return CinemachineSettings.UnpackColour(EditorPrefs.GetString(kFeelerActiveColourKey, CinemachineSettings.PackColor(Color.white)));
            }

            set
            {
                if (value != FeelerActiveColor)
                {
                    EditorPrefs.SetString(kFeelerActiveColourKey, CinemachineSettings.PackColor(value));
                }
            }
        }

        public static Color FeelerInactiveColor
        {
            get
            {
                return CinemachineSettings.UnpackColour(EditorPrefs.GetString(kFeelerInactiveColourKey, CinemachineSettings.PackColor(Color.gray)));
            }

            set
            {
                if (value != FeelerInactiveColor)
                {
                    EditorPrefs.SetString(kFeelerInactiveColourKey, CinemachineSettings.PackColor(value));
                }
            }
        }

        public static Color FeelerHitColor
        {
            get
            {
                return CinemachineSettings.UnpackColour(EditorPrefs.GetString(kFeelerHitColourKey, CinemachineSettings.PackColor(Color.red)));
            }

            set
            {
                if (value != FeelerHitColor)
                {
                    EditorPrefs.SetString(kFeelerHitColourKey, CinemachineSettings.PackColor(value));
                }
            }
        }

        private const string kColliderSettingsFoldoutKey = "CNMCN_Collider_Foldout";
        private const string kFeelerActiveColourKey     = "CNMCN_Collider_Feeler_Active_Colour";
        private const string kFeelerInactiveColourKey   = "CNMCN_Collider_Feeler_Inactive_Colour";
        private const string kFeelerHitColourKey        = "CNMCN_Collider_Feeler_Hit_Colour";

        static CinemachineColliderPrefs()
        {
            Cinemachine.Editor.CinemachineSettings.AdditionalCategories += DrawColliderSettings;
        }

        private static void DrawColliderSettings()
        {
            SettingsFoldedOut = EditorGUILayout.Foldout(SettingsFoldedOut, "Collider Settings");
            if (SettingsFoldedOut)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                FeelerActiveColor   = EditorGUILayout.ColorField("Feeler Active", FeelerActiveColor);
                FeelerInactiveColor = EditorGUILayout.ColorField("Feeler Inactive", FeelerInactiveColor);
                FeelerHitColor      = EditorGUILayout.ColorField("Feeler Hit", FeelerHitColor);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
