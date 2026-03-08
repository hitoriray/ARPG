using JKFrame;
using Michsky.MUIP;
using TMPro;
using UnityEngine;

namespace UI
{
    [UIWindowData(typeof(UI_RespawnCountdownWindow), true, nameof(UI_RespawnCountdownWindow), 2)]
    public class UI_RespawnCountdownWindow : UI_WindowBase
    {
        [Header("Countdown")]
        [SerializeField] private ProgressBar progressBar;
        [SerializeField] private TMP_Text countdownTMPText;
        [SerializeField] private string countdownFormat = "{0:0.0}s";
        [SerializeField] private string defaultCountdownText = "--";

        private float totalSeconds = 1f;
        private float remainSeconds;
        private float countdownEndUnscaledTime;
        private bool countdownRunning;

        public override void OnShow()
        {
            base.OnShow();

            totalSeconds = Mathf.Max(0.0001f, totalSeconds);
            remainSeconds = totalSeconds;
            countdownRunning = false;

            SetCountdownText(defaultCountdownText);
            SetProgress(1f);
        }

        private void Update()
        {
            if (!UIEnable || !countdownRunning)
                return;

            float remain = countdownEndUnscaledTime - Time.unscaledTime;
            if (remain <= 0f)
            {
                remainSeconds = 0f;
                countdownRunning = false;
                ApplyCountdownDisplay();
                return;
            }

            remainSeconds = remain;
            ApplyCountdownDisplay();
        }

        protected override void RegisterEventListener()
        {
            base.RegisterEventListener();
            EventSystem.AddEventListener<float>(RespawnCountdownEvents.CountdownStartEvent, OnCountdownStart);
            EventSystem.AddEventListener<float>(RespawnCountdownEvents.CountdownTickEvent, OnCountdownTick);
            EventSystem.AddEventListener(RespawnCountdownEvents.CountdownEndEvent, OnCountdownEnd);
        }

        protected override void UnRegisterEventListener()
        {
            base.UnRegisterEventListener();
            EventSystem.RemoveEventListener<float>(RespawnCountdownEvents.CountdownStartEvent, OnCountdownStart);
            EventSystem.RemoveEventListener<float>(RespawnCountdownEvents.CountdownTickEvent, OnCountdownTick);
            EventSystem.RemoveEventListener(RespawnCountdownEvents.CountdownEndEvent, OnCountdownEnd);
        }

        private void OnCountdownStart(float total)
        {
            totalSeconds = Mathf.Max(0.0001f, total);
            remainSeconds = Mathf.Clamp(total, 0f, totalSeconds);
            countdownEndUnscaledTime = Time.unscaledTime + remainSeconds;
            countdownRunning = remainSeconds > 0f;

            ApplyCountdownDisplay();
        }

        private void OnCountdownTick(float remain)
        {
            remainSeconds = Mathf.Clamp(remain, 0f, totalSeconds);
            countdownEndUnscaledTime = Time.unscaledTime + remainSeconds;
            countdownRunning = remainSeconds > 0f;

            ApplyCountdownDisplay();
        }

        private void OnCountdownEnd()
        {
            countdownRunning = false;
            remainSeconds = 0f;
            ApplyCountdownDisplay();
        }

        private void ApplyCountdownDisplay()
        {
            // Countdown bar goes from full to empty and reaches zero exactly when respawn time ends.
            float progress01 = Mathf.Clamp01(remainSeconds / totalSeconds);
            SetProgress(progress01);
            SetCountdownText(string.Format(countdownFormat, remainSeconds));
        }

        private void SetProgress(float progress01)
        {
            if (progressBar == null)
                return;

            progressBar.isOn = false;
            progressBar.speed = 0;

            float maxValue = progressBar.maxValue > 0f ? progressBar.maxValue : 100f;
            progressBar.SetValue(maxValue * Mathf.Clamp01(progress01));
        }

        private void SetCountdownText(string content)
        {
            if (countdownTMPText != null)
                countdownTMPText.text = content;
        }
    }
}
