#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>해금 시스템 테스트용 에디터 메뉴.</summary>
public static class UnlockDebugMenu
{
    [MenuItem("Tools/Unlocks/Reset All (모두 잠금)")]
    public static void ResetAll()
    {
        UnlockManager.ResetAll();
        Debug.Log("[Unlock] 모든 해금 초기화됨.");
    }

    [MenuItem("Tools/Unlocks/Show Save Path")]
    public static void ShowPath()
    {
        Debug.Log($"[Unlock] 저장 경로: {System.IO.Path.Combine(Application.persistentDataPath, "unlocks.json")}");
    }
}
#endif
