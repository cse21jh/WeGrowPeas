using System;
using System.Collections.Generic;

/// <summary>
/// 땅에 놓인 효과의 상태와 조회. 어느 칸에 무엇이 있는지만 알고, 마커 오브젝트는 모른다.
/// 마커를 만들고 지우는 일은 <see cref="SoilEffects"/>가 맡는다.
/// </summary>
public sealed class SoilState
{
    private readonly PlantBoard board;
    private readonly GridModifiers modifiers;

    public SoilState(PlantBoard board, GridModifiers modifiers)
    {
        this.board = board;
        this.modifiers = modifiers;
    }

    /// <summary>황금 비료가 뿌려진 칸.</summary>
    public List<int> GoldSoilTiles { get; } = new List<int>();
    public List<int> PetBottleTiles { get; } = new List<int>();
    /// <summary>페트병 칸별 남은 보호 횟수. 키 -1은 새 페트병에 붙는 전체 보너스.</summary>
    public Dictionary<int, int> PetBottleBlockCount { get; } = new Dictionary<int, int>();
    /// <summary>세로줄 번호 → 전용 비료 종류.</summary>
    public Dictionary<int, WaveType> FertilizerColumns { get; } = new Dictionary<int, WaveType>();
    /// <summary>저항력 흡수 비료가 뿌려진 칸.</summary>
    public HashSet<int> AbsorbFertilizerTiles { get; } = new HashSet<int>();
    /// <summary>특수(땅부자): 세로줄 번호 → 판매 배수 보너스.</summary>
    public Dictionary<int, float> ColumnGoldMulBonus { get; } = new Dictionary<int, float>();
    public bool HasCoolingShield { get; set; }
    public bool IsCoolingShieldActivated { get; set; }

    public bool HasPetBottle(int idx) => PetBottleTiles.Contains(idx);
    public bool HasGoldSoil(int idx) => GoldSoilTiles.Contains(idx);
    public bool HasFertilizerAt(int idx) => FertilizerColumns.ContainsKey(idx / 4);
    public bool HasAbsorbFertilizer(int idx) => AbsorbFertilizerTiles.Contains(idx);

    public bool TryGetFertilizerType(int gridIndex, out WaveType type)
        => FertilizerColumns.TryGetValue(gridIndex / 4, out type);

    public float GetColumnGoldMulBonus(int idx)
        => ColumnGoldMulBonus.TryGetValue(idx / 4, out float v) ? v : 0f;

    public bool IsPlantFrozen(int gridIndex)
        => board.States.TryGetValue(gridIndex, out PlantState plant) && plant.IsFrozen;

    /// <summary>
    /// 페트병이 죽음을 막는지 판정하고 보호 횟수를 쓴다. 웨이브와 삽으로 인한 죽음만 막는다.
    /// 횟수를 다 써서 페트병이 사라졌으면 removed가 true다.
    /// </summary>
    public bool TryBlockDeathWithPetBottle(int idx, DeathCause cause, out bool removed)
    {
        removed = false;
        if (!PetBottleTiles.Contains(idx) || (cause != DeathCause.Generic && cause != DeathCause.Shovel))
            return false;

        if (PetBottleBlockCount.ContainsKey(idx) && PetBottleBlockCount[idx] > 0)
        {
            PetBottleBlockCount[idx]--;
            if (PetBottleBlockCount[idx] <= 0)
            {
                PetBottleTiles.Remove(idx);
                PetBottleBlockCount.Remove(idx);
                removed = true;
            }
            return true;
        }

        // 보호 횟수 기록이 없으면 한 번 막고 사라진다
        PetBottleTiles.Remove(idx);
        PetBottleBlockCount.Remove(idx);
        removed = true;
        return true;
    }

    /// <summary>냉각 방패를 쓸 수 있으면 써 버리고 true.</summary>
    public bool TryUseCoolingShield()
    {
        if (!HasCoolingShield || IsCoolingShieldActivated) return false;
        IsCoolingShieldActivated = true;
        HasCoolingShield = false; // 사용 후 소멸
        return true;
    }

    /// <summary>특수(알록달록): 해당 칸에 적용 중인 땅과 식물 효과 개수 (정보 UI 기준과 같다).</summary>
    public int CountTileEffects(int idx)
    {
        int count = 0;
        if (HasGoldSoil(idx)) count++;
        if (TryGetFertilizerType(idx, out _)) count++;
        if (HasPetBottle(idx)) count++;
        if (IsAffectedByChiliPepper(idx)) count++;
        if (IsPlantFrozen(idx)) count++;
        if (IsAffectedBySprinkler(idx)) count++;
        if (HasAbsorbFertilizer(idx)) count++;
        return count;
    }

    /// <summary>특수(임시완두B): 주변 4칸의 서로 다른 식물 종 수.</summary>
    public int CountDistinctNeighborSpecies(int idx)
    {
        var species = new HashSet<string>();
        int row = idx % 4;
        int[] neighbors = { idx - 4, idx + 4, row > 0 ? idx - 1 : -1, row < 3 ? idx + 1 : -1 };
        foreach (int n in neighbors)
        {
            if (n < 0 || n >= board.MaxCol * 4) continue;
            if (board.States.TryGetValue(n, out PlantState p) && p != null)
                species.Add(p.Species);
        }
        return species.Count;
    }

    /// <summary>스프링클러의 가로세로 범위 안에 있는가. 스프링클러 자신은 제외.</summary>
    public bool IsAffectedBySprinkler(int targetIdx)
    {
        int range = 1 + modifiers.SprinklerRangeBonus;

        foreach (var kvp in board.States)
        {
            if (kvp.Value.Kind != PlantKind.Sprinkler) continue;

            if (kvp.Key == targetIdx)
                return false; // 본인에게는 효과 없음

            int sIdx = kvp.Key;
            int sRow = sIdx % 4;
            int tRow = targetIdx % 4;
            int sCol = sIdx / 4;
            int tCol = targetIdx / 4;

            if (sCol == tCol && Math.Abs(sRow - tRow) <= range) return true; // 같은 세로줄
            if (sRow == tRow && Math.Abs(sCol - tCol) <= range) return true; // 같은 가로줄
        }
        return false;
    }

    /// <summary>고추의 영향 범위 안에 있는가. 범위는 강화 단계에 따라 좌우, 상하, 대각선으로 넓어진다.</summary>
    public bool IsAffectedByChiliPepper(int gridIndex)
    {
        int maxIndex = board.MaxCol * 4;

        // 0단계: 좌우
        if ((gridIndex - 4) >= 0 && IsChiliPepperAt(gridIndex - 4)) return true;
        if ((gridIndex + 4) < maxIndex && IsChiliPepperAt(gridIndex + 4)) return true;

        // 1단계: 상하
        if (modifiers.ChiliPepperRangeLevel >= 1)
        {
            if ((gridIndex - 1) / 4 == gridIndex / 4 && IsChiliPepperAt(gridIndex - 1)) return true;
            if ((gridIndex + 1) / 4 == gridIndex / 4 && IsChiliPepperAt(gridIndex + 1)) return true;
        }

        // 2단계: 대각선. 조건식은 기존 동작을 그대로 옮긴 것이다.
        if (modifiers.ChiliPepperRangeLevel >= 2)
        {
            // 왼쪽 위
            if ((gridIndex - 5) >= 0 && (gridIndex - 5) % 4 == (gridIndex - 1) % 4 && IsChiliPepperAt(gridIndex - 5)) return true;
            // 오른쪽 위
            if ((gridIndex - 3) >= 0 && (gridIndex - 3) % 4 == (gridIndex - 1) % 4 && IsChiliPepperAt(gridIndex + 3)) return true;
            // 왼쪽 아래
            if ((gridIndex - 3) >= 0 && (gridIndex - 3) % 4 == (gridIndex + 1) % 4 && IsChiliPepperAt(gridIndex - 3)) return true;
            // 오른쪽 아래
            if ((gridIndex + 5) < maxIndex && (gridIndex + 5) % 4 == (gridIndex + 1) % 4 && IsChiliPepperAt(gridIndex + 5)) return true;
        }

        return false;
    }

    /// <summary>고추의 치료 대상. 범위 칸은 기존 동작을 그대로 옮긴 것으로, 저항력 보너스 범위와 0·1단계 방향이 다르다.</summary>
    public List<PlantState> GetPlantsNearChiliPepper(int chiliIndex)
    {
        var list = new List<PlantState>();
        AddHealTargets(list, chiliIndex - 1, chiliIndex + 1);
        if (modifiers.ChiliPepperRangeLevel >= 1)
            AddHealTargets(list, chiliIndex - 4, chiliIndex + 4);
        if (modifiers.ChiliPepperRangeLevel >= 2)
            AddHealTargets(list, chiliIndex - 5, chiliIndex - 3, chiliIndex + 3, chiliIndex + 5);
        return list;
    }

    private void AddHealTargets(List<PlantState> list, params int[] cells)
    {
        foreach (int idx in cells)
            if (idx >= 0 && idx < board.CellCount && board.States.TryGetValue(idx, out PlantState plant)
                && plant.Kind != PlantKind.ChiliPepper)
                list.Add(plant);
    }

    /// <summary>저항력 흡수 비료: 위의 식물이 상하좌우 식물과 겹치는 형질마다 3%p씩 뺏어 온다. 뺏기는 쪽은 10%까지만 준다.</summary>
    public void ProcessResistanceAbsorption()
    {
        const float amount = 0.03f;
        foreach (int centerIdx in AbsorbFertilizerTiles)
        {
            if (!board.States.TryGetValue(centerIdx, out PlantState center) || center.IsDying) continue;
            var centerTraits = new List<GeneticTrait>(center.Traits);

            int col = centerIdx / 4, row = centerIdx % 4;
            int[] neighbors =
            {
                row + 1 < 4 ? centerIdx + 1 : -1,
                row - 1 >= 0 ? centerIdx - 1 : -1,
                col - 1 >= 0 ? centerIdx - 4 : -1,
                col + 1 < board.MaxCol ? centerIdx + 4 : -1,
            };

            foreach (int idx in neighbors)
            {
                if (idx < 0 || !board.States.TryGetValue(idx, out PlantState neighbor) || neighbor.IsDying) continue;
                var neighborTraits = new List<GeneticTrait>(neighbor.Traits);

                foreach (var c in centerTraits)
                {
                    foreach (var n in neighborTraits)
                    {
                        if (c.traitType != n.traitType || n.resistance <= 0.1f) continue;

                        float absorbed = n.resistance - amount < 0.1f ? n.resistance - 0.1f : amount;
                        if (absorbed <= 0.0001f) continue;
                        neighbor.ChangeResistance((int)n.traitType, -absorbed);
                        center.ChangeResistance((int)c.traitType, absorbed);
                    }
                }
            }
        }
    }

    private bool IsChiliPepperAt(int idx)
        => board.States.TryGetValue(idx, out PlantState plant) && plant.Kind == PlantKind.ChiliPepper;
}
