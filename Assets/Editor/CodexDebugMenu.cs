using UnityEditor;
using UnityEngine;

/// <summary>도감 진행(발견/통계) 테스트용 메뉴.</summary>
public static class CodexDebugMenu
{
    [MenuItem("Tools/Codex/Reset Progress (발견·통계 초기화)")]
    public static void Reset()
    {
        CodexProgress.ResetAll();
        Debug.Log("[Codex] 진행 초기화 완료");
    }
}
