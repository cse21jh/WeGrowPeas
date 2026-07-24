using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 아이템 해금 상태 관리(메타 진행). 해금은 런/새 게임/게임오버와 무관하게 영구 저장된다.
/// (게임오버 시 일반 세이브는 삭제되지만 해금은 별도 파일에 유지)
///
/// 사용:
///   UnlockManager.Unlock(item);             // 또는 Unlock("아이템id")
///   bool ok = UnlockManager.IsAvailable(item); // 상점 노출 가능 여부
/// </summary>
public static class UnlockManager
{
    /// <summary>
    /// 인게임 사건으로 해금되는 아이템의 해금 id. (사건 발생 시 UnlockGrants.GrantEventUnlocks가 기록)
    /// </summary>
    public static class Ids
    {
        /// <summary>"황금 식물"을 최초 1회 만들었을 때 — 황금 비료</summary>
        public const string GoldenPlantCreated = "event_golden_plant_created";
        /// <summary>겨울에 최초로 도달했을 때 — 급속 냉각기 / 냉각 방패</summary>
        public const string WinterReached = "event_winter_reached";
        /// <summary>토양에 전용 비료가 4줄 이상 존재했을 때 — 저항력 흡수 비료</summary>
        public const string FertilizerFourColumns = "event_fertilizer_four_columns";
    }

    /// <summary>인게임 사건 해금 id를 도감/안내용 한국어 설명으로 변환.</summary>
    public static string GetEventDescription(string eventId)
    {
        if (eventId == Ids.GoldenPlantCreated) return "황금 식물을 처음 만들면 해금됩니다.";
        if (eventId == Ids.WinterReached) return "겨울에 처음 도달하면 해금됩니다.";
        if (eventId == Ids.FertilizerFourColumns) return "전용 비료를 4줄 이상 설치하면 해금됩니다.";
        return "특정 조건을 만족하면 해금됩니다.";
    }

    private static HashSet<string> _unlocked;

    public static List<string> GetUnlockedList()
    {
        EnsureLoaded();
        return new List<string>(_unlocked);
    }

    public static void SetUnlockedList(List<string> list)
    {
        if (list == null)
        {
            _unlocked = new HashSet<string>();
        }
        else
        {
            _unlocked = new HashSet<string>(list);
        }
    }

    private static void EnsureLoaded()
    {
        if (_unlocked == null)
            _unlocked = new HashSet<string>();
    }

    private static void Save()
    {
        // 글로벌 프로필 저장은 게임 종료 시(SaveManager)에 일괄 수행됩니다.
    }

    // ── 조회 ──────────────────────────────────────────────────────────────────
    public static bool IsUnlocked(string id)
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(id) && _unlocked.Contains(id);
    }

    /// <summary>상점에 노출 가능한가: 해금이 필요 없거나, 이미 해금됨.</summary>
    public static bool IsAvailable(ItemData item)
    {
        if (item == null) return false;
        if (!item.requiresUnlock) return true;
        return IsUnlocked(item.UnlockId);
    }

    // ── 해금 ──────────────────────────────────────────────────────────────────
    public static void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        EnsureLoaded();
        if (_unlocked.Add(id))
        {
            Save();
            Debug.Log($"[Unlock] 해금: {id}");
        }
    }

    public static void Unlock(ItemData item)
    {
        if (item != null) Unlock(item.UnlockId);
    }

    // ── 테스트/리셋용 ─────────────────────────────────────────────────────────
    public static void Lock(string id)
    {
        EnsureLoaded();
        if (!string.IsNullOrEmpty(id) && _unlocked.Remove(id)) Save();
    }

    public static void ResetAll()
    {
        _unlocked = new HashSet<string>();
        Save();
    }
}
