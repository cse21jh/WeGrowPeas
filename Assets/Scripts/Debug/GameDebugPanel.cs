using UnityEngine;

/// <summary>
/// 인게임 통합 디버그 패널 (F11 토글).
/// - 씬 배치 불필요: 게임 시작 시 자동 생성 (에디터/개발 빌드에서만).
/// - 특수 아이템 선물 지급, 언락, 골드 치트 등 테스트 기능 모음.
/// - 기존 디버그 키: F8 도감, F9 저주 상태 오버레이, F10 저주 강제 패널.
/// </summary>
public class GameDebugPanel : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const KeyCode ToggleKey = KeyCode.F11;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (FindObjectOfType<GameDebugPanel>() != null) return;
        var go = new GameObject("GameDebugPanel(Debug)");
        go.AddComponent<GameDebugPanel>();
        DontDestroyOnLoad(go);
    }

    private bool show;
    private Vector2 scroll;
    private GUIStyle wrapLabel;

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) show = !show;
    }

    private void OnGUI()
    {
        if (!show) return;

        if (wrapLabel == null)
            wrapLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };

        GUILayout.BeginArea(new Rect(Screen.width - 330, 10, 320, Screen.height - 20), GUI.skin.box);
        GUILayout.Label("── 디버그 패널 (F11) ──");
        GUILayout.Label("F8 도감 · F9 저주 상태 · F10 저주 강제", wrapLabel);

        scroll = GUILayout.BeginScrollView(scroll);

        // ── 특수 아이템 ──
        GUILayout.Label("[특수 아이템]");
        GUILayout.Label($"미수령 선물: {SpecialItemSystem.PendingGifts}개");
        if (GUILayout.Button("선물 +1")) SpecialItemSystem.AddGift();
        if (GUILayout.Button("식물별 아이템 전부 언락"))
        {
            foreach (var d in Resources.LoadAll<SpecialItemData>(SpecialItemSystem.ResourcePath))
                if (d != null && d.plantSpecific)
                    UnlockManager.Unlock(d.UnlockId);
            Debug.Log("[Debug] 식물별 특수 아이템 전부 언락");
        }
        GUILayout.Label("보유: " + (SpecialItemSystem.OwnedIds.Count > 0
            ? string.Join(", ", SpecialItemSystem.OwnedIds) : "없음"), wrapLabel);

        GUILayout.Space(8);

        // ── 경제 ──
        GUILayout.Label("[경제]");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("골드 +1000")) GameManager.Instance?.economyManager.AddGold(1000);
        if (GUILayout.Button("골드 +10000")) GameManager.Instance?.economyManager.AddGold(10000);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── 새벽 / 도감 / 해금 ──
        GUILayout.Label("[새벽 / 도감 / 해금]");
        GUILayout.Label($"새벽 해금 최대 단계: {DawnSystem.MaxUnlockedDawnStage}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("새벽 1단계")) DawnSystem.UnlockUpTo(1);
        if (GUILayout.Button("새벽 12단계")) DawnSystem.UnlockUpTo(12);
        if (GUILayout.Button("새벽 잠금")) DawnSystem.MaxUnlockedDawnStage = 0;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("도감 진행 초기화")) CodexProgress.ResetAll();
        if (GUILayout.Button("아이템 해금 전부 초기화")) { UnlockManager.ResetAll(); Debug.Log("[Debug] 모든 해금 초기화"); }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
#endif
}
