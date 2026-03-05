using System;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// Global settings save data.
    /// </summary>
    [Serializable]
    public class GameSettingsData
    {
        public int QualityProfileSchemaVersion = 3;
        public int AudioValueSchemaVersion = 3;
        public DisplaySettings Display = new();
        public AudioSettings Audio = new();
        public InputSettings Input = new();
        public GameplaySettings Gameplay = new();

        public GameSettingsData Clone()
        {
            return JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(this));
        }

        public void Sanitize()
        {
            Display ??= new DisplaySettings();
            Audio ??= new AudioSettings();
            Input ??= new InputSettings();
            Gameplay ??= new GameplaySettings();

            // Quality profile migrations.
            // v1: [Medium, High, ...]
            // v2: [Low, Medium, High, ...]
            // v3: [Very Low, Low, Medium, High, ...]
            if (QualityProfileSchemaVersion < 2)
            {
                if (NeedShiftQualityIndexForLowPreset())
                {
                    Display.QualityLevel += 1;
                }

                QualityProfileSchemaVersion = 2;
            }

            if (QualityProfileSchemaVersion < 3)
            {
                if (NeedShiftQualityIndexForVeryLowPreset())
                {
                    Display.QualityLevel += 1;
                }

                QualityProfileSchemaVersion = 3;
            }

            // Audio value migrations.
            // v1: 0~1
            // v2: 1~100
            // v3: fix legacy saves that were accidentally written in 0~1 while marked as v2.
            if (AudioValueSchemaVersion < 2)
            {
                MigrateAudioToPercentIfNeeded();
                AudioValueSchemaVersion = 2;
            }

            if (AudioValueSchemaVersion < 3)
            {
                MigrateAudioToPercentIfNeeded();
                AudioValueSchemaVersion = 3;
            }

            Display.Width = Mathf.Max(640, Display.Width);
            Display.Height = Mathf.Max(360, Display.Height);
            Display.RefreshRate = Mathf.Clamp(Display.RefreshRate, 30, 360);
            Display.TargetFrameRate = Display.TargetFrameRate <= 0
                ? -1
                : Mathf.Clamp(Display.TargetFrameRate, 30, 360);

            int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            if (qualityCount > 0)
            {
                Display.QualityLevel = Mathf.Clamp(Display.QualityLevel, 0, qualityCount - 1);
            }
            else
            {
                Display.QualityLevel = 0;
            }

            Audio.MasterVolume = ClampAudioPercent(Audio.MasterVolume);
            Audio.BgmVolume = ClampAudioPercent(Audio.BgmVolume);
            Audio.SfxVolume = ClampAudioPercent(Audio.SfxVolume);
            Audio.UiVolume = ClampAudioPercent(Audio.UiVolume);

            Input.MouseSensitivity = Mathf.Clamp(Input.MouseSensitivity, 0.1f, 10f);
            Input.StickSensitivity = Mathf.Clamp(Input.StickSensitivity, 0.1f, 10f);
            Input.LeftStickDeadZone = Mathf.Clamp(Input.LeftStickDeadZone, 0f, 0.95f);
            Input.RightStickDeadZone = Mathf.Clamp(Input.RightStickDeadZone, 0f, 0.95f);
            Input.VibrationScale = Mathf.Clamp01(Input.VibrationScale);
            Input.BindingOverridesJson ??= string.Empty;

            Gameplay.CameraShakeScale = Mathf.Clamp01(Gameplay.CameraShakeScale);
        }

        private static bool NeedShiftQualityIndexForLowPreset()
        {
            string[] names = QualitySettings.names;
            if (names == null || names.Length <= 0) return false;

            int lowIndex = Array.FindIndex(names, n => string.Equals(n, "Low", StringComparison.OrdinalIgnoreCase));
            int mediumIndex = Array.FindIndex(names, n => string.Equals(n, "Medium", StringComparison.OrdinalIgnoreCase));
            if (lowIndex < 0 || mediumIndex < 0) return false;

            return lowIndex < mediumIndex;
        }

        private static bool NeedShiftQualityIndexForVeryLowPreset()
        {
            string[] names = QualitySettings.names;
            if (names == null || names.Length <= 0) return false;

            int veryLowIndex = Array.FindIndex(names, n => string.Equals(n, "Very Low", StringComparison.OrdinalIgnoreCase));
            int lowIndex = Array.FindIndex(names, n => string.Equals(n, "Low", StringComparison.OrdinalIgnoreCase));
            if (veryLowIndex < 0 || lowIndex < 0) return false;

            return veryLowIndex < lowIndex;
        }

        private bool NeedMigrateAudioToPercent()
        {
            // Old 0~1 data usually has every channel <= 1.
            return Audio.MasterVolume <= 1.001f
                   && Audio.BgmVolume <= 1.001f
                   && Audio.SfxVolume <= 1.001f
                   && Audio.UiVolume <= 1.001f;
        }

        private void MigrateAudioToPercentIfNeeded()
        {
            if (!NeedMigrateAudioToPercent()) return;

            Audio.MasterVolume *= 100f;
            Audio.BgmVolume *= 100f;
            Audio.SfxVolume *= 100f;
            Audio.UiVolume *= 100f;
        }

        private static float ClampAudioPercent(float value)
        {
            return Mathf.Clamp(value, 1f, 100f);
        }
    }

    [Serializable]
    public class DisplaySettings
    {
        public int Width = 1920;
        public int Height = 1080;
        public int RefreshRate = 60;
        public FullScreenMode FullScreenMode = FullScreenMode.FullScreenWindow;
        public bool VSync = true;
        public int TargetFrameRate = 120;
        public int QualityLevel = 2;
    }

    [Serializable]
    public class AudioSettings
    {
        public float MasterVolume = 100f;
        public float BgmVolume = 80f;
        public float SfxVolume = 100f;
        public float UiVolume = 100f;
    }

    [Serializable]
    public class InputSettings
    {
        /// <summary>
        /// InputSystem binding overrides json.
        /// </summary>
        public string BindingOverridesJson = string.Empty;

        public float MouseSensitivity = 1f;
        public float StickSensitivity = 1f;
        public float LeftStickDeadZone = 0.15f;
        public float RightStickDeadZone = 0.15f;
        public bool InvertY = false;
        public bool EnableVibration = true;
        public float VibrationScale = 1f;
    }

    [Serializable]
    public class GameplaySettings
    {
        public bool QuickCast = true;
        public bool AutoLockTarget = true;
        public bool CameraShake = true;
        public float CameraShakeScale = 1f;
        public bool ShowDamageNumber = true;
    }
}
