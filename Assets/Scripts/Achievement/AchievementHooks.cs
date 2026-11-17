using UnityEngine;

/// <summary>
/// 게임 진행 중 조건을 확인해 Manual 업적을 달성 처리한다.
/// 누적 통계로 판정하는 업적(벌레 처치 등)은 에셋만으로 동작하므로 여기 없다.
/// id는 Steam 업적 이름과 같으므로 바꾸면 기존 달성 기록과 연결이 끊긴다.
/// </summary>
public static class AchievementHooks
{
    public const string GoldenPeaNoSoil = "GOLDEN_PEA_NO_SOIL";
    public const string GoldenPeanutNoSoil = "GOLDEN_PEANUT_NO_SOIL";
    public const string Dawn12Pea = "DAWN12_PEA";
    public const string Dawn12PeaNoSkill = "DAWN12_PEA_NO_SKILL";
    public const string Dawn12Peanut = "DAWN12_PEANUT";
    public const string Dawn12PeanutNoSkill = "DAWN12_PEANUT_NO_SKILL";
    public const string FullFieldNoLoss = "FULL_FIELD_NO_LOSS";
    public const string SellOver2000 = "SELL_2000";

    public const int DawnTargetStage = 12;
    public const int SellTargetPrice = 2000;

    private static readonly (int days, string id)[] SurviveTargets =
    {
        (10, "SURVIVE_10"), (20, "SURVIVE_20"), (30, "SURVIVE_30"), (40, "SURVIVE_40"),
    };

    private static readonly (int gold, string id)[] ClearGoldTargets =
    {
        (10000, "CLEAR_GOLD_10K"), (40000, "CLEAR_GOLD_40K"), (70000, "CLEAR_GOLD_70K"), (100000, "CLEAR_GOLD_100K"),
    };

    /// <summary>튜토리얼에서는 업적을 주지 않는다.</summary>
    private static bool InRun => GameManager.Instance != null && GameManager.Instance.Tutorial == null;

    /// <summary>엔딩(일반·새벽 모두)에 도달했을 때. 보유 골드는 세금 처리 뒤 값이다.</summary>
    public static void OnRunCleared(int gold)
    {
        if (!InRun) return;
        foreach (var (target, id) in ClearGoldTargets)
            if (gold >= target) AchievementSystem.Unlock(id);
    }

    /// <summary>새벽 단계를 클리어했을 때. 특성은 이번 런에 장착한 식물·공통 특성을 본다.</summary>
    public static void OnDawnCleared(string plant, int stage)
    {
        if (!InRun || stage < DawnTargetStage) return;

        var abilities = AbilityManager.Instance;
        bool noSkill = abilities != null
            && abilities.CurrentPlantAbility.Count == 0
            && abilities.CurrentGeneralAbility.Count == 0;

        if (plant == PlantIds.Pea)
        {
            AchievementSystem.Unlock(Dawn12Pea);
            if (noSkill) AchievementSystem.Unlock(Dawn12PeaNoSkill);
        }
        else if (plant == PlantIds.Peanut)
        {
            AchievementSystem.Unlock(Dawn12Peanut);
            if (noSkill) AchievementSystem.Unlock(Dawn12PeanutNoSkill);
        }
    }

    /// <summary>삽으로 식물 하나를 팔았을 때. 판매 가격만 보고 사망 보너스는 넣지 않는다.</summary>
    public static void OnPlantSold(int sellingPrice)
    {
        if (InRun && sellingPrice >= SellTargetPrice) AchievementSystem.Unlock(SellOver2000);
    }

    /// <summary>
    /// 황금 식물이 됐을 때. 황금 토양은 저항력만 올리고 황금 판정에는 쓰이지 않으므로,
    /// "황금 비료 없이"는 그 순간 밭에 황금 토양이 하나도 없는 경우로 본다.
    /// </summary>
    public static void OnGoldenPlant(Plant plant)
    {
        if (!InRun || plant == null) return;
        var grid = GameManager.Instance.grid;
        if (grid == null || grid.Soil.GoldSoilTiles.Count > 0) return;

        if (plant.speciesname == PlantIds.Pea) AchievementSystem.Unlock(GoldenPeaNoSoil);
        else if (plant.speciesname == PlantIds.Peanut) AchievementSystem.Unlock(GoldenPeanutNoSoil);
    }

    /// <summary>하루가 끝났을 때. 밭에 가장 오래 있던 식물의 일수로 확인한다.</summary>
    public static void OnDayEnded(PlantBoard board)
    {
        if (!InRun || board == null) return;

        int longest = 0;
        foreach (var plant in board.Plants.Values)
            if (plant != null && !plant.isDying) longest = Mathf.Max(longest, plant.DaysOnField);

        foreach (var (target, id) in SurviveTargets)
            if (longest >= target) AchievementSystem.Unlock(id);
    }

    /// <summary>모든 칸이 찬 상태로 시작한 웨이브가 끝났을 때. 웨이브 동안 식물이 하나도 사라지지 않았으면 달성.</summary>
    public static void OnFullFieldWaveEnded(int plantsBefore, int plantsAfter)
    {
        if (InRun && plantsBefore > 0 && plantsAfter >= plantsBefore) AchievementSystem.Unlock(FullFieldNoLoss);
    }
}
