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
        // 식물별 특수 아이템은 이제 "그 식물로 새벽 unlockDawnStage(4/8/12) 클리어" 시 자동 해금된다.
        // (아래 [새벽] 섹션에서 각 식물 12단계로 올리면 해당 식물 특수 아이템이 전부 열림)
        if (GUILayout.Button("식물별 특수 아이템 해금 (양쪽 새벽 12단계)"))
        {
            foreach (var plant in DawnSystem.Plants)
            {
                DawnSystem.SetMaxUnlockedStage(plant, 13);       // 12단계 클리어 = 해금 13단계
                UnlockGrants.GrantDawnClearUnlocks(plant, 12);   // 상점+특수 아이템 실제 해금 기록
            }
            Debug.Log("[Debug] 양쪽 식물 새벽 12단계 클리어 처리 → 식물별 아이템 전부 해금");
        }
        GUILayout.Label("보유: " + (SpecialItemSystem.OwnedIds.Count > 0
            ? string.Join(", ", SpecialItemSystem.OwnedIds) : "없음"), wrapLabel);

        GUILayout.Space(8);

        // ── 변종 (악성/양성) ──
        GUILayout.Label("[변종]");
        GUILayout.Label(MutationDebug.HasChanceOverride
            ? $"발생 확률: {MutationDebug.ChanceOverride:0.#}% (강제)"
            : "발생 확률: 기본(1% + 새벽/저주)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("기본")) MutationDebug.ChanceOverride = -1f;
        if (GUILayout.Button("50%")) MutationDebug.ChanceOverride = 50f;
        if (GUILayout.Button("100%")) MutationDebug.ChanceOverride = 100f;
        GUILayout.EndHorizontal();

        GUILayout.Label(MutationDebug.HasRatioOverride
            ? $"악성 : 양성 = {MutationDebug.MalignantRatioOverride * 100f:0}% : {(1f - MutationDebug.MalignantRatioOverride) * 100f:0}% (강제)"
            : "악성 : 양성 = 80% : 20% (기본)");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("기본")) MutationDebug.MalignantRatioOverride = -1f;
        if (GUILayout.Button("악성만")) MutationDebug.MalignantRatioOverride = 1f;
        if (GUILayout.Button("양성만")) MutationDebug.MalignantRatioOverride = 0f;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── 경제 ──
        GUILayout.Label("[경제]");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("골드 +1000")) GameManager.Instance?.economyManager.AddGold(1000);
        if (GUILayout.Button("골드 +10000")) GameManager.Instance?.economyManager.AddGold(10000);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── 진행 / 스폰 ──
        GUILayout.Label("[진행 / 스폰]");
        var gm = GameManager.Instance;
        if (gm == null)
        {
            GUILayout.Label("게임 씬이 아닙니다.", wrapLabel);
        }
        else
        {
            GUILayout.Label($"현재 {gm.stage}일차 / 엔딩 {gm.EndStage}일차");
            if (GUILayout.Button($"{gm.EndStage}일차로 넘기기 (이번 날 끝나면 엔딩)"))
                gm.DebugSetStage(gm.EndStage);

            if (GUILayout.Button($"{gm.currentPlant} 1개 생성 (무작위 형질)"))
                gm.grid?.DebugSpawnRandomPlant();

            if (GUILayout.Button("벌레 1마리 소환"))
                gm.grid?.DebugSpawnBug();

            // 다음 웨이브 지정
            var ec = gm.enemyController;
            if (ec != null)
            {
                GUILayout.Label($"현재 웨이브: {ec.CurrentWave?.WaveType} → 다음: {ec.NextWave?.WaveType}", wrapLabel);
                int drawn = 0;
                foreach (WaveType wt in (WaveType[])System.Enum.GetValues(typeof(WaveType)))
                {
                    if (drawn % 3 == 0) GUILayout.BeginHorizontal();
                    if (GUILayout.Button(wt.ToString())) ec.DebugSetNextWave(wt);
                    drawn++;
                    if (drawn % 3 == 0) GUILayout.EndHorizontal();
                }
                if (drawn % 3 != 0) GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(8);

        // ── 새벽 / 도감 / 해금 ──
        GUILayout.Label("[새벽 / 도감 / 해금]");
        // 새벽 진행도는 식물별로 따로 관리된다
        GUILayout.Label($"기준 식물: {DawnSystem.CurrentPlant}", wrapLabel);
        foreach (var plant in DawnSystem.Plants)
        {
            GUILayout.Label($"[{plant}] 해금 {DawnSystem.GetMaxUnlockedStage(plant)}단계 / 클리어 {DawnSystem.GetMaxClearedStage(plant)}단계");
            GUILayout.BeginHorizontal();
            // "N클리어" = N단계까지 클리어 처리(선택 해금 N+1) + 조건 아이템 실제 해금
            if (GUILayout.Button("1클리어")) { DawnSystem.SetMaxUnlockedStage(plant, 2); UnlockGrants.GrantDawnClearUnlocks(plant, 1); }
            if (GUILayout.Button("12클리어")) { DawnSystem.SetMaxUnlockedStage(plant, 13); UnlockGrants.GrantDawnClearUnlocks(plant, 12); }
            if (GUILayout.Button("잠금")) DawnSystem.SetMaxUnlockedStage(plant, 0);
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("새벽 전체 초기화")) DawnSystem.ResetAllPlantProgress();

        // 인게임 사건 해금(황금 비료·급속 냉각기·냉각 방패·저항력 흡수 비료)
        GUILayout.Label("사건 해금: "
            + (UnlockManager.IsUnlocked(UnlockManager.Ids.GoldenPlantCreated) ? "황금식물 " : "")
            + (UnlockManager.IsUnlocked(UnlockManager.Ids.WinterReached) ? "겨울 " : "")
            + (UnlockManager.IsUnlocked(UnlockManager.Ids.FertilizerFourColumns) ? "비료4줄 " : ""), wrapLabel);
        if (GUILayout.Button("사건 해금 전부 적용"))
        {
            UnlockGrants.GrantEventUnlocks(UnlockManager.Ids.GoldenPlantCreated);
            UnlockGrants.GrantEventUnlocks(UnlockManager.Ids.WinterReached);
            UnlockGrants.GrantEventUnlocks(UnlockManager.Ids.FertilizerFourColumns);
            Debug.Log("[Debug] 사건 기반 해금 전부 적용");
        }
        if (GUILayout.Button("도감 진행 초기화")) CodexProgress.ResetAll();
        if (GUILayout.Button("아이템 해금 전부 초기화")) { UnlockManager.ResetAll(); Debug.Log("[Debug] 모든 해금 초기화"); }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
#endif
}
