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

    /// <summary>
    /// 현재 프로필 번호를 바꾸고 "마지막 프로필"로 기록한다.
    /// 메모리 상태를 바꾸지는 않으므로 직접 부르지 말고 <see cref="SaveManager.SwitchProfile"/>를 쓴다.
    /// </summary>
    public static void SetCurrent(int index)
    {
        CurrentIndex = index;
        Global.lastProfileIndex = index;
        SaveGlobal();
    }

    // ── 선택 화면용 ───────────────────────────────────────────────────────────

    public static bool IsValidIndex(int index) => index >= 0 && index < ProfileCount;

    /// <summary>프로필에 진행이 저장돼 있는가. 없으면 선택 화면에 "비어있음".</summary>
    public static bool HasData(int index) => SaveIO.Exists(ProfileFileOf(index));

    /// <summary>선택 화면 카드에 보여줄 정보. 비었거나 읽을 수 없으면 exists = false.</summary>
    public static ProfileSummary GetSummary(int index)
    {
        var summary = new ProfileSummary { index = index };
        if (SaveIO.TryRead(ProfileFileOf(index), out ProfileData data))
        {
            summary.exists = true;
            summary.lastSavedUnix = data.meta.lastSavedUnix;
            summary.playTimeSeconds = data.meta.playTimeSeconds;
        }

        // 현재 프로필은 아직 파일에 쓰지 않은 플레이 시간까지 보여준다.
        if (summary.exists && index == CurrentIndex && SaveManager.Instance != null)
            summary.playTimeSeconds = SaveManager.Instance.PlayTimeSeconds;

        return summary;
    }

    /// <summary>
    /// 프로필 폴더를 통째로 지운다(진행·도감·회상·런 세이브 전부).
    /// 메모리 상태는 건드리지 않으므로 직접 부르지 말고 <see cref="SaveManager.DeleteProfile"/>를 쓴다.
    /// </summary>
    public static bool DeleteFiles(int index)
    {
        string dir = DirOf(index);
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ProfileStore] 프로필 삭제 실패: {dir}\n{e}");
            return false;
        }
    }
}

/// <summary>프로필 선택 화면 카드 1장의 정보.</summary>
public struct ProfileSummary
{
    public int index;
    public bool exists;

    /// <summary>마지막 저장 시각(UTC, 유닉스 초).</summary>
    public long lastSavedUnix;

    /// <summary>누적 플레이 시간(초).</summary>
    public double playTimeSeconds;

    public DateTime LastSavedLocal => DateTimeOffset.FromUnixTimeSeconds(lastSavedUnix).LocalDateTime;

    /// <summary>"110:41:36" 형식(시간은 24를 넘어도 그대로 센다).</summary>
    public string PlayTimeText
    {
        get
        {
            long total = (long)Math.Max(0, playTimeSeconds);
            return $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}";
        }
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
