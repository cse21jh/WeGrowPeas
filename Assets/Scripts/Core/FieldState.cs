/// <summary>
/// 밭 규칙이 참조하는 상태 묶음. 칸과 식물, 보정치, 땅 효과.
/// 게임에서는 <see cref="Grid"/>가 자기 것을 묶어 내주고, 시뮬레이터는 씬 없이 직접 만든다.
/// </summary>
public sealed class FieldState
{
    public FieldState(PlantBoard board, GridModifiers modifiers, SoilState soil)
    {
        Board = board;
        Modifiers = modifiers;
        Soil = soil;
    }

    /// <summary>씬 없이 빈 밭을 만든다.</summary>
    public static FieldState CreateEmpty()
    {
        var board = new PlantBoard();
        var modifiers = new GridModifiers();
        return new FieldState(board, modifiers, new SoilState(board, modifiers));
    }

    public PlantBoard Board { get; }
    public GridModifiers Modifiers { get; }
    public SoilState Soil { get; }

    /// <summary>튜토리얼에서는 마지막 남은 식물이 죽지 않는다.</summary>
    public bool IsTutorial { get; set; }

    // ───── 심기 ─────

    /// <summary>심을 칸을 고른다. 원하는 칸이 비어 있으면 그 칸, 아니면 앞에서부터 첫 빈 칸. 없으면 -1.</summary>
    public int FindCellFor(int preferredIdx = -1)
    {
        if (preferredIdx != -1 && !Board.States.ContainsKey(preferredIdx))
            return preferredIdx;

        for (int idx = 0; idx < Board.MaxCol * PlantBoard.RowCount; idx++)
        {
            if (!Board.States.ContainsKey(idx))
                return idx;
        }
        return -1;
    }

    /// <summary>
    /// 씬 없이 새 식물을 심는다. 자리가 없으면 false. (시뮬레이터용)
    /// 게임에서는 Grid.AddPlantToGrid와 Grid.PlantPlant가 같은 순서로 같은 함수들을 부르고, 사이에 화면 처리를 넣는다.
    /// 순서를 바꿀 때는 양쪽을 함께 바꾼다.
    /// </summary>
    public bool AddPlant(PlantState plant, int preferredIdx = -1)
    {
        int idx = FindCellFor(preferredIdx);
        if (idx < 0) return false;

        plant.OnPlanted(this, idx);
        Board.PlaceState(idx, plant);
        plant.TryRootByDawn();
        Board.NotifyChanged();
        return true;
    }

    /// <summary>특수(신상품): 새 식물이 심겼을 때 받는 골드.</summary>
    public static int GetPlantingGold(PlantState plant)
        => SpecialItemSystem.Has("new_product") ? (int)System.Math.Round(plant.GetSellingPrice() * 0.5f) : 0;

    // ───── 죽음 ─────

    /// <summary>
    /// 식물을 죽이려 시도한다. 막히지 않으면 밭에서 빼고 사망 보너스 골드를 계산해 돌려준다.
    /// 골드 지급과 연출은 호출한 쪽이 한다.
    /// </summary>
    /// <param name="ladybugCanIntercept">벌레가 죽이려는데 밭에 무당벌레가 있어 대신 막아 줄 수 있는가.</param>
    public DeathOutcome TryKill(PlantState plant, DeathCause cause, bool ladybugCanIntercept = false)
    {
        int idx = plant.GridIndex;

        if (IsTutorial && Board.CountLivingPlants() <= 1)
            return DeathOutcome.Blocked(DeathResult.BlockedLastPlant);

        // 페트병이 얼어 있는지보다 먼저 판정된다. 얼어 있어도 보호 횟수는 줄어든다.
        if (Soil.TryBlockDeathWithPetBottle(idx, cause, out bool petBottleRemoved))
            return new DeathOutcome { Result = DeathResult.BlockedByPetBottle, PetBottleRemoved = petBottleRemoved };

        if (ladybugCanIntercept)
            return DeathOutcome.Blocked(DeathResult.BlockedByLadybug);

        if (plant.IsFrozen && cause == DeathCause.Generic)
            return DeathOutcome.Blocked(DeathResult.BlockedFrozen); // 얼어 있으면 웨이브에 죽지 않는다

        // 냉각 방패: 살아 있는 식물이 1개 이하일 때 한 번만 막아 준다
        if (Board.CountLivingPlants() <= 1 && Soil.TryUseCoolingShield())
            return DeathOutcome.Blocked(DeathResult.BlockedByCoolingShield);

        plant.IsDying = true;
        if (Board.Remove(idx))
            Board.NotifyChanged();

        return new DeathOutcome
        {
            Result = DeathResult.Died,
            BonusGold = (int)(plant.GetSellingPrice() * Modifiers.BonusRatioWhenDie),
        };
    }
}

public enum DeathResult
{
    Died,
    BlockedLastPlant,
    BlockedByPetBottle,
    BlockedByLadybug,
    BlockedFrozen,
    BlockedByCoolingShield,
}

public struct DeathOutcome
{
    public DeathResult Result;
    /// <summary>페트병이 막으면서 보호 횟수를 다 써 사라졌는가.</summary>
    public bool PetBottleRemoved;
    /// <summary>죽었을 때 받는 골드 (판매가 × 사망 보너스 비율).</summary>
    public int BonusGold;

    public bool Died => Result == DeathResult.Died;

    public static DeathOutcome Blocked(DeathResult result) => new DeathOutcome { Result = result };
}
