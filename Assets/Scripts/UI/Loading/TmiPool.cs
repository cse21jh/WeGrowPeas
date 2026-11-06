using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로딩창 TMI 문구 접근자.
/// 우선순위: Resources의 <see cref="TmiConfig"/> 에셋 → (없으면) 코드 내장 기본값.
/// (WaveSchedule·TaxSchedule과 동일한 구조)
/// </summary>
public static class TmiPool
{
    public const string ResourcePath = "Data/TmiConfig";

    // 에셋이 없을 때 쓰는 폴백 겸, 에디터가 에셋을 만들 때의 시드.
    private static bool _loaded;
    private static string _language;
    private static List<string> _tips;
    private static int _lastIndex = -1;

    private static void EnsureLoaded()
    {
        if (_loaded && _language == Loc.Language) return;
        _loaded = true;
        _language = Loc.Language;

        var cfg = Resources.Load<TmiConfig>(ResourcePath);
        _tips = cfg != null ? cfg.GetLocalizedTips() : new List<string>();
        if (_tips.Count == 0)
            Debug.LogWarning($"[Tmi] Resources/{ResourcePath} 또는 번역 표에 문구가 없습니다.");
    }

    public static void Reload()
    {
        _loaded = false;
        _lastIndex = -1;
        EnsureLoaded();
    }

    public static int Count { get { EnsureLoaded(); return _tips.Count; } }

    /// <summary>무작위 문구 하나. 직전에 보여준 것과는 겹치지 않게 고른다.</summary>
    public static string GetRandom()
    {
        EnsureLoaded();
        if (_tips.Count == 0) return string.Empty;
        if (_tips.Count == 1) return _tips[0];

        int idx;
        do { idx = Random.Range(0, _tips.Count); }
        while (idx == _lastIndex);

        _lastIndex = idx;
        return _tips[idx];
    }
}
