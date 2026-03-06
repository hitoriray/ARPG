using System;
using System.Collections.Generic;
using Data;
using JKFrame;
using Manager;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    [UIWindowData(typeof(UI_GameSettingsWindow), true, nameof(UI_GameSettingsWindow), 2)]
    public class UI_GameSettingsWindow : UI_WindowBase
    {
        [Serializable]
        private class RebindEntry
        {
            public string actionMap = "Player";
            public string actionName;
            [Min(0)] public int bindingIndex;

            [Header("Binding Label")]
            public TMP_Text bindingTMPText;
            public Text bindingText;

            [Header("Buttons")]
            public Button rebindButton;
            public Button clearButton;
            public ButtonManager rebindButtonManager;
            public ButtonManager clearButtonManager;
        }

        private struct ResolutionOption
        {
            public int Width;
            public int Height;
            public int RefreshRate;

            public ResolutionOption(int width, int height, int refreshRate)
            {
                Width = width;
                Height = height;
                RefreshRate = refreshRate;
            }

            public string GetLabel()
            {
                return $"{Width}x{Height} @{RefreshRate}Hz";
            }
        }

        [Header("Window Pages")]
        [SerializeField] private WindowManager windowManager;
        [SerializeField, Min(0)] private int displayPageIndex = 0;
        [SerializeField, Min(0)] private int audioPageIndex = 1;
        [SerializeField, Min(0)] private int gameplayPageIndex = 2;

        [Header("Display")]
        [SerializeField] private CustomDropdown resolutionDropdown;
        [SerializeField] private CustomDropdown fullScreenDropdown;
        [SerializeField] private SwitchManager vSyncToggle;
        [SerializeField] private CustomDropdown frameRateDropdown;
        [SerializeField] private CustomDropdown qualityDropdown;

        [Header("Audio")]
        [SerializeField] private SliderManager masterVolumeSlider;
        [SerializeField] private SliderManager bgmVolumeSlider;
        [SerializeField] private SliderManager sfxVolumeSlider;

        [Header("Gameplay")]
        [SerializeField] private SwitchManager quickCastToggle;
        [SerializeField] private SwitchManager autoLockToggle;
        [SerializeField] private SwitchManager cameraShakeToggle;
        [SerializeField] private SwitchManager damageNumberToggle;

        [Header("Input Rebind")]
        [SerializeField] private List<RebindEntry> rebindEntries = new();
        [SerializeField] private TMP_Text rebindTipTMPText;
        [SerializeField] private Text rebindTipText;
        [SerializeField] private ButtonManager resetBindingsButtonManager;

        [Header("General Buttons")]
        [SerializeField] private ButtonManager applyButtonManager;
        [SerializeField] private ButtonManager defaultButtonManager;
        [SerializeField] private ButtonManager closeButtonManager;
        [SerializeField] private ButtonManager quitGameButtonManager;

        [Header("Behavior")]
        [SerializeField] private bool autoApplyOnClose = true;

        private const float AudioSliderMin = 1f;
        private const float AudioSliderMax = 100f;

        private readonly List<ResolutionOption> _resolutionOptions = new();
        private readonly List<int> _frameRateOptions = new() { -1, 30, 60, 90, 120, 144, 240 };
        private readonly List<FullScreenMode> _fullScreenOptions = new()
        {
            FullScreenMode.FullScreenWindow,
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.MaximizedWindow,
            FullScreenMode.Windowed
        };

        private GameSettingsData _editingData;
        private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
        private int _currentPageIndex = -1;

        public override void Init()
        {
            base.Init();
            GameSettingsManager.Init();

            ResolveReferencesIfNeeded();
            ConfigureModernUIPackPersistence();
            BuildResolutionOptions();
            BuildDropDownOptions();
            BindButtons();
            BindWindowManagerEvent();
        }

        public override void OnShow()
        {
            base.OnShow();
            PlayerService.Instance?.SetCharacterControl(false);
            InputService.Instance?.inputMap?.UI.Disable();
            PlayerService.Instance?.PushUICursor();
            UIModalStack.Push(Close);

            // 防止按钮监听在运行中被覆盖，显示时重新绑定一次。
            BindButtons();

            _editingData = GameSettingsManager.GetCurrentSettingsCopy();
            RefreshUIFromData(_editingData);
            RefreshBindingLabels();
            SyncCurrentPage();
            SetTip("点击重绑按钮后按下新按键，ESC 取消。");
        }

        public override void OnClose()
        {
            base.OnClose();
            CancelRebindIfNeeded();

            if (autoApplyOnClose)
            {
                ApplyCurrentUIToSettings(saveTip: false);
            }

            PlayerService.Instance?.SetCharacterControl(true);
            InputService.Instance?.inputMap?.UI.Enable();
            PlayerService.Instance?.PopUICursor();
            UIModalStack.Remove(Close);
        }

        private void OnDestroy()
        {
            CancelRebindIfNeeded();
            UnbindWindowManagerEvent();
        }

        private void ResolveReferencesIfNeeded()
        {
            if (windowManager == null)
            {
                windowManager = GetComponentInChildren<WindowManager>(true);
            }
        }

        private void ConfigureModernUIPackPersistence()
        {
            ConfigureDropdownPersistence(resolutionDropdown);
            ConfigureDropdownPersistence(fullScreenDropdown);
            ConfigureDropdownPersistence(frameRateDropdown);
            ConfigureDropdownPersistence(qualityDropdown);

            ConfigureSliderPersistence(masterVolumeSlider);
            ConfigureSliderPersistence(bgmVolumeSlider);
            ConfigureSliderPersistence(sfxVolumeSlider);
            ConfigureAudioSliderRange(masterVolumeSlider);
            ConfigureAudioSliderRange(bgmVolumeSlider);
            ConfigureAudioSliderRange(sfxVolumeSlider);

            ConfigureSwitchPersistence(vSyncToggle);
            ConfigureSwitchPersistence(quickCastToggle);
            ConfigureSwitchPersistence(autoLockToggle);
            ConfigureSwitchPersistence(cameraShakeToggle);
            ConfigureSwitchPersistence(damageNumberToggle);
        }

        private static void ConfigureDropdownPersistence(CustomDropdown dropdown)
        {
            if (dropdown == null) return;
            dropdown.saveSelected = false;
        }

        private static void ConfigureSliderPersistence(SliderManager slider)
        {
            if (slider == null) return;
            slider.enableSaving = false;
        }

        private static void ConfigureAudioSliderRange(SliderManager sliderManager)
        {
            Slider slider = ResolveSlider(sliderManager);
            if (slider == null) return;

            float oldMin = slider.minValue;
            float oldMax = slider.maxValue;
            float oldValue = slider.value;
            bool wasNormalizedRange = oldMin >= -0.001f && oldMax <= 1.001f;

            slider.minValue = AudioSliderMin;
            slider.maxValue = AudioSliderMax;

            float targetValue = wasNormalizedRange
                ? Mathf.Clamp01(oldValue) * AudioSliderMax
                : oldValue;
            targetValue = Mathf.Clamp(targetValue, AudioSliderMin, AudioSliderMax);
            slider.SetValueWithoutNotify(targetValue);

            sliderManager?.UpdateUI();
        }

        private static void ConfigureSwitchPersistence(SwitchManager toggle)
        {
            if (toggle == null) return;
            toggle.saveValue = false;
        }

        private void BindWindowManagerEvent()
        {
            if (windowManager == null) return;

            windowManager.onWindowChange.RemoveListener(OnWindowPageChanged);
            windowManager.onWindowChange.AddListener(OnWindowPageChanged);
        }

        private void UnbindWindowManagerEvent()
        {
            if (windowManager == null) return;
            windowManager.onWindowChange.RemoveListener(OnWindowPageChanged);
        }

        private void SyncCurrentPage()
        {
            if (windowManager == null || windowManager.windows == null || windowManager.windows.Count == 0)
            {
                _currentPageIndex = -1;
                return;
            }

            _currentPageIndex = Mathf.Clamp(windowManager.currentWindowIndex, 0, windowManager.windows.Count - 1);
            OnWindowPageChanged(_currentPageIndex);
        }

        private void OnWindowPageChanged(int pageIndex)
        {
            _currentPageIndex = pageIndex;
            if (_editingData == null) return;

            if (_currentPageIndex == displayPageIndex)
            {
                RefreshDisplayUI(_editingData);
            }
            else if (_currentPageIndex == audioPageIndex)
            {
                RefreshAudioUI(_editingData);
            }
            else if (_currentPageIndex == gameplayPageIndex)
            {
                RefreshGameplayUI(_editingData);
            }
            else
            {
                RefreshUIFromData(_editingData);
            }
        }

        private void BindButtons()
        {
            if (closeButtonManager != null) { closeButtonManager.onClick.RemoveListener(Close); closeButtonManager.onClick.AddListener(Close); }
            if (applyButtonManager != null) { applyButtonManager.onClick.RemoveListener(OnApplyClick); applyButtonManager.onClick.AddListener(OnApplyClick); }
            if (defaultButtonManager != null) { defaultButtonManager.onClick.RemoveListener(OnDefaultClick); defaultButtonManager.onClick.AddListener(OnDefaultClick); }
            if (resetBindingsButtonManager != null) { resetBindingsButtonManager.onClick.RemoveListener(OnResetBindingsClick); resetBindingsButtonManager.onClick.AddListener(OnResetBindingsClick); }
            if (quitGameButtonManager != null) { quitGameButtonManager.onClick.RemoveListener(OnQuitGameClick); quitGameButtonManager.onClick.AddListener(OnQuitGameClick); }

            for (int i = 0; i < rebindEntries.Count; i++)
            {
                var entry = rebindEntries[i];
                if (entry == null) continue;

                if (entry.rebindButtonManager != null) entry.rebindButtonManager.onClick.RemoveAllListeners();
                if (entry.rebindButton != null) entry.rebindButton.onClick.RemoveAllListeners();
                if (entry.clearButtonManager != null) entry.clearButtonManager.onClick.RemoveAllListeners();
                if (entry.clearButton != null) entry.clearButton.onClick.RemoveAllListeners();

                var cacheEntry = entry;
                AddClick(entry.rebindButton, entry.rebindButtonManager, () => StartRebind(cacheEntry));
                AddClick(entry.clearButton, entry.clearButtonManager, () => ClearBindingOverride(cacheEntry));
            }
        }

        private static void BindClick(Button legacyButton, ButtonManager modernButton, UnityAction callback)
        {
            if (callback == null) return;

            if (modernButton != null)
            {
                modernButton.onClick.RemoveListener(callback);
                modernButton.onClick.AddListener(callback);
                return;
            }

            if (legacyButton == null) return;
            legacyButton.onClick.RemoveListener(callback);
            legacyButton.onClick.AddListener(callback);
        }

        private static void AddClick(Button legacyButton, ButtonManager modernButton, UnityAction callback)
        {
            if (callback == null) return;

            if (modernButton != null)
            {
                modernButton.onClick.AddListener(callback);
                return;
            }

            legacyButton?.onClick.AddListener(callback);
        }

        private void OnApplyClick()
        {
            ApplyCurrentUIToSettings(saveTip: true);
        }

        private void ApplyCurrentUIToSettings(bool saveTip)
        {
            if (_editingData == null)
            {
                _editingData = GameSettingsManager.GetCurrentSettingsCopy();
            }

            ReadUIToData(_editingData);
            GameSettingsManager.ApplyAndSave(_editingData);

            if (saveTip)
            {
                SetTip("设置已保存。");
            }
        }

        private void OnDefaultClick()
        {
            _editingData = GameSettingsManager.CreateDefaultSettings();
            RefreshUIFromData(_editingData);
            SetTip("已加载默认设置，点击应用后生效。");
        }

        private void OnResetBindingsClick()
        {
            GameSettingsManager.ResetAllBindings(true);
            _editingData = GameSettingsManager.GetCurrentSettingsCopy();
            RefreshBindingLabels();
            SetTip("按键已恢复默认。");
        }

        private void OnQuitGameClick()
        {
            UISystem.Show<UI_ConfirmWindow>()?.Show("退出游戏", "确认退出游戏？", UIHelper.QuitGame, null);
        }

        private void StartRebind(RebindEntry entry)
        {
            if (_rebindOperation != null)
            {
                SetTip("当前已有重绑流程进行中。");
                return;
            }

            if (entry == null || string.IsNullOrEmpty(entry.actionMap) || string.IsNullOrEmpty(entry.actionName))
            {
                SetTip("重绑配置不完整，请检查 actionMap/actionName。");
                return;
            }

            SetTip($"请按下 [{entry.actionName}] 新按键，ESC 取消。");
            _rebindOperation = GameSettingsManager.StartInteractiveRebind(
                entry.actionMap,
                entry.actionName,
                entry.bindingIndex,
                _ =>
                {
                    _rebindOperation = null;
                    _editingData = GameSettingsManager.GetCurrentSettingsCopy();
                    RefreshBindingLabels();
                    SetTip($"[{entry.actionName}] 重绑成功。");
                },
                () =>
                {
                    _rebindOperation = null;
                    SetTip("已取消重绑。");
                },
                reason =>
                {
                    _rebindOperation = null;
                    SetTip(reason);
                });

            if (_rebindOperation == null)
            {
                SetTip("重绑启动失败。");
            }
        }

        private void ClearBindingOverride(RebindEntry entry)
        {
            if (entry == null) return;

            bool ok = GameSettingsManager.RemoveBindingOverride(entry.actionMap, entry.actionName, entry.bindingIndex);
            if (ok)
            {
                _editingData = GameSettingsManager.GetCurrentSettingsCopy();
                RefreshBindingLabels();
                SetTip($"[{entry.actionName}] 已恢复默认。");
            }
            else
            {
                SetTip($"[{entry.actionName}] 恢复失败。");
            }
        }

        private void RefreshBindingLabels()
        {
            for (int i = 0; i < rebindEntries.Count; i++)
            {
                var entry = rebindEntries[i];
                if (entry == null) continue;

                string display = GameSettingsManager.GetBindingDisplayString(entry.actionMap, entry.actionName, entry.bindingIndex);
                if (string.IsNullOrEmpty(display))
                {
                    display = "未绑定";
                }

                SetBindingText(entry, display);
            }
        }

        private static void SetBindingText(RebindEntry entry, string text)
        {
            if (entry == null) return;

            if (entry.bindingTMPText != null)
            {
                entry.bindingTMPText.text = text;
                return;
            }

            if (entry.bindingText != null)
            {
                entry.bindingText.text = text;
            }
        }

        private void BuildDropDownOptions()
        {
            var resolutionLabels = new List<string>(_resolutionOptions.Count);
            for (int i = 0; i < _resolutionOptions.Count; i++)
            {
                resolutionLabels.Add(_resolutionOptions[i].GetLabel());
            }

            SetDropdownOptions(resolutionDropdown, resolutionLabels);
            SetDropdownOptions(fullScreenDropdown, new List<string>
            {
                "FullScreenWindow",
                "ExclusiveFullScreen",
                "MaximizedWindow",
                "Windowed"
            });

            var frameRateLabels = new List<string>(_frameRateOptions.Count);
            for (int i = 0; i < _frameRateOptions.Count; i++)
            {
                int fps = _frameRateOptions[i];
                frameRateLabels.Add(fps <= 0 ? "Unlimited" : fps.ToString());
            }

            SetDropdownOptions(frameRateDropdown, frameRateLabels);

            var qualityNames = QualitySettings.names ?? Array.Empty<string>();
            SetDropdownOptions(qualityDropdown, new List<string>(qualityNames));
        }

        private static void SetDropdownOptions(CustomDropdown dropdown, List<string> labels)
        {
            if (dropdown == null) return;
            if (labels == null) labels = new List<string>();

            int previousIndex = dropdown.selectedItemIndex;
            dropdown.items ??= new List<CustomDropdown.Item>();
            dropdown.items.Clear();

            for (int i = 0; i < labels.Count; i++)
            {
                dropdown.items.Add(new CustomDropdown.Item { itemName = labels[i] });
            }

            if (dropdown.items.Count == 0)
            {
                dropdown.items.Add(new CustomDropdown.Item { itemName = "N/A" });
            }

            dropdown.selectedItemIndex = Mathf.Clamp(previousIndex, 0, dropdown.items.Count - 1);
            dropdown.SetupDropdown();
            dropdown.SetDropdownIndex(dropdown.selectedItemIndex, true);
        }

        private void BuildResolutionOptions()
        {
            _resolutionOptions.Clear();
            var set = new HashSet<string>();

            var resolutions = Screen.resolutions;
            for (int i = 0; i < resolutions.Length; i++)
            {
                var res = resolutions[i];
                int refreshRate = GetRefreshRate(res);
                string key = $"{res.width}x{res.height}@{refreshRate}";
                if (!set.Add(key)) continue;

                _resolutionOptions.Add(new ResolutionOption(res.width, res.height, refreshRate));
            }

            _resolutionOptions.Sort((a, b) =>
            {
                int widthCompare = a.Width.CompareTo(b.Width);
                if (widthCompare != 0) return widthCompare;
                int heightCompare = a.Height.CompareTo(b.Height);
                if (heightCompare != 0) return heightCompare;
                return a.RefreshRate.CompareTo(b.RefreshRate);
            });

            if (_resolutionOptions.Count == 0)
            {
                Resolution current = Screen.currentResolution;
                _resolutionOptions.Add(new ResolutionOption(current.width, current.height, GetRefreshRate(current)));
            }
        }

        private void RefreshUIFromData(GameSettingsData data)
        {
            if (data == null) return;
            data.Sanitize();

            RefreshDisplayUI(data);
            RefreshAudioUI(data);
            RefreshGameplayUI(data);
        }

        private void RefreshDisplayUI(GameSettingsData data)
        {
            if (data == null) return;

            if (resolutionDropdown != null && _resolutionOptions.Count > 0)
            {
                int targetIndex = 0;
                for (int i = 0; i < _resolutionOptions.Count; i++)
                {
                    var item = _resolutionOptions[i];
                    if (item.Width != data.Display.Width || item.Height != data.Display.Height) continue;

                    targetIndex = i;
                    if (item.RefreshRate == data.Display.RefreshRate) break;
                }

                SetDropdownIndex(resolutionDropdown, targetIndex);
            }

            if (fullScreenDropdown != null)
            {
                int index = _fullScreenOptions.IndexOf(data.Display.FullScreenMode);
                if (index < 0) index = 0;
                SetDropdownIndex(fullScreenDropdown, index);
            }

            SetSwitchValue(vSyncToggle, data.Display.VSync);

            if (frameRateDropdown != null)
            {
                int index = _frameRateOptions.IndexOf(data.Display.TargetFrameRate);
                if (index < 0)
                {
                    index = _frameRateOptions.IndexOf(-1);
                }

                SetDropdownIndex(frameRateDropdown, index < 0 ? 0 : index);
            }

            if (qualityDropdown != null)
            {
                int count = Mathf.Max(1, GetDropdownOptionCount(qualityDropdown));
                SetDropdownIndex(qualityDropdown, Mathf.Clamp(data.Display.QualityLevel, 0, count - 1));
            }
        }

        private void RefreshAudioUI(GameSettingsData data)
        {
            if (data == null) return;

            SetSliderValue(masterVolumeSlider, data.Audio.MasterVolume);
            SetSliderValue(bgmVolumeSlider, data.Audio.BgmVolume);
            SetSliderValue(sfxVolumeSlider, data.Audio.SfxVolume);
        }

        private void RefreshGameplayUI(GameSettingsData data)
        {
            if (data == null) return;

            SetSwitchValue(quickCastToggle, data.Gameplay.QuickCast);
            SetSwitchValue(autoLockToggle, data.Gameplay.AutoLockTarget);
            SetSwitchValue(cameraShakeToggle, data.Gameplay.CameraShake);
            SetSwitchValue(damageNumberToggle, data.Gameplay.ShowDamageNumber);
        }

        private void ReadUIToData(GameSettingsData data)
        {
            if (data == null) return;

            if (resolutionDropdown != null && _resolutionOptions.Count > 0)
            {
                int index = Mathf.Clamp(GetDropdownIndex(resolutionDropdown), 0, _resolutionOptions.Count - 1);
                var resolution = _resolutionOptions[index];
                data.Display.Width = resolution.Width;
                data.Display.Height = resolution.Height;
                data.Display.RefreshRate = resolution.RefreshRate;
            }

            if (fullScreenDropdown != null)
            {
                int index = Mathf.Clamp(GetDropdownIndex(fullScreenDropdown), 0, _fullScreenOptions.Count - 1);
                data.Display.FullScreenMode = _fullScreenOptions[index];
            }

            data.Display.VSync = GetSwitchValue(vSyncToggle);

            if (frameRateDropdown != null)
            {
                int index = Mathf.Clamp(GetDropdownIndex(frameRateDropdown), 0, _frameRateOptions.Count - 1);
                data.Display.TargetFrameRate = _frameRateOptions[index];
            }

            if (qualityDropdown != null)
            {
                int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
                int qualityIndex = GetDropdownIndex(qualityDropdown);
                if (qualityCount > 0)
                {
                    data.Display.QualityLevel = Mathf.Clamp(qualityIndex, 0, qualityCount - 1);
                }
                else
                {
                    data.Display.QualityLevel = 0;
                }
            }

            data.Audio.MasterVolume = GetSliderValue(masterVolumeSlider, data.Audio.MasterVolume);
            data.Audio.BgmVolume = GetSliderValue(bgmVolumeSlider, data.Audio.BgmVolume);
            data.Audio.SfxVolume = GetSliderValue(sfxVolumeSlider, data.Audio.SfxVolume);

            data.Gameplay.QuickCast = GetSwitchValue(quickCastToggle, data.Gameplay.QuickCast);
            data.Gameplay.AutoLockTarget = GetSwitchValue(autoLockToggle, data.Gameplay.AutoLockTarget);
            data.Gameplay.CameraShake = GetSwitchValue(cameraShakeToggle, data.Gameplay.CameraShake);
            data.Gameplay.ShowDamageNumber = GetSwitchValue(damageNumberToggle, data.Gameplay.ShowDamageNumber);

            data.Sanitize();
        }

        private static int GetDropdownOptionCount(CustomDropdown dropdown)
        {
            return dropdown?.items?.Count ?? 0;
        }

        private static int GetDropdownIndex(CustomDropdown dropdown)
        {
            if (dropdown == null) return 0;
            int count = GetDropdownOptionCount(dropdown);
            if (count <= 0) return 0;

            return Mathf.Clamp(dropdown.selectedItemIndex, 0, count - 1);
        }

        private static void SetDropdownIndex(CustomDropdown dropdown, int index)
        {
            if (dropdown == null) return;
            int count = GetDropdownOptionCount(dropdown);
            if (count <= 0) return;

            int clampedIndex = Mathf.Clamp(index, 0, count - 1);
            dropdown.selectedItemIndex = clampedIndex;
            dropdown.SetDropdownIndex(clampedIndex, true);
        }

        private static Slider ResolveSlider(SliderManager sliderManager)
        {
            if (sliderManager == null) return null;
            if (sliderManager.mainSlider == null)
            {
                sliderManager.mainSlider = sliderManager.GetComponent<Slider>();
            }

            return sliderManager.mainSlider;
        }

        private static void SetSliderValue(SliderManager sliderManager, float value)
        {
            if (sliderManager == null) return;

            Slider slider = ResolveSlider(sliderManager);
            if (slider == null) return;

            float targetValue;
            if (IsNormalizedRange01(slider))
            {
                targetValue = Mathf.Clamp(value, AudioSliderMin, AudioSliderMax) / 100f;
            }
            else if (slider.maxValue > slider.minValue)
            {
                targetValue = Mathf.Clamp(value, slider.minValue, slider.maxValue);
            }
            else
            {
                targetValue = Mathf.Clamp(value, AudioSliderMin, AudioSliderMax);
            }

            slider.SetValueWithoutNotify(targetValue);
            sliderManager.UpdateUI();
        }

        private static float GetSliderValue(SliderManager sliderManager, float fallbackValue = 0f)
        {
            Slider slider = ResolveSlider(sliderManager);
            if (slider == null) return fallbackValue;

            if (IsNormalizedRange01(slider))
            {
                return Mathf.Clamp01(slider.value) * 100f;
            }

            if (slider.maxValue > slider.minValue)
            {
                return Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
            }

            return slider.value;
        }

        private static bool IsNormalizedRange01(Slider slider)
        {
            if (slider == null) return false;
            return slider.minValue >= -0.001f && slider.maxValue <= 1.001f;
        }

        private static void SetSwitchValue(SwitchManager switchManager, bool isOn)
        {
            if (switchManager == null) return;
            switchManager.isOn = isOn;

            // 可能因为页签被隐藏导致 Awake 尚未缓存 Animator，这里做惰性初始化保护。
            if (switchManager.switchAnimator == null)
            {
                switchManager.switchAnimator = switchManager.GetComponent<Animator>();
                if (switchManager.switchAnimator == null)
                {
                    return;
                }
            }

            switchManager.UpdateUI();
        }

        private static bool GetSwitchValue(SwitchManager switchManager, bool fallbackValue = false)
        {
            return switchManager != null ? switchManager.isOn : fallbackValue;
        }

        private void Close()
        {
            UISystem.Close<UI_GameSettingsWindow>();
        }

        private void CancelRebindIfNeeded()
        {
            if (_rebindOperation == null) return;

            var operation = _rebindOperation;
            _rebindOperation = null;
            operation.Cancel();
        }

        private void SetTip(string message)
        {
            if (rebindTipTMPText != null)
            {
                rebindTipTMPText.text = message;
                return;
            }

            if (rebindTipText != null)
            {
                rebindTipText.text = message;
            }
        }

        private static int GetRefreshRate(Resolution resolution)
        {
#if UNITY_2022_2_OR_NEWER
            return Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
#else
            return resolution.refreshRate;
#endif
        }
    }
}
