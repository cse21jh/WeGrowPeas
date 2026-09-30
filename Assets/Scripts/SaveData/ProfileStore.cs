using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 세이브 파일 위치를 한 곳에서 정한다. 경로를 직접 조합하지 말고 여기를 거친다.
///
/// persistentDataPath/
///   settings.json          기기 설정(볼륨 등) — <see cref="DeviceSettings"/>
///   profiles.json          프로필 전역 정보(마지막 프로필 등) — <see cref="ProfilesGlobal"/>
///   Profiles/Profile_N/
///     profile.json         계정 진행 — <see cref="ProfileData"/>
///     codex.json           도감 — <see cref="CodexProgress"/>
///     Recall/              회상 기록 — <see cref="RecallStore"/>
///     UserData_0~2.json    런 세이브 슬롯 — <see cref="RunSave"/>
/// </summary>
public static class ProfileStore
{
    public const int ProfileCount = 3;

    /// <summary>지금 플레이 중인 프로필 번호(0부터).</summary>
    public static int CurrentIndex { get; private set; }

    public static string RootPath => Application.persistentDataPath;
    public static string SettingsPath => Path.Combine(RootPath, "settings.json");
    public static string GlobalPath => Path.Combine(RootPath, "profiles.json");

    public static string DirOf(int index) => Path.Combine(RootPath, "Profiles", $"Profile_{index}");
    public static string CurrentDir => DirOf(CurrentIndex);

    /// <summary>현재 프로필 폴더 안의 파일 경로.</summary>
    public static string PathOf(string fileName) => Path.Combine(CurrentDir, fileName);

    public static string ProfileFileOf(int index) => Path.Combine(DirOf(index), "profile.json");
    public static string CurrentProfileFile => ProfileFileOf(CurrentIndex);

    // ── 전역 정보 ─────────────────────────────────────────────────────────────

    private static ProfilesGlobal _global;

    public static ProfilesGlobal Global
    {
        get
        {
            if (_global == null && !SaveIO.TryRead(GlobalPath, out _global))
                _global = new ProfilesGlobal();
            return _global;
        }
    }

    public static void SaveGlobal()
    {
        Global.version = ProfilesGlobal.CurrentVersion;
        SaveIO.Write(GlobalPath, Global);
    }

    /// <summary>게임을 켤 때 마지막으로 플레이한 프로필을 현재 프로필로 잡는다.</summary>
    public static void SelectLastProfile()
    {
        CurrentIndex = Mathf.Clamp(Global.lastProfileIndex, 0, ProfileCount - 1);
    }
}

/// <summary>프로필 전역 정보(profiles.json).</summary>
[Serializable]
public class ProfilesGlobal
{
    public const int CurrentVersion = 1;

    public int version;

    /// <summary>마지막으로 플레이한 프로필. 게임을 켜면 이 프로필을 불러온다.</summary>
    public int lastProfileIndex;

    /// <summary>프로필 도입 전 세이브를 Profile_0으로 옮겼는가. 한 번만 옮긴다.</summary>
    public bool legacyMigrated;
}
