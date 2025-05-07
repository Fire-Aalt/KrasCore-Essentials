using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Eflatun.SceneReference;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditor;

namespace KrasCore.Essentials.Editor
{
    [InitializeOnLoad]
    public static class EditorBootLoader
    {
        static EditorBootLoader()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (ScenesDataSO.Instance == null)
            {
                return;
            }
            
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    SaveScenesDataBeforePlayMode();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    TearDownAfterPlayMode();
                    break;
            }
        }
        
        private static void SaveScenesDataBeforePlayMode()
        {
            ScenesDataSO scenesData = ScenesDataSO.Instance;
            SceneReference bootLoaderScene = scenesData.bootLoaderScene;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // Get SceneGroup
            var openedScenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isSubScene) continue;
                
                openedScenes.Add(scene.path);
            }
            int firstSceneGroupIndex = GetFirstSceneGroupIndex(scenesData.sceneGroups, openedScenes);

            EditorScenesDataWrapper.SetOpenedScenes(openedScenes.ToArray());
            EditorScenesDataWrapper.SetFirstSceneGroupIndex(firstSceneGroupIndex);

            // Force return if unknown SceneGroup
            if (!bootLoaderScene.LoadedScene.IsValid() && firstSceneGroupIndex == -1)
            {
                return;
            }
            
            // Open and move _BootLoader scene as first
            Scene topScene = SceneManager.GetSceneAt(0);
            if (!bootLoaderScene.LoadedScene.IsValid())
            {
                EditorSceneManager.OpenScene(bootLoaderScene.Path, OpenSceneMode.Additive);
                EditorSceneManager.MoveSceneBefore(bootLoaderScene.LoadedScene, topScene);
            }
            else if (topScene != bootLoaderScene.LoadedScene)
            {
                EditorSceneManager.MoveSceneBefore(bootLoaderScene.LoadedScene, topScene);
            }

            // Move Main scene as second
            if (firstSceneGroupIndex != -1)
            {
                SceneReference mainScene = scenesData.sceneGroups[firstSceneGroupIndex].MainScene.Reference;
                EditorSceneManager.MoveSceneAfter(mainScene.LoadedScene, topScene);
            }
            
            if (scenesData.simulateBuild)
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));
                for (int i = SceneManager.sceneCount - 1; i >= 1; i--)
                {
                    EditorSceneManager.CloseScene(SceneManager.GetSceneAt(i), true);
                }
            }
        }

        private static void TearDownAfterPlayMode()
        {
            ScenesDataSO scenesData = ScenesDataSO.Instance;
            SceneReference bootLoaderScene = ScenesDataSO.Instance.bootLoaderScene;

            var openedScenes = EditorScenesDataWrapper.GetOpenedScenes();
            
            if (scenesData.simulateBuild)
            {
                foreach (var openedScene in openedScenes)
                {
                    EditorSceneManager.OpenScene(openedScene, OpenSceneMode.Additive);
                }
            }
            
            // Close _BootLoader scene if it was not opened
            if (string.IsNullOrEmpty(openedScenes.FirstOrDefault(p => p == bootLoaderScene.Path)))
            {
                EditorSceneManager.CloseScene(bootLoaderScene.LoadedScene, true);
            }
            
            EditorScenesDataWrapper.ClearData();
        }

        private static int GetFirstSceneGroupIndex(SceneGroup[] sceneGroups, List<string> openedScenes)
        {
            for (int i = 0; i < openedScenes.Count; i++)
            {
                int sceneGroupIndex = GetSceneGroupIndex(sceneGroups, openedScenes[i]);
                if (sceneGroupIndex != -1)
                {
                    return sceneGroupIndex;
                }
            }
            return -1;
        }

        private static int GetSceneGroupIndex(SceneGroup[] sceneGroups, string scenePath)
        {
            for (int i = 0; i < sceneGroups.Length; i++)
            {
                if (scenePath == sceneGroups[i].MainScene.Reference.Path)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}