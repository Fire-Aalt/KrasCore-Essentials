using UnityEngine;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;

namespace KrasCore.Essentials
{
    public static class BootLoader
    {
        private static ScenesDataSO scenesData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void OnAfterSceneLoad()
        {
            scenesData = ScenesDataSO.Instance;

            if (scenesData == null)
            {
                Debug.LogError($"{typeof(ScenesDataSO)} was not found." +
                    $" Please, ensure you have a ScriptableObject.asset called 'ScenesData' in the root of any folder called 'Resources'" +
                    $" for the {typeof(BootLoader)} to work properly");
                return;
            }

            if (scenesData.simulateBuild)
            {
                Debug.LogWarning("Simulate Build mode is ON. It will affect loading performance");
            }
            
            int firstSceneGroupIndex;
            SceneReference bootLoaderScene = scenesData.bootLoaderScene;
            
            if (bootLoaderScene.LoadedScene.IsValid())
            {
                firstSceneGroupIndex = GetFirstSceneGroupIndex(scenesData.sceneGroups);
                if (firstSceneGroupIndex == -1)
                {
                    Debug.LogError($"No MainMenu Scene was found in {scenesData}." +
                        $" Please, ensure you have at least one SceneDependency with type of {SceneType.MainMenu}");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("Unknown dependencies found. BootLoader is inactive");
                return;
            }
            
            await UniTask.WaitUntil(() => SceneLoader.TryGetInstance());
            await SceneLoader.Instance.LoadSceneGroup(new LoadSceneGroupParams
                    { SceneGroupIndex = firstSceneGroupIndex, Transition = SceneTransition.TransitionOut })
                .SuppressCancellationThrow();
        }

        private static int GetFirstSceneGroupIndex(SceneGroup[] sceneGroups)
        {
            int firstSceneGroupIndex = -1;

#if UNITY_EDITOR
            firstSceneGroupIndex = EditorScenesDataWrapper.GetFirstSceneGroupIndex();
#endif
            if (firstSceneGroupIndex == -1 && sceneGroups.Length > 0)
            {
                firstSceneGroupIndex = 0;
            }

            return firstSceneGroupIndex;
        }
    }
}
