#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KrasCore.Essentials.Editor
{
    public class GameEssentialsSettingsProvider : SettingsProvider
    {
        private const string OpenSubScenes = "OpenSubScenes";
        public static bool OpenSubScenesEnabled => EditorPrefs.GetBool(OpenSubScenes, true);
        
        private const string RestoreHierarchy = "RestoreHierarchy";
        public static bool RestoreHierarchyEnabled => EditorPrefs.GetBool(RestoreHierarchy, true);

        private GameEssentialsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            Toggle(OpenSubScenesEnabled, OpenSubScenes, "Open SubScenes When Scene Is Opened");
            Toggle(RestoreHierarchyEnabled, RestoreHierarchy, "Restore Hierarchy After Scene Reopening");
        }

        private static void Toggle(bool toggleProperty, string togglePropertyName, string text)
        {
            var value = EditorGUILayout.ToggleLeft(text, toggleProperty, GUILayout.ExpandWidth(true));
            if (toggleProperty != value)
            {
                EditorPrefs.SetBool(togglePropertyName, value);
            }
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new GameEssentialsSettingsProvider("Tools/Game Essentials", SettingsScope.Project);
        }

        [MenuItem("Tools/Game Essentials Preferences")]
        private static void Open()
        {
            SettingsService.OpenProjectSettings("Tools/Game Essentials");
        }
    }
}
#endif