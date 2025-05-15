using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace KrasCore.Essentials
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] protected UIDocument loadingDocument;
        [SerializeField] protected Camera loadingCamera;

        [Title("MMF Players")]
        [SerializeField] protected MMF_Player transitionInPlayer;
        [SerializeField] protected MMF_Player transitionOutPlayer;

        [Title("Transition Params")]
        [SerializeField] protected float initializeSceneGroupDuration = 1f;
        [SerializeField] protected float transitionDuration = 0.5f;

        [Title("Loading Bar")]
        [SerializeField] protected bool loadingBar;
        [SerializeField, ShowIf("loadingBar")] protected Image loadingBarImage;
        [SerializeField, ShowIf("loadingBar")] protected float fillSpeed = 5f;

        protected float targetProgress;
        private VisualElement _blackPanel;

        protected virtual void Awake()
        {
            _blackPanel = loadingDocument.rootVisualElement.Q<VisualElement>("BlackPanel");
            
            SetBlackPanelActive(true);
            transitionInPlayer.Initialization();
            transitionOutPlayer.Initialization();
        }

        protected virtual void Update()
        {
            if (loadingBar && SceneLoader.IsLoading)
            {
                float currentFillAmount = loadingBarImage.fillAmount;
                float progressDifference = Mathf.Abs(currentFillAmount - targetProgress);

                float dynamicFillSpeed = progressDifference * fillSpeed;

                loadingBarImage.fillAmount = Mathf.Lerp(currentFillAmount, targetProgress, Time.deltaTime * dynamicFillSpeed);
            }
        }

        public virtual LoadingProgress InitializeProgressBar()
        {
            if (loadingBar)
            {
                loadingBarImage.fillAmount = 0f;
            }
            targetProgress = 1f;

            LoadingProgress progress = new();
            progress.Progressed += target => targetProgress = Mathf.Max(target, targetProgress);

            return progress;
        }

        public virtual void EnableLoadingCamera(bool enable)
        {
            loadingCamera.depth = enable ? 100 : -100;
            loadingCamera.enabled = enable;
        }

        public virtual async UniTask TransitionIn(CancellationToken token)
        {
            transitionInPlayer.PlayFeedbacks();

            await LerpBlackPanelOpacity(transitionDuration, true, token);
        }

        public virtual async UniTask TransitionOut(CancellationToken token)
        {
            transitionOutPlayer.PlayFeedbacks();

            await UniTask.WaitForSeconds(initializeSceneGroupDuration, ignoreTimeScale: true, cancellationToken: token);
            await LerpBlackPanelOpacity(transitionDuration, false, token);
        }

        protected virtual async UniTask LerpBlackPanelOpacity(float duration, bool setActive, CancellationToken token)
        {
            float startAlpha = setActive ? 0f : 1f;
            float endAlpha = setActive ? 1f : 0f;

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                _blackPanel.style.opacity = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                await UniTask.Yield(cancellationToken: token);
            }

            SetBlackPanelActive(setActive);
        }

        private void SetBlackPanelActive(bool isActive)
        {
            _blackPanel.style.opacity = isActive ? 1f : 0f;
            _blackPanel.pickingMode = isActive ? PickingMode.Position : PickingMode.Ignore;
        }
    }
}
