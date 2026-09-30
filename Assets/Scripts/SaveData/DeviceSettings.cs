using System;

/// <summary>
/// 기기 공용 설정(settings.json). 프로필을 바꿔도 유지된다.
/// 각 시스템은 값을 따로 들고 있지 말고 <see cref="Data"/>를 직접 읽고 쓴다.
/// 처음 접근할 때 파일에서 읽으므로 매니저 초기화 순서와 무관하다.
/// </summary>
public static class DeviceSettings
{
    private static SettingsData _data;

    public static SettingsData Data
    {
        get
        {
            if (_data == null && !SaveIO.TryRead(ProfileStore.SettingsPath, out _data))
                _data = new SettingsData();
            return _data;
        }
    }

    public static void Save()
    {
        Data.version = SettingsData.CurrentVersion;
        SaveIO.Write(ProfileStore.SettingsPath, Data);
    }

    /// <summary>파일이 아직 없을 때만 값을 채운다. (프로필 도입 전 세이브에서 옮겨올 때)</summary>
    public static void SeedIfMissing(SettingsData seed)
    {
        if (seed == null || SaveIO.Exists(ProfileStore.SettingsPath)) return;
        _data = seed;
        Save();
    }
}

[Serializable]
public class SettingsData
{
    public const int CurrentVersion = 1;

    public int version;

    public float bgmVolume = 0.05f;
    public float effectVolume = 0.3f;

    /// <summary>교배 결과 팝업 표시.</summary>
    public bool showBreedPopup = false;

    /// <summary>이미 읽은 메시지도 알림음 재생.</summary>
    public bool playAlarmForSeenMessages = true;
}
