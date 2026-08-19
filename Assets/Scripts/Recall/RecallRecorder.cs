using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회상용 일자별 스냅샷을 이번 런 동안 모아 둔다.
///
/// 자유시간이 끝난 시점(<c>GameManager.StartStage</c> 끝)마다 <see cref="CaptureDay"/>가 한 번 불린다.
/// 모인 기록은 런 세이브(<see cref="SaveData.recall"/>)에 실리므로 저장 후 종료했다가 이어해도
/// 타임라인이 끊기지 않는다. 런이 끝나면(엔딩/게임오버) 별도 회상 파일로 확정 복사된다.
///
/// 아이콘·설명은 표시할 때 id로 다시 조회하므로 여기서는 id와 수치만 모은다.
/// (<see cref="PlayerRecordForGraph"/>와 같은 static 누적 + SaveTo/LoadFromSave 패턴)
/// </summary>
public static class RecallRecorder
{
    private static List<DaySnapshot> _days = new List<DaySnapshot>();

    /// <summary>이번 런에 쌓인 일자별 스냅샷 (일차 오름차순).</summary>
    public static IReadOnlyList<DaySnapshot> Days => _days;

    /// <summary>새 게임 시작 시 초기화.</summary>
    public static void ResetRun()
    {
        _days = new List<DaySnapshot>();
    }

    /// <summary>
    /// 오늘 하루가 끝난 시점의 농장 상태를 찍는다.
    /// 같은 일차가 이미 있으면 덮어쓴다(게임오버 당일 재캡처 등).
    /// </summary>
    /// <param name="isFinalPartial">자유시간을 채우지 못하고 끝난 날(게임오버 당일)인가.</param>
    public static void CaptureDay(bool isFinalPartial = false)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        var snap = new DaySnapshot
        {
            day = gm.stage,
            isFinalPartial = isFinalPartial
        };

        // ── 골드 / 판매 누적 ──────────────────────────────────────────────
        if (gm.economyManager != null)
        {
            snap.gold = gm.economyManager.GetGold();
            snap.earnedGold = gm.economyManager.EarnedGoldToday;
            snap.cumSellCount = gm.economyManager.PeaSellCount + gm.economyManager.PeanutSellCount;
        }

        // ── 웨이브 / 사망 수 ──────────────────────────────────────────────
        // 두 기록 모두 일차를 인덱스로 쓴다(EnemyController). 범위를 벗어나면 기본값.
        if (gm.enemyController != null)
        {
            var waves = gm.enemyController.StageWaveRecord;
            var kills = gm.enemyController.StageKillRecord;

            if (waves != null && snap.day >= 0 && snap.day < waves.Count)
                snap.waveType = waves[snap.day];
            if (kills != null && snap.day >= 0 && snap.day < kills.Count)
                snap.diedCount = kills[snap.day];
        }

        // ── 밭 (칸별 식물 종) ─────────────────────────────────────────────
        if (gm.grid != null)
        {
            snap.maxCol = gm.grid.GetMaxCol();
            snap.cumBreedCount = gm.grid.totalBreedCount;

            int cells = Mathf.Max(0, snap.maxCol * 4);
            var species = new string[cells];
            for (int i = 0; i < cells; i++)
            {
                species[i] = gm.grid.plantGrid.TryGetValue(i, out Plant p) && p != null
                    ? p.speciesname
                    : string.Empty;
            }
            snap.cellSpecies = species;
        }

        // ── 상점 누적 구매 (전날과의 차이가 그날 구매분) ──────────────────
        if (ShopManager.Instance != null && ShopManager.Instance.PurchaseHistory != null)
        {
            var names = new List<string>();
            var counts = new List<int>();
            foreach (var kv in ShopManager.Instance.PurchaseHistory)
            {
                if (kv.Value <= 0) continue;
                names.Add(kv.Key);
                counts.Add(kv.Value);
            }
            snap.itemNames = names.ToArray();
            snap.itemCounts = counts.ToArray();
        }

        // ── 특수 아이템 ───────────────────────────────────────────────────
        if (SpecialItemSystem.OwnedIds != null)
        {
            snap.specialItemIds = new List<string>(SpecialItemSystem.OwnedIds).ToArray();
        }

        // ── 그날 효과가 적용된 저주 ───────────────────────────────────────
        CurseManager cm = gm.curseManager != null ? gm.curseManager : CurseManager.Instance;
        if (cm != null)
        {
            var curseIds = new List<string>();
            if (cm.currentTempCurse != null && cm.currentTempCurse.Data != null)
                curseIds.Add(cm.currentTempCurse.Data.curseId);
            if (cm.currentSeasonCurse != null && cm.currentSeasonCurse.Data != null)
                curseIds.Add(cm.currentSeasonCurse.Data.curseId);
            snap.curseIds = curseIds.ToArray();
        }

        int existing = _days.FindIndex(d => d.day == snap.day);
        if (existing >= 0) _days[existing] = snap;
        else _days.Add(snap);
    }

    /// <summary>모아 둔 스냅샷을 저장 데이터에 담는다. <see cref="LoadFromSave"/>와 짝.</summary>
    public static void SaveTo(RecallSave save)
    {
        if (save == null) return;
        save.days = _days;
    }

    public static void LoadFromSave(RecallSave saveData)
    {
        _days = (saveData != null && saveData.days != null) ? saveData.days : new List<DaySnapshot>();
    }
}
