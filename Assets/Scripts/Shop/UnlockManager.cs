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
    private static HashSet<string> _unlocked;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "unlocks.json");

    [Serializable]
    private class UnlockSaveData { public List<string> unlocked = new List<string>(); }

    private static void EnsureLoaded()
    {
        if (_unlocked != null) return;
        _unlocked = new HashSet<string>();
        try
        {
            if (File.Exists(FilePath))
            {
                var data = JsonUtility.FromJson<UnlockSaveData>(File.ReadAllText(FilePath));
                if (data != null && data.unlocked != null)
                    foreach (var id in data.unlocked) _unlocked.Add(id);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Unlock] 로드 실패: {e.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            var data = new UnlockSaveData { unlocked = new List<string>(_unlocked) };
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Unlock] 저장 실패: {e.Message}");
        }
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
