using UnityEngine;

namespace WeGrowPeas.UI
{
    /// <summary>Applies saved graphics preferences without changing legacy defaults.</summary>
    public static class SettingsDisplay
    {
        public static bool SupportsWindowSettings => Application.isEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer;

        // JsonUtility can materialize an empty nested object when loading an older save.
        private static bool IsConfigured(DisplaySettingsData data) => data != null && data.width > 0 && data.height > 0;

        public static DisplaySettingsData Current => IsConfigured(DeviceSettings.Data.display) ? DeviceSettings.Data.display : new DisplaySettingsData
        {
            width = Screen.width, height = Screen.height,
            fullScreenMode = (int)Screen.fullScreenMode,
            vSyncCount = QualitySettings.vSyncCount,
            frameRate = Application.targetFrameRate
        };

        public static DisplaySettingsData Edit()
        {
            if (!IsConfigured(DeviceSettings.Data.display)) DeviceSettings.Data.display = Current;
            return DeviceSettings.Data.display;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyOnStart() => Apply(DeviceSettings.Data.display);

        public static void Apply(DisplaySettingsData data)
        {
            if (!IsConfigured(data)) return;
            // GraphicsQuality owns rendering presets. Legacy display.qualityLevel is intentionally ignored.
            if (data.vSyncCount >= 0 && data.vSyncCount <= 4) QualitySettings.vSyncCount = data.vSyncCount;
            if (data.frameRate == -1 || data.frameRate >= 15 && data.frameRate <= 360) Application.targetFrameRate = data.frameRate;
            // The editor Game view has its own resolution selector; don't resize editor windows.
            if (!Application.isEditor && SupportsWindowSettings && data.width > 0 && data.height > 0 && data.fullScreenMode >= 0 && data.fullScreenMode <= 3)
            {
                var mode = (FullScreenMode)data.fullScreenMode;
                if (Screen.width != data.width || Screen.height != data.height || Screen.fullScreenMode != mode)
                    Screen.SetResolution(data.width, data.height, mode);
            }
        }
    }
}
