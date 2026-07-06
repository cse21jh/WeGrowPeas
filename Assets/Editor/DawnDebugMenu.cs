#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>새벽 모드 해금 상태 테스트용 메뉴.</summary>
public static class DawnDebugMenu
{
    [MenuItem("Tools/Unlocks/Dawn - Unlock All (12)")]
    public static void UnlockAll()
    {
        DawnSystem.UnlockUpTo(12);
        Debug.Log("[Dawn] 12단계까지 해금.");
    }

    [MenuItem("Tools/Unlocks/Dawn - Unlock First (1)")]
    public static void UnlockFirst()
    {
        DawnSystem.UnlockUpTo(1);
        Debug.Log("[Dawn] 1단계 해금(새벽 모드 ON).");
    }

    [MenuItem("Tools/Unlocks/Dawn - Lock (0)")]
    public static void Lock()
    {
        DawnSystem.MaxUnlockedDawnStage = 0;
        Debug.Log("[Dawn] 새벽 모드 잠금.");
    }
}
#endif
