using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 도감 진행(메타). 발견(사용/조우) 여부와 누적 통계를 게임오버/새 게임과 무관하게 영구 저장한다.
/// (<see cref="UnlockManager"/>와 동일한 메타 저장 패턴 — 별도 codex.json)
///
/// 발견: 카테고리별 id 집합. 미발견 항목은 도감에서 ??? 로 표시.
/// 통계: 문자열 키 → 누적 int (예: 판매한 완두콩 수, 벌레 종류별 잡은 수).
/// </summary>
public static class CodexProgress
{
    public enum Category { Item, Plant, Curse, Bug }

    // 통계 키 헬퍼
    public const string StatSoldPea = "sold_pea";
    public const string StatSoldPeanut = "sold_peanut";
    public const string StatBugKillTotal = "bugkill_total";
    public static string StatBugKill(string bugId) => "bugkill_" + bugId;

    private static HashSet<string> _discovered;   // "Item:id", "Bug:DefaultBug" ...
    private static Dictionary<string, int> _stats; // "sold_pea" -> 123
    private static bool _saveBlocked;              // 파일을 읽지 못했을 때 덮어쓰기 방지

    private static string FilePath => Path.Combine(Application.persistentDataPath, "codex.json");

    [Serializable]
    private class CodexSaveData
    {
        public List<string> discovered = new List<string>();
        public List<string> statKeys = new List<string>();
        public List<int> statValues = new List<int>();
    }

    private static void EnsureLoaded()
    {
        if (_discovered != null) return;
        _discovered = new HashSet<string>();
        _stats = new Dictionary<string, int>();
        if (!SaveIO.Exists(FilePath)) return;

        if (!SaveIO.TryRead(FilePath, out CodexSaveData data))
        {
            // 빈 도감으로 저장해 기존 기록을 덮지 않도록 이번 실행에서는 저장을 막는다.
            _saveBlocked = true;
            Debug.LogError($"[Codex] 도감을 읽지 못해 이번 실행에서는 저장하지 않습니다: {FilePath}");
            return;
        }

        if (data.discovered != null)
            foreach (var id in data.discovered) _discovered.Add(id);
        if (data.statKeys != null && data.statValues != null)
            for (int i = 0; i < data.statKeys.Count && i < data.statValues.Count; i++)
                _stats[data.statKeys[i]] = data.statValues[i];
    }

    private static void Save()
    {
        if (_saveBlocked) return;

        var data = new CodexSaveData { discovered = new List<string>(_discovered) };
        foreach (var kv in _stats)
        {
            data.statKeys.Add(kv.Key);
            data.statValues.Add(kv.Value);
        }
        SaveIO.Write(FilePath, data, pretty: false);
    }

    private static string Key(Category cat, string id) => $"{cat}:{id}";

    // ── 발견 ──────────────────────────────────────────────────────────────────
    public static void Discover(Category cat, string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        EnsureLoaded();
        if (_discovered.Add(Key(cat, id)))
        {
            Save();
            Debug.Log($"[Codex] 발견: {cat} {id}");
        }
    }

    public static bool IsDiscovered(Category cat, string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        EnsureLoaded();
        return _discovered.Contains(Key(cat, id));
    }

    // ── 통계 ──────────────────────────────────────────────────────────────────
    public static void AddStat(string key, int amount = 1)
    {
        if (string.IsNullOrEmpty(key) || amount == 0) return;
        EnsureLoaded();
        _stats.TryGetValue(key, out int cur);
        _stats[key] = cur + amount;
        Save();
    }

    public static int GetStat(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        EnsureLoaded();
        return _stats.TryGetValue(key, out int v) ? v : 0;
    }

    // ── 편의 메서드 ───────────────────────────────────────────────────────────
    /// <summary>벌레 처치: 종류별 + 전체 카운트 증가 + 발견 마킹.</summary>
    public static void AddBugKill(string bugId)
    {
        if (string.IsNullOrEmpty(bugId)) return;
        Discover(Category.Bug, bugId);
        AddStat(StatBugKill(bugId));
        AddStat(StatBugKillTotal);
    }

    /// <summary>완두콩/땅콩 판매 수 누적.</summary>
    public static void AddSold(bool isPea, int count = 1)
        => AddStat(isPea ? StatSoldPea : StatSoldPeanut, count);

    // ── 테스트/리셋용 ─────────────────────────────────────────────────────────
    public static void ResetAll()
    {
        _discovered = new HashSet<string>();
        _stats = new Dictionary<string, int>();
        _saveBlocked = false; // 명시적 초기화는 덮어쓰기가 의도다
        Save();
    }
}
