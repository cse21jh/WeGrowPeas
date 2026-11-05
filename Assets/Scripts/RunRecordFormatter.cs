using UnityEngine;

/// <summary>
/// 런 요약(<see cref="RunSummary"/>)을 결과 화면 문구로 바꾼다.
///
/// 결과 화면과 회상이 같은 문구를 쓰도록 표시 로직만 여기 모았다.
/// 이 클래스는 값을 읽기만 하며 게임 상태를 바꾸지 않는다 —
/// 유일한 예외인 <see cref="AwardGenetics"/>는 런이 끝나는 지점에서만 불린다.
/// </summary>
public static class RunRecordFormatter
{
    private const int RankCount = 4; // 번역 표의 game.ending.rank0~3

    /// <summary>등급을 표에 넣을 수 있는 범위로 자른다.</summary>
    private static int ClampRank(int rank) => Mathf.Clamp(rank, 0, RankCount - 1);

    /// <summary>등급별 완두콩 표정 스프라이트 인덱스. 등급이 높을수록 앞쪽 그림.</summary>
    public static int GetPeaEmotionSpriteIndex(int rank) => (RankCount - 1) - ClampRank(rank);

    /// <summary>등급별 유전자 토큰 배율.</summary>
    public static float GetGeneticsMultiplier(int rank)
    {
        switch (ClampRank(rank))
        {
            case 0: return 0.75f;
            case 3: return 1.25f;
            default: return 1.0f;
        }
    }

    // ── 문구 ──────────────────────────────────────────────────────────────────

    public static string BuildEndingText(RunSummary s)
        => Loc.T("game.ending.result", ("days", s.maxStageReached), ("ending", Loc.T($"game.ending.rank{ClampRank(s.playerRank)}")));

    public static string BuildDaysLine(RunSummary s)
        => Loc.T("game.ending.days", ("days", s.maxStageReached));

    public static string BuildStatsLine(RunSummary s)
    {
        return Loc.T("game.ending.stats",
            ("total", s.totalPeas), ("sold", s.soldPeas), ("bugs", s.totalBugsKilled),
            ("earned", s.totalGoldEarned), ("spent", s.totalGoldSpend), ("best", s.mostExpensivePlant),
            ("helped", s.completeRequestCount), ("genes", s.totalGenetics));
    }

    public static string BuildFarmNoteLine(RunSummary s)
    {
        string wave = s.mostKilledWave;
        string plant = RecallLookup.Plant(s.mostSellPlantName).name;

        return string.IsNullOrEmpty(s.popularItemName)
            ? Loc.T("game.ending.note", ("wave", wave), ("plant", plant))
            : Loc.T("game.ending.note_item", ("wave", wave), ("item", RecallLookup.Item(s.popularItemName).name), ("plant", plant));
    }

    // ── 지급 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 이번 런의 유전자 토큰을 지급한다. 런이 끝나는 지점에서 딱 한 번만 부른다.
    ///
    /// 결과 화면이 아니라 <see cref="GameManager"/>에서 부르는 이유:
    /// 결과 화면 UI가 켜질 때마다 지급하면 회상으로 같은 화면을 다시 볼 때 또 지급된다.
    /// 지급 결과(<see cref="RunSummary.totalGenetics"/>)는 회상 기록에도 그대로 남는다.
    /// </summary>
    public static void AwardGenetics(RunSummary s)
    {
        if (AbilityManager.Instance == null)
        {
            Debug.Log("Ability Manager가 없습니다");
            return;
        }

        int g = s.totalGoldEarned / 500;
        AbilityManager.Instance.AddGeneStorage((int)(g * GetGeneticsMultiplier(s.playerRank)));
        AbilityManager.Instance.AddGenetics(); // 내부에서 GameRecordHolder.SaveGenetics 호출
    }
}
