using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace KrasCore.Essentials
{
    [RequireComponent(typeof(SceneTransitionManager))]
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Title("Scenes")]
        [SerializeField] protected ScenesDataSO scenesData;
        [Title("Debug")]
        [SerializeField] protected int sceneLoadDelay;
        [SerializeField] protected bool debugLogs;

        public SceneTransitionManager SceneTransitionManager { get; private set; }
        public readonly SceneGroupManager SceneGroupManager = new();

        public static Action OnSceneGroupLoadStart;
        public static Action OnSceneGroupLoadEnd;
        public static bool IsTransitioning { get; private set; }
        public static bool IsLoading { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            SceneTransitionManager = GetComponent<SceneTransitionManager>();

            if (debugLogs)
            {
                SceneGroupManager.OnSceneInfo += GameEssentialsDebug.Log;
                SceneGroupManager.OnSceneLoaded += sceneName => GameEssentialsDebug.Log("Loaded: " + sceneName);
                SceneGroupManager.OnSceneUnloaded += sceneName => GameEssentialsDebug.Log("Unloaded: " + sceneName);
                SceneGroupManager.OnSceneGroupLoaded += () => GameEssentialsDebug.Log($"Scene group '{SceneGroupManager.ActiveSceneGroup.GroupName}' loaded");
            }

            IsTransitioning = false;
            IsLoading = false;
        }

        public virtual async UniTask LoadSceneGroup(LoadSceneGroupParams loadParams)
        {
            if (!IsIndexValid(loadParams.SceneGroupIndex) || IsTransitioning) return;

            // Save -> TransitionIn -> FreeAllLoopingSounds
            IsTransitioning = true;
            EventBus<SaveGameEvent>.Raise(new SaveGameEvent());
            var loadingProgress = SceneTransitionManager.InitializeProgressBar();
            if (loadParams.Transition == SceneTransition.TransitionInAndOut)
            {
                await SceneTransitionManager.TransitionIn(destroyCancellationToken);
            }
            
            OnSceneGroupLoadStart?.Invoke();

            // Enable camera -> LoadScenes -> Disable camera
            IsLoading = true;
            SceneTransitionManager.EnableLoadingCamera(true);
            await SceneGroupManager.LoadScenes(scenesData.sceneGroups[loadParams.SceneGroupIndex], loadingProgress, loadParams.ReloadDupScenes, loadParams.RestartEntitiesWorld, sceneLoadDelay, destroyCancellationToken);
            SceneTransitionManager.EnableLoadingCamera(false);
            IsLoading = false;

            OnSceneGroupLoadEnd?.Invoke();
            
            // TransitionOut
            if (loadParams.Transition == SceneTransition.TransitionOut || loadParams.Transition == SceneTransition.TransitionInAndOut)
            {
                await SceneTransitionManager.TransitionOut(destroyCancellationToken);
            }
            IsTransitioning = false;
        }

        protected bool IsIndexValid(int index)
        {
            if (index < 0 || index >= scenesData.sceneGroups.Length)
            {
                Debug.LogError("Invalid scene group index: " + index);
                return false;
            }
            return true;
        }
    }

    public struct LoadSceneGroupParams
    {
        public int SceneGroupIndex;
        public SceneTransition Transition;
        public bool ReloadDupScenes;
        public bool RestartEntitiesWorld;
    }
    
    public enum SceneTransition
    {
        NoTransition,
        TransitionOut,
        TransitionInAndOut
    }

    public struct SaveGameEvent : IEvent { }
    
    public class LoadingProgress : IProgress<float>
    {
        public event Action<float> Progressed;

        const float ratio = 1f;

        public void Report(float value)
        {
            Progressed?.Invoke(value / ratio);
        }
    }
}
