using JKFrame;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_LoadingWindow), true, nameof(UI_LoadingWindow), 2)]
    public class UI_LoadingWindow : UI_WindowBase
    {
        private const string LoadingSceneProgressEvent = "LoadingSceneProgress";
        private const string LoadSceneSucceedEvent = "LoadSceneSucceed";

        [Header("Progress")]
        [SerializeField] private ProgressBar progressBar;
        [SerializeField, Range(0.5f, 0.99f)] private float progressWhenSceneLoaded = 0.9f;
        [SerializeField, Min(0.1f)] private float loadingProgressSpeed = 0.6f;
        [SerializeField, Min(0.1f)] private float finalizeProgressSpeed = 3f;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite defaultBackground;
        [SerializeField] private string[] backgroundAssetKeys;
        [SerializeField] private bool cropBackgroundToCover = true;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private string loadingStatus = "Loading scene...";
        [SerializeField] private string initializingStatus = "Initializing game...";
        [SerializeField] private string completeStatus = "Complete";
        [SerializeField] private bool autoCloseOnReady = true;

        private bool _waitForSceneReadyEvent;
        private bool _waitingForGameReady;
        private int _backgroundLoadVersion;
        private float _displayProgress;
        private float _targetProgress;

        public override void OnShow()
        {
            base.OnShow();
            BeginLoading();
        }

        public void BeginLoading()
        {
            _waitForSceneReadyEvent = GameManager.Instance != null && GameManager.Instance.WaitForSceneReadyEvent;
            _waitingForGameReady = _waitForSceneReadyEvent;
            _displayProgress = 0f;
            _targetProgress = 0f;
            SetProgress(_displayProgress);
            SetStatus(loadingStatus);
            ApplyRandomBackground();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyBackgroundCoverLayout();
        }

        private void Update()
        {
            if (!UIEnable) return;

            float speed = _waitingForGameReady ? loadingProgressSpeed : finalizeProgressSpeed;
            _displayProgress = Mathf.MoveTowards(_displayProgress, _targetProgress, speed * Time.unscaledDeltaTime);
            SetProgress(_displayProgress);

            if (!_waitingForGameReady && autoCloseOnReady && _displayProgress >= 1f - 0.0001f)
            {
                UISystem.Close<UI_LoadingWindow>();
            }
        }

        protected override void RegisterEventListener()
        {
            base.RegisterEventListener();
            EventSystem.AddEventListener<float>(LoadingSceneProgressEvent, OnLoadingProgress);
            EventSystem.AddEventListener(LoadSceneSucceedEvent, OnSceneLoadSucceed);
            EventSystem.AddEventListener(GameManager.GameSceneReadyEvent, OnGameSceneReady);
        }

        protected override void UnRegisterEventListener()
        {
            base.UnRegisterEventListener();
            EventSystem.RemoveEventListener<float>(LoadingSceneProgressEvent, OnLoadingProgress);
            EventSystem.RemoveEventListener(LoadSceneSucceedEvent, OnSceneLoadSucceed);
            EventSystem.RemoveEventListener(GameManager.GameSceneReadyEvent, OnGameSceneReady);
        }

        private void OnLoadingProgress(float rawProgress)
        {
            if (!UIEnable) return;

            float normalized = Mathf.Clamp01(rawProgress / 0.9f);
            float visualProgress = Mathf.Min(normalized * progressWhenSceneLoaded, progressWhenSceneLoaded);
            _targetProgress = Mathf.Max(_targetProgress, visualProgress);
            SetStatus(loadingStatus);
        }

        private void OnSceneLoadSucceed()
        {
            if (!UIEnable) return;

            if (_waitForSceneReadyEvent)
            {
                _targetProgress = Mathf.Max(_targetProgress, progressWhenSceneLoaded);
                SetStatus(initializingStatus);
                return;
            }

            _waitingForGameReady = false;
            _targetProgress = 1f;
            SetStatus(completeStatus);
        }

        private void OnGameSceneReady()
        {
            if (!UIEnable || !_waitForSceneReadyEvent) return;

            _waitingForGameReady = false;
            _targetProgress = 1f;
            SetStatus(completeStatus);
        }

        private void SetProgress(float value)
        {
            float clamped = Mathf.Clamp01(value);

            if (progressBar != null)
            {
                progressBar.minValue = 0f;
                progressBar.maxValue = 100f;
                progressBar.currentPercent = clamped * 100f;
                progressBar.UpdateUI();

                if (progressBar.textPercent != null)
                {
                    progressBar.textPercent.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
                }
            }
        }

        private void SetStatus(string content)
        {
            if (statusText != null)
            {
                statusText.text = content;
            }
        }

        private void ApplyRandomBackground()
        {
            if (backgroundImage == null)
            {
                return;
            }

            _backgroundLoadVersion++;
            int currentVersion = _backgroundLoadVersion;

            if (defaultBackground != null)
            {
                backgroundImage.sprite = defaultBackground;
            }

            ApplyBackgroundCoverLayout();

            if (backgroundAssetKeys == null || backgroundAssetKeys.Length == 0)
            {
                return;
            }

            int index = Random.Range(0, backgroundAssetKeys.Length);
            string key = backgroundAssetKeys[index];
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            ResSystem.LoadAssetAsync<Sprite>(key, sprite =>
            {
                if (!UIEnable || currentVersion != _backgroundLoadVersion) return;
                if (sprite == null) return;
                backgroundImage.sprite = sprite;
                ApplyBackgroundCoverLayout();
            });
        }

        private void ApplyBackgroundCoverLayout()
        {
            if (backgroundImage == null) return;

            if (!cropBackgroundToCover)
            {
                StretchBackgroundToParent();
                return;
            }

            Sprite sprite = backgroundImage.sprite;
            RectTransform imageRect = backgroundImage.rectTransform;
            RectTransform parentRect = imageRect != null ? imageRect.parent as RectTransform : null;
            if (sprite == null || imageRect == null || parentRect == null)
            {
                StretchBackgroundToParent();
                return;
            }

            float viewWidth = parentRect.rect.width;
            float viewHeight = parentRect.rect.height;
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;
            if (viewWidth <= 0f || viewHeight <= 0f || spriteWidth <= 0f || spriteHeight <= 0f)
            {
                StretchBackgroundToParent();
                return;
            }

            float scale = Mathf.Max(viewWidth / spriteWidth, viewHeight / spriteHeight);
            float coverWidth = spriteWidth * scale;
            float coverHeight = spriteHeight * scale;

            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, coverWidth);
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, coverHeight);
            backgroundImage.preserveAspect = true;
        }

        private void StretchBackgroundToParent()
        {
            if (backgroundImage == null) return;

            RectTransform imageRect = backgroundImage.rectTransform;
            if (imageRect == null) return;

            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            imageRect.anchoredPosition = Vector2.zero;
            backgroundImage.preserveAspect = false;
        }
    }
}
