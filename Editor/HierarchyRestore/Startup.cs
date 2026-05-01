#if UNITY_EDITOR
using KrasCore.Essentials;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HierarchyRestore
{
    [InitializeOnLoad]
    public class Startup
    {
        static Startup()
        {
            if (!GameEssentialsSettingsProvider.RestoreHierarchyEnabled)
            {
                return;
            }

            EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
            EditorSceneManager.sceneClosing += OnEditorSceneClosing;
            EditorApplication.quitting += () =>
            {
                HierarchyRestorer.SaveActiveHierarchy(SceneManager.GetActiveScene());
            };
            EditorApplication.delayCall += () =>
            {
                HierarchyRestorer.LoadActiveHierarchy(SceneManager.GetActiveScene());
            };
        }

        private static void OnEditorSceneManagerSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!GameEssentialsSettingsProvider.RestoreHierarchyEnabled || !scene.isLoaded)
            {
                return;
            }

            HierarchyRestorer.LoadActiveHierarchy(scene);
        }

        private static void OnEditorSceneClosing(Scene scene, bool removingScene)
        {
            if (!GameEssentialsSettingsProvider.RestoreHierarchyEnabled)
            {
                return;
            }

            HierarchyRestorer.SaveActiveHierarchy(scene);
        }
    }
}
#endif