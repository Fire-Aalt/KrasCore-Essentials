#if UNITY_ENTITIES && UNITY_EDITOR
using System.Collections.Generic;
using Unity.Scenes;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace KrasCore.Essentials.Editor
{
            
    [InitializeOnLoad]
    public static class EditorSubSceneUtils
    {
        static EditorSubSceneUtils()
        {
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorApplication.delayCall += HandleStartup;
        }

        private static void HandleStartup()
        {
            if (!GameEssentialsSettingsProvider.OpenSubScenesEnabled) return;
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                OpenSubScenes(SceneManager.GetSceneAt(i));
            }
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (!GameEssentialsSettingsProvider.OpenSubScenesEnabled || !scene.isLoaded) return;
            
            OpenSubScenes(scene);
        }
        
        public static void OpenSubScenes(Scene scene)
        {
            foreach (var subScene in GetSubScenes(scene))
            {
                var subSceneScene = EditorSceneManager.OpenScene(subScene.EditableScenePath, OpenSceneMode.Additive);
                SubSceneInspectorUtility.SetSceneAsSubScene(subSceneScene);
            }
        }

        public static List<SubScene> GetSubScenes(Scene scene)
        {
            var result = new List<SubScene>();
            
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                result.AddRange(root.GetComponentsInChildren<SubScene>());
            }
            return result;
        }
    }
}
#endif
