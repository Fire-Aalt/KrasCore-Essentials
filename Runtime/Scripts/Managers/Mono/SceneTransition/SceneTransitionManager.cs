using Cysharp.Threading.Tasks;
using System.Threading;
using ArtificeToolkit.Attributes;
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

        [Title("Transition Params")]
        [SerializeField] protected float initializeSceneGroupDuration = 1f;
        [SerializeField] protected float transitionDuration = 0.5f;

        [Title("Loading Bar")]
        [SerializeField] protected bool loadingBar;
        [SerializeField, EnableIf(nameof(loadingBar), true)] protected Image loadingBarImage;
        [SerializeField, EnableIf(nameof(loadingBar), true)] protected float fillSpeed = 5f;

        protected float targetProgress;
        private VisualElement _blackPanel;

        protected virtual void Awake()
        {
            _blackPanel = loadingDocument.rootVisualElement.Q<VisualElement>("BlackPanel");
            
            SetBlackPanelActive(true);
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
            await LerpBlackPanelOpacity(transitionDuration, true, token);
        }

        public virtual async UniTask TransitionOut(CancellationToken token)
        {
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
