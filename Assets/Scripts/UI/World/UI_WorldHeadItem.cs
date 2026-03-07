using UnityEngine;
using UnityEngine.UI;
using Michsky.MUIP;
using TMPro;

namespace UI
{
    public class UI_WorldHeadItem : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject hpRoot;
        [SerializeField] private ProgressBar hpProgressBar;
        public RectTransform RootRect => rootRect;

        private void Awake()
        {
            if (rootRect == null)
                rootRect = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (hpRoot == null && hpProgressBar != null)
                hpRoot = hpProgressBar.gameObject;
            if (hpProgressBar == null)
            {
                hpProgressBar = GetComponentInChildren<ProgressBar>(true);
                if (hpProgressBar != null && hpRoot == null)
                    hpRoot = hpProgressBar.gameObject;
            }

            // Keep MUIP progress bars fully data-driven (no auto-play drift).
            if (hpProgressBar != null)
            {
                hpProgressBar.isOn = false;
                hpProgressBar.speed = 0;
            }
        }

        public void SetDisplayName(string displayName)
        {
            if (nameText == null)
                return;

            nameText.text = string.IsNullOrWhiteSpace(displayName) ? "Unknown" : displayName;
        }

        public void SetHpVisible(bool visible)
        {
            if (hpRoot != null && hpRoot.activeSelf != visible)
                hpRoot.SetActive(visible);
        }

        public void SetHpRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);

            if (hpProgressBar != null)
            {
                hpProgressBar.isOn = false;
                hpProgressBar.speed = 0;
                float maxValue = hpProgressBar.maxValue > 0f ? hpProgressBar.maxValue : 1f;
                hpProgressBar.SetValue(maxValue * ratio);
            }
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void SetScale(float scale)
        {
            if (rootRect == null)
                return;

            float clamped = Mathf.Max(0.01f, scale);
            rootRect.localScale = new Vector3(clamped, clamped, 1f);
        }
    }
}
