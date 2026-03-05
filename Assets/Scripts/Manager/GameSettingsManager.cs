using System;
using Data;
using JKFrame;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using AudioSettingsData = Data.AudioSettings;
using InputSettingsData = Data.InputSettings;

namespace Manager
{
    /// <summary>
    /// 全局设置管理：加载、应用、保存、按键重绑。
    /// </summary>
    public static class GameSettingsManager
    {
        private const string SettingsFileName = nameof(GameSettingsData);

        private static bool initialized;
        private static GameSettingsData currentSettings;

        public static event Action<GameSettingsData> OnSettingsApplied;

        public static GameSettingsData CurrentSettings
        {
            get
            {
                EnsureInit();
                return currentSettings;
            }
        }

        public static void Init()
        {
            if (initialized) return;

            SaveSystem.Init();
            currentSettings = SaveSystem.LoadSetting<GameSettingsData>(SettingsFileName);
            if (currentSettings == null)
            {
                currentSettings = CreateDefaultSettings();
                SaveSystem.SaveSetting(currentSettings, SettingsFileName);
            }

            currentSettings.Sanitize();
            ApplyInternal(currentSettings, applyResolution: true);
            initialized = true;
        }

        public static GameSettingsData CreateDefaultSettings()
        {
            var settings = new GameSettingsData();

            Resolution resolution = Screen.currentResolution;
            settings.Display.Width = resolution.width;
            settings.Display.Height = resolution.height;
            settings.Display.RefreshRate = GetRefreshRate(resolution);
            settings.Display.FullScreenMode = Screen.fullScreenMode;
            settings.Display.VSync = QualitySettings.vSyncCount > 0;
            settings.Display.TargetFrameRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 120;
            settings.Display.QualityLevel = QualitySettings.GetQualityLevel();

            settings.Input.BindingOverridesJson = string.Empty;
            settings.Sanitize();
            return settings;
        }

        public static GameSettingsData GetCurrentSettingsCopy()
        {
            EnsureInit();
            return currentSettings.Clone();
        }

        public static void ApplyAndSave(GameSettingsData settings)
        {
            EnsureInit();
            ApplyInternal(settings, applyResolution: true);
            SaveCurrent();
        }

        public static void ApplyWithoutSave(GameSettingsData settings, bool applyResolution = true)
        {
            EnsureInit();
            ApplyInternal(settings, applyResolution);
        }

        public static void SaveCurrent()
        {
            EnsureInit();
            SaveSystem.Init();
            SaveSystem.SaveSetting(currentSettings, SettingsFileName);
        }

        public static void ResetToDefault(bool saveImmediately = true)
        {
            EnsureInit();
            ApplyInternal(CreateDefaultSettings(), applyResolution: true);
            if (saveImmediately)
            {
                SaveCurrent();
            }
        }

        public static void CaptureCurrentBindingOverrides(bool saveImmediately = true)
        {
            EnsureInit();
            var inputAsset = InputService.Instance?.inputMap?.asset;
            if (inputAsset == null) return;

            currentSettings.Input.BindingOverridesJson = inputAsset.SaveBindingOverridesAsJson();
            if (saveImmediately)
            {
                SaveCurrent();
            }
        }

        public static void ResetAllBindings(bool saveImmediately = true)
        {
            EnsureInit();
            var inputAsset = InputService.Instance?.inputMap?.asset;
            if (inputAsset == null) return;

            inputAsset.RemoveAllBindingOverrides();
            currentSettings.Input.BindingOverridesJson = string.Empty;
            if (saveImmediately)
            {
                SaveCurrent();
            }
        }

        public static bool RemoveBindingOverride(string actionMapName, string actionName, int bindingIndex,
            bool saveImmediately = true)
        {
            EnsureInit();
            if (!TryGetAction(actionMapName, actionName, out var action))
            {
                return false;
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return false;
            }

            action.RemoveBindingOverride(bindingIndex);
            CaptureCurrentBindingOverrides(saveImmediately);
            return true;
        }

        public static string GetBindingDisplayString(string actionMapName, string actionName, int bindingIndex,
            bool omitDevice = true)
        {
            if (!TryGetAction(actionMapName, actionName, out var action))
            {
                return string.Empty;
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return string.Empty;
            }

            string path = action.bindings[bindingIndex].effectivePath;
            if (string.IsNullOrEmpty(path))
            {
                path = action.bindings[bindingIndex].path;
            }

            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var options = omitDevice
                ? InputControlPath.HumanReadableStringOptions.OmitDevice
                : InputControlPath.HumanReadableStringOptions.None;
            return InputControlPath.ToHumanReadableString(path, options);
        }

        public static InputActionRebindingExtensions.RebindingOperation StartInteractiveRebind(
            string actionMapName,
            string actionName,
            int bindingIndex,
            Action<string> onSuccess,
            Action onCanceled = null,
            Action<string> onFailed = null,
            bool saveImmediately = true)
        {
            EnsureInit();
            if (!TryGetAction(actionMapName, actionName, out var action))
            {
                onFailed?.Invoke($"未找到输入动作: {actionMapName}/{actionName}");
                return null;
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                onFailed?.Invoke($"bindingIndex 越界: {bindingIndex}");
                return null;
            }

            var targetBinding = action.bindings[bindingIndex];
            if (targetBinding.isComposite)
            {
                onFailed?.Invoke("请重绑组合键的子按键，而不是组合键本体。");
                return null;
            }

            action.Disable();

            var operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .OnMatchWaitForAnother(0.08f)
                .OnCancel(op =>
                {
                    op.Dispose();
                    action.Enable();
                    onCanceled?.Invoke();
                })
                .OnComplete(op =>
                {
                    op.Dispose();

                    if (HasBindingConflict(action.actionMap, action, bindingIndex, out var conflictAction))
                    {
                        action.RemoveBindingOverride(bindingIndex);
                        action.Enable();
                        onFailed?.Invoke($"按键冲突：{conflictAction.name}");
                        return;
                    }

                    action.Enable();
                    CaptureCurrentBindingOverrides(saveImmediately);
                    onSuccess?.Invoke(GetBindingDisplayString(actionMapName, actionName, bindingIndex));
                });

            operation.Start();
            return operation;
        }

        private static void EnsureInit()
        {
            if (!initialized)
            {
                Init();
            }
        }

        private static void ApplyInternal(GameSettingsData settings, bool applyResolution)
        {
            if (settings == null)
            {
                JKLog.Warning("[GameSettingsManager] ApplyInternal ignored: settings is null.");
                return;
            }

            currentSettings = settings.Clone();
            currentSettings.Sanitize();

            ApplyDisplaySettings(currentSettings.Display, applyResolution);
            ApplyAudioSettings(currentSettings.Audio);
            ApplyInputSettings(currentSettings.Input);

            OnSettingsApplied?.Invoke(currentSettings);
        }

        private static void ApplyDisplaySettings(DisplaySettings settings, bool applyResolution)
        {
            if (settings == null) return;

            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;
            Application.targetFrameRate = settings.TargetFrameRate <= 0 ? -1 : settings.TargetFrameRate;

            int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            if (qualityCount > 0)
            {
                int quality = Mathf.Clamp(settings.QualityLevel, 0, qualityCount - 1);
                if (QualitySettings.GetQualityLevel() != quality)
                {
                    QualitySettings.SetQualityLevel(quality, true);
                }
            }

            if (!applyResolution) return;

            int width = Mathf.Max(640, settings.Width);
            int height = Mathf.Max(360, settings.Height);
            int refreshRate = Mathf.Clamp(settings.RefreshRate, 30, 360);
#if UNITY_2022_2_OR_NEWER
            var refresh = new RefreshRate
            {
                numerator = (uint)refreshRate,
                denominator = 1
            };
            Screen.SetResolution(width, height, settings.FullScreenMode, refresh);
#else
            Screen.SetResolution(width, height, settings.FullScreenMode, refreshRate);
#endif
        }

        private static void ApplyAudioSettings(AudioSettingsData settings)
        {
            if (settings == null) return;

            float master01 = ToVolume01(settings.MasterVolume);
            float bgm01 = ToVolume01(settings.BgmVolume);
            float sfx01 = ToVolume01(settings.SfxVolume);

            AudioSystem.GlobalVolume = master01;
            AudioSystem.BGVolume = bgm01;
            AudioSystem.EffectVolume = sfx01;
            AudioSystem.IsMute = master01 <= 0.0001f;
        }

        private static void ApplyInputSettings(InputSettingsData settings)
        {
            if (settings == null) return;

            var inputAsset = InputService.Instance?.inputMap?.asset;
            if (inputAsset == null) return;

            inputAsset.RemoveAllBindingOverrides();
            if (string.IsNullOrEmpty(settings.BindingOverridesJson)) return;

            try
            {
                inputAsset.LoadBindingOverridesFromJson(settings.BindingOverridesJson);
            }
            catch (Exception e)
            {
                JKLog.Warning($"[GameSettingsManager] 读取按键重绑失败，已跳过：{e.Message}");
            }
        }

        private static bool TryGetAction(string actionMapName, string actionName, out InputAction action)
        {
            action = null;
            var inputAsset = InputService.Instance?.inputMap?.asset;
            if (inputAsset == null) return false;

            var actionMap = inputAsset.FindActionMap(actionMapName, throwIfNotFound: false);
            if (actionMap == null) return false;

            action = actionMap.FindAction(actionName, throwIfNotFound: false);
            return action != null;
        }

        private static bool HasBindingConflict(InputActionMap map, InputAction targetAction, int targetBindingIndex,
            out InputAction conflictAction)
        {
            conflictAction = null;
            if (map == null || targetAction == null) return false;
            if (targetBindingIndex < 0 || targetBindingIndex >= targetAction.bindings.Count) return false;

            string targetPath = targetAction.bindings[targetBindingIndex].effectivePath;
            if (string.IsNullOrEmpty(targetPath)) return false;

            foreach (var action in map.actions)
            {
                var bindings = action.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (action == targetAction && i == targetBindingIndex) continue;
                    if (bindings[i].isComposite) continue;

                    string path = bindings[i].effectivePath;
                    if (string.IsNullOrEmpty(path)) continue;

                    if (!string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase)) continue;

                    conflictAction = action;
                    return true;
                }
            }

            return false;
        }

        private static int GetRefreshRate(Resolution resolution)
        {
#if UNITY_2022_2_OR_NEWER
            return Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
#else
            return resolution.refreshRate;
#endif
        }

        private static float ToVolume01(float valuePercent)
        {
            return Mathf.Clamp(valuePercent, 1f, 100f) / 100f;
        }
    }
}
