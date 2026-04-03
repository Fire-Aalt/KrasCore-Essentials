using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using BovineLabs.Core.Pause;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace KrasCore.Essentials
{
    public class SceneGroupManager
    {
        public event Action<SceneData> OnScenePersisted = delegate { };
        public event Action<string> OnSceneInfo = delegate { };
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

#if ADDRESSABLES
        private readonly AsyncOperationHandleGroup _handleGroup = new(10);
#endif
        
        public SceneGroup ActiveSceneGroup { get; private set; }

        public async UniTask LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes, bool restartEntitiesWorld, int loadDelay, CancellationToken token)
        {
            ActiveSceneGroup = group;
            var loadedScenes = new List<string>();

            // Set _BootLoader as active scene to unload everything else
            var bootLoader = ScenesDataSO.Instance.bootLoaderScene;
            SceneManager.SetActiveScene(bootLoader.LoadedScene);

#if UNITY_ENTITIES
            if (restartEntitiesWorld)
            {
                DisposeEntitiesWorld();
            }
#endif
            
            await UnloadScenes(bootLoader.LoadedScene, reloadDupScenes, token);

#if UNITY_ENTITIES
            if (restartEntitiesWorld)
            {
                StartEntitiesWorld();
            }
            // Freeze systems to ensure all objects are loaded
            var world = World.DefaultGameObjectInjectionWorld;
            var handle = world.GetOrCreateSystem<SceneLoaderPauseSystem>();
            SetEntitiesWorldPauseState(world, handle, true);
#endif

            int sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalScenesToLoad = ActiveSceneGroup.Scenes.Count;
            var operationGroup = new AsyncOperationGroup(totalScenesToLoad);

            for (var i = 0; i < totalScenesToLoad; i++)
            {
                var sceneData = group.Scenes[i];
                if (reloadDupScenes == false && loadedScenes.Contains(sceneData.Name)) continue;
                
                if (sceneData.Reference.State == SceneReferenceState.Regular)
                {
                    var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    operationGroup.Operations.Add(operation);

                    operation.completed += _ => HandleSceneLoaded(sceneData);
                }
#if ADDRESSABLES
                else if (sceneData.Reference.State == SceneReferenceState.Addressable)
                {
                    var sceneHandle = Addressables.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    _handleGroup.Handles.Add(sceneHandle);

                    sceneHandle.Completed += _ => HandleSceneLoaded(sceneData);
                }
#endif
            }
            
            if (loadDelay > 0)
            {
                await UniTask.Delay(loadDelay, cancellationToken: token);
            }

            // Wait until all AsyncOperations in the group are done
#if ADDRESSABLES
            while (!operationGroup.IsDone || !_handleGroup.IsDone)
            {
                progress?.Report((operationGroup.Progress + _handleGroup.Progress) / 2);
                
                await UniTask.Delay(1, true, cancellationToken: token);
            }
#else
            while (!operationGroup.IsDone)
            {
                progress?.Report(operationGroup.Progress);
                
                await UniTask.Delay(1, true, cancellationToken: token);
            }
#endif

#if UNITY_ENTITIES
            // Unfreeze systems
            SetEntitiesWorldPauseState(world, handle, false);
#endif
            OnSceneGroupLoaded.Invoke();
        }

        private void HandleSceneLoaded(SceneData sceneData)
        {
            if (sceneData.Reference.Path == ActiveSceneGroup.MainScene.Reference.Path)
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(ActiveSceneGroup.MainScene.Name));
            }
            OnSceneLoaded.Invoke(sceneData.Reference.Path);
        }

        private async UniTask UnloadScenes(Scene bootloaderScene, bool unloadDupScenes, CancellationToken token)
        {
            var scenesToUnload = new List<Scene>();
#if ADDRESSABLES
            var addressableScenesToUnload = new List<AsyncOperationHandle<SceneInstance>>();
#endif
            int sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                
                OnSceneInfo.Invoke("Scene Info: " + scene.path + " | isLoaded: " + scene.isLoaded + " | isSubScene: " + scene.isSubScene);
                if (!scene.isLoaded || scene == bootloaderScene) continue;

#if UNITY_EDITOR
                if (scene.isSubScene) continue;
#endif
                
                var scenePath = scene.path;
                if (ActiveSceneGroup.IsSceneInGroup(scene) && !unloadDupScenes)
                {
                    var sceneData = ActiveSceneGroup.GetSceneData(scene);
                    HandleSceneLoaded(sceneData);
                    OnScenePersisted.Invoke(sceneData);
                    continue;
                }
                
#if ADDRESSABLES
                var addressableHandle =
                    _handleGroup.Handles.FirstOrDefault(h => h.IsValid() && h.Result.Scene.path == scenePath);

                if (addressableHandle.IsValid())
                {
                    addressableScenesToUnload.Add(addressableHandle);
                }
                else
#endif
                {
                    scenesToUnload.Add(scene);
                }
            }

            var operationGroup = new AsyncOperationGroup(scenesToUnload.Count);

            foreach (var scene in scenesToUnload)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null) continue;
                operationGroup.Operations.Add(operation);
            }

#if ADDRESSABLES
            var operationHandleGroup = new AsyncOperationHandleGroup(addressableScenesToUnload.Count);
            foreach (var handle in addressableScenesToUnload)
            {
                var unloadHandle = Addressables.UnloadSceneAsync(handle);
                if (!unloadHandle.IsValid()) continue;
                operationHandleGroup.Handles.Add(unloadHandle);
            }
#endif

            // Wait until all AsyncOperations in the group are done
            while (!operationGroup.IsDone
#if ADDRESSABLES
                   && !operationHandleGroup.IsDone
#endif
)
            {
                await UniTask.Delay(1, cancellationToken: token);
            }
            
            foreach (var scene in scenesToUnload)
            {
                OnSceneUnloaded.Invoke(scene.path);
            }
#if ADDRESSABLES
            foreach (var handle in addressableScenesToUnload)
            {
                _handleGroup.Handles.Remove(handle);
                OnSceneUnloaded.Invoke(handle.Result.Scene.path);
            }
#endif

            // Optional: UnloadUnusedAssets - unloads all unused assets from memory
            await Resources.UnloadUnusedAssets();
        }
        
#if UNITY_ENTITIES
        private static void DisposeEntitiesWorld()
        {
            World.DefaultGameObjectInjectionWorld.Dispose();
        }
        
        private static void StartEntitiesWorld()
        {
            DefaultWorldInitialization.Initialize("Default World");
            if (!ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(World.DefaultGameObjectInjectionWorld))
            {
                ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(World.DefaultGameObjectInjectionWorld);
            }
        }
        
        private static void SetEntitiesWorldPauseState(World world, SystemHandle handle, bool pause)
        {
            ref var state = ref world.Unmanaged.ResolveSystemStateRef(handle);
            if (pause) PauseGame.Pause(ref state, pauseAll: true);
            else PauseGame.Unpause(ref state);
        }
#endif
    }
    
    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
        public bool IsDone => Operations.All(o => o.isDone);

        public AsyncOperationGroup(int initialCapacity)
        {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }

#if ADDRESSABLES
    public readonly struct AsyncOperationHandleGroup
    {
        public readonly List<AsyncOperationHandle<SceneInstance>> Handles;

        public float Progress => Handles.Count == 0 ? 0 : Handles.Average(h => h.PercentComplete);
        public bool IsDone => Handles.Count == 0 || Handles.All(o => o.IsDone);

        public AsyncOperationHandleGroup(int initialCapacity)
        {
            Handles = new List<AsyncOperationHandle<SceneInstance>>(initialCapacity);
        }
    }
#endif
}
