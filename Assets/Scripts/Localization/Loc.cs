using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 게임 텍스트 조회 창구. 표는 Resources/Localization/*.csv 이고, 처음 접근할 때 읽으므로
/// 매니저 초기화 순서와 무관하다.
///
///   Loc.T("shop.error.cannot_buy")                 키로 찾는다. 없으면 키를 그대로 보여준다.
///   Loc.T("day.label", ("day", 3))                 자리표시자를 채운다. 문법은 LocFormat 참고.
///   Loc.Or("item.GoldenClock.name", DisplayName)   없으면 기존 문자열을 쓴다. 옮기는 도중에 쓴다.
///
/// 게임 로직의 식별자(currentPlant, speciesname, 구매 이력 키 등)는 세이브에 저장되므로
/// 번역하지 않는다. 화면에 보여주는 지점만 이 클래스를 거치게 한다.
/// </summary>
public static class Loc
{
    private const string ResourcePath = "Localization";

    private static LocDatabase _database;
    private static string _language;
    private static readonly HashSet<string> WarnedKeys = new HashSet<string>();

    /// <summary>언어가 바뀌면 호출된다. 화면에 떠 있는 텍스트는 여기서 다시 그린다.</summary>
    public static event Action LanguageChanged;

    public static string Language
    {
        get
        {
            EnsureLoaded();
            return _language;
        }
    }

    public static IReadOnlyList<string> AvailableLanguages
    {
        get
        {
            EnsureLoaded();
            return _database.Languages;
        }
    }

    public static bool Has(string key)
    {
        EnsureLoaded();
        return _database.Contains(key);
    }

    public static string T(string key) => Lookup(key) ?? ReportMissing(key);

    public static string T(string key, params (string name, object value)[] args)
        => LocFormat.Format(T(key), args);

    public static string Or(string key, string fallback) => Lookup(key) ?? fallback;

    public static string Or(string key, string fallback, params (string name, object value)[] args)
        => LocFormat.Format(Or(key, fallback), args);

    /// <summary>언어를 바꾼다. save가 false면 이번 실행에만 적용한다. (에디터 미리보기용)</summary>
    public static bool SetLanguage(string language, bool save = true)
    {
        EnsureLoaded();

        if (!_database.Languages.Contains(language))
        {
            Debug.LogWarning($"[Loc] 표에 없는 언어입니다: {language}");
            return false;
        }

        _language = language;

        if (save)
        {
            DeviceSettings.Data.language = language;
            DeviceSettings.Save();
        }

        LanguageChanged?.Invoke();
        return true;
    }

    /// <summary>표를 다시 읽는다. 플레이 중 표를 고쳤을 때 쓴다.</summary>
    public static void Reload()
    {
        Load();
        LanguageChanged?.Invoke();
    }

    // 도메인 리로드를 꺼도 플레이마다 표를 새로 읽도록 정적 상태를 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _database = null;
        _language = null;
        LanguageChanged = null;
        WarnedKeys.Clear();
    }

    private static void EnsureLoaded()
    {
        if (_database == null) Load();
    }

    private static void Load()
    {
        var database = new LocDatabase();

        // 키가 겹쳤을 때 결과가 매번 같도록 파일 이름순으로 읽는다.
        foreach (var asset in Resources.LoadAll<TextAsset>(ResourcePath).OrderBy(a => a.name, StringComparer.Ordinal))
            database.Add(LocTable.Parse(asset.name, asset.text));

        LogIssues(database);

        _database = database;

        // 저장된 언어가 표에서 빠졌더라도 설정값은 지우지 않는다. 표가 돌아오면 다시 쓰인다.
        string saved = DeviceSettings.Data.language;
        _language = database.Languages.Contains(saved) ? saved : LocTable.SourceLanguage;
    }

    private static string Lookup(string key)
    {
        EnsureLoaded();
        return _database.TryGet(key, _language, out string text) ? text : null;
    }

    private static string ReportMissing(string key)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (WarnedKeys.Add(key ?? "")) Debug.LogWarning($"[Loc] 표에 없는 키: '{key}'");
#endif
        return key ?? "";
    }

    private static void LogIssues(LocDatabase database)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        foreach (var issue in database.Issues)
        {
            if (issue.Level == LocIssueLevel.Error) Debug.LogError($"[Loc] {issue}");
            else Debug.LogWarning($"[Loc] {issue}");
        }
#endif
    }
}
