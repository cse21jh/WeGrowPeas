using System;
using System.Collections.Generic;

/// <summary>식물 종류. 값은 기존 plantID와 같다.</summary>
public enum PlantKind
{
    Pea = 0,
    Peanut = 1,
    Nepenthes = 2,
    ChiliPepper = 3,
    Sprinkler = 4,
    MoneyTree = 5,
}

/// <summary>
/// 식물 한 포기의 상태와 규칙. 씬 오브젝트가 아니라서 씬 없이 만들고 계산할 수 있다.
///
/// 화면 표시, 이펙트, 입력은 <see cref="Plant"/>가 맡고, 여기에는 판정에 쓰는 값과 공식만 둔다.
/// 저항력과 판매가 공식은 이 파일 한 곳에만 있다. 더하는 순서가 결과를 바꾸므로 순서를 옮기지 않는다.
/// </summary>
public sealed class PlantState
{
    private static readonly int[] PeaBasePrices = { 90, 130, 160, 180, 200, 230, 270 };
    private static readonly int[] PeanutBasePrices = { 60, 100, 130, 150, 170, 200, 240 };
    private const int VegetarianBasePrice = 500;

    public PlantState(PlantKind kind)
    {
        Kind = kind;
    }

    public PlantKind Kind { get; }

    /// <summary>밭에 심기 전에는 null. 그때는 땅과 밭 보정 없이 계산한다.</summary>
    public FieldState Field { get; private set; }

    // ───── 저장되는 값 ─────
    public List<GeneticTrait> Traits { get; set; } = new List<GeneticTrait>();
    public int GridIndex { get; set; }
    public int Taste { get; set; }
    public int ResistWaveCount { get; set; }
    /// <summary>스프링클러 등으로 얻은 추가 배수 횟수.</summary>
    public int BonusGoldMultiplierCount { get; set; }
    /// <summary>특수(세계여행): 낮 이동 거리로 쌓인 배수.</summary>
    public float TravelSellBonus { get; set; }
    /// <summary>완두커피: 보유 중에 지나간 자유시간 횟수.</summary>
    public int FreeTimePassedCount { get; set; }
    public int DaysOnField { get; set; }
    /// <summary>활성형 껍질: 교배를 한 번이라도 시도했는가.</summary>
    public bool HasTriedBreed { get; set; }
    /// <summary>뿌리를 내려 옮길 수 없는 상태.</summary>
    public bool IsRooted { get; set; }

    // ───── 저장되지 않는 값 ─────
    /// <summary>특수(세계여행): 낮이 시작될 때의 칸. 기록 전에는 -1.</summary>
    public int DayStartGridIndex { get; set; } = -1;
    public bool IsDying { get; set; }
    public bool IsSeized { get; set; }
    public bool IsFrozen { get; private set; }
    public int FrozenPrice { get; private set; }
    /// <summary>저주(꽃가루 실종): 교배 가능 여부.</summary>
    public bool Breedable { get; set; } = true;
    /// <summary>
    /// 황금 식물 여부. 황금 식물은 웨이브 뒤 저항력 감소를 받지 않는다.
    /// 형질이 바뀔 때마다 다시 판정하고(<see cref="UpdateGoldFromTraits"/>), 황금 비료 칸에 놓이면 켜진다.
    /// </summary>
    public bool IsGold { get; set; }
    /// <summary>돈나무: 버틴 웨이브 수. 저장된다.</summary>
    public int SurvivedTurns { get; set; }

    /// <summary>기본 저항력이 바뀐 직후 발생한다. 인자는 형질 번호.</summary>
    public event Action<int> ResistanceChanged;

    public bool IsMovableKind => Kind == PlantKind.Pea || Kind == PlantKind.Peanut;
    public bool IsMovable => IsMovableKind && !IsRooted;

    public string Species
    {
        get
        {
            switch (Kind)
            {
                case PlantKind.Pea: return PlantIds.Pea;
                case PlantKind.Peanut: return PlantIds.Peanut;
                case PlantKind.Nepenthes: return PlantIds.Nepenthes;
                case PlantKind.ChiliPepper: return PlantIds.ChiliPepper;
                case PlantKind.Sprinkler: return PlantIds.Sprinkler;
                default: return PlantIds.MoneyTree;
            }
        }
    }

    public void Attach(FieldState field, int gridIndex)
    {
        Field = field;
        GridIndex = gridIndex;
    }

    /// <summary>밭에 심길 때. 칸을 정하고 맛을 굴린다. 불러오기에서도 거치며, 그때는 저장된 맛이 뒤에 덮어쓴다.</summary>
    public void OnPlanted(FieldState field, int gridIndex)
    {
        Attach(field, gridIndex);
        Taste = GameRun.Rng.Field.Range(0, 7);
        if (IsMovableKind) MarkGoldIfOnGoldSoil();
    }

    /// <summary>다른 칸으로 옮긴다. 밭의 칸 지도는 호출한 쪽이 맞춘다.</summary>
    public void MoveTo(int gridIndex)
    {
        GridIndex = gridIndex;
        MarkGoldIfOnGoldSoil();
    }

    private void MarkGoldIfOnGoldSoil()
    {
        if (Field != null && Field.Soil.HasGoldSoil(GridIndex))
            IsGold = true;
    }

    // ───── 황금 ─────

    /// <summary>모든 형질(8개)을 갖고 각각의 기본 + 추가 저항력이 80% 이상이면 황금이다.</summary>
    public static bool AreTraitsGold(List<GeneticTrait> traits)
    {
        int n = 0;
        foreach (var t in traits)
        {
            if (t.resistance + t.additionalResistance < 0.7999f) // 부동소수점 오차 때문에 0.8 대신
                return false;
            n++;
        }
        return n == (int)TraitType.None;
    }

    /// <summary>형질이 바뀐 뒤 황금 여부를 다시 판정한다. 황금 비료로 켜진 표시도 이때 형질 기준으로 돌아간다.</summary>
    public bool UpdateGoldFromTraits()
    {
        IsGold = AreTraitsGold(Traits);
        return IsGold;
    }

    // ───── 뿌리 ─────

    /// <summary>새벽: 심기거나 옮겨질 때 확률로 뿌리를 내려 옮길 수 없게 된다. 뿌리를 내렸으면 true.</summary>
    public bool TryRootByDawn()
    {
        if (!IsMovableKind || IsRooted) return false;
        float chance = DawnSystem.Current.rootChancePercent;
        if (chance <= 0f) return false;
        if (GameRun.Rng.Field.Range(0f, 100f) >= chance) return false;

        IsRooted = true;

        // 특수(임시땅콩C): 뿌리를 내리면 모든 저항력 40%p 증가
        if (SpecialItemSystem.Has("peanut_special_12"))
            for (int i = 0; i < Wave.NumberOfWave; i++)
                ChangeResistance(i, 0.4f);

        return true;
    }

    // ───── 저항력 ─────

    /// <summary>해당 웨이브(형질)에 대한 최종 저항력. 땅, 주변 식물, 저주 보정을 모두 더한 값이다.</summary>
    public float GetResistanceValue(int traitNum)
    {
        // 고추, 네펜데스, 스프링클러, 돈나무는 웨이브에 죽지 않는다.
        if (!IsMovableKind) return 1f;

        // 황금 비료에 심어진 식물은 모든 저항력 90%
        if (Field != null && Field.Soil.HasGoldSoil(GridIndex))
            return 0.9f;

        float resistance = 0f;
        foreach (var g in Traits)
        {
            if (traitNum != (int)g.traitType) continue;

            if (Field != null)
            {
                // 그 세로줄에 같은 종류의 전용 비료가 있으면 +5%p
                if (Field.Soil.TryGetFertilizerType(GridIndex, out WaveType fertilizer) && (int)fertilizer == traitNum)
                {
                    resistance += 0.05f;
                    // 스프링클러 범위 안이면 시너지 보너스
                    if (Field.Soil.IsAffectedBySprinkler(GridIndex))
                        resistance += Field.Modifiers.SprinklerFertilizerSynergyBonus;
                }
                // 고추가 주변에 있고 우성이 아닌 형질이면 +20%p
                if (Field.Soil.IsAffectedByChiliPepper(GridIndex) && g.genetics <= 1)
                    resistance += 0.2f;
                // 무당벌레 한 마리당 저항력 증가 (모든 형질)
                resistance += Field.Board.Ladybugs.Count * Field.Modifiers.AdditionalLadybugResistancePerUnit;
            }

            // 기본 저항력 + 추가 저항력(업그레이드, 벌레잡기)
            resistance += Math.Max(0f, g.resistance) + g.additionalResistance;

            // 저주(반란): 우성 아님(0·1) +%p, 우성(2) -%p
            if (CurseState.ReversePercent > 0f)
                resistance += (g.genetics <= 1 ? 1f : -1f) * (CurseState.ReversePercent / 100f);

            if (resistance < 0f) resistance = 0f;
            return resistance >= 1f ? 1f : resistance; // 총합 상한 1.0
        }

        return 0.1f; // 해당 형질이 없으면 10%
    }

    /// <summary>웨이브를 버티는지 굴린다.</summary>
    public bool CanResist(WaveType wave)
    {
        // 얼어 있어도 난수는 먼저 뽑는다. 순서를 바꾸면 같은 시드의 결과가 달라진다.
        int roll = GameRun.Rng.Survival.Range(0, 100);
        if (IsFrozen) return true; // 얼어 있으면 무조건 버틴다

        if (roll < (int)(GetResistanceValue((int)wave) * 100))
            return true;

        // 특수(이중 시도): 실패하면 한 번 더 굴린다
        if (SpecialItemSystem.Has("double_try"))
        {
            roll = GameRun.Rng.Survival.Range(0, 100);
            if (roll < (int)(GetResistanceValue((int)wave) * 100))
                return true;
        }

        return false;
    }

    /// <summary>기본 저항력을 바꾼다. 기본 저항력은 반드시 이 함수로만 바꾼다. 해당 형질이 없으면 false.</summary>
    public bool ChangeResistance(int traitNum, float amount)
    {
        if (amount == 0)
            return false;

        for (int i = 0; i < Traits.Count; i++)
        {
            if ((int)Traits[i].traitType != traitNum) continue;

            float value = Traits[i].resistance + amount;

            // 특수(프로모션): 10% 이하가 되면 90%로
            if (value <= 0.1f && SpecialItemSystem.Has("promotion"))
                value = 0.9f;

            if (value < 0.1f)
                value = 0.1f;

            Traits[i] = new GeneticTrait((TraitType)traitNum, value, Traits[i].genetics, Traits[i].additionalResistance);
            if (IsMovableKind) UpdateGoldFromTraits();
            ResistanceChanged?.Invoke(traitNum);
            return true;
        }
        return false;
    }

    public bool HasTrait(WaveType wave)
    {
        for (int i = 0; i < Traits.Count; i++)
        {
            if ((int)Traits[i].traitType == (int)wave)
                return true;
        }
        return false;
    }

    /// <summary>자연사, 해충, 바람, 홍수 중 우성이 아닌(genetics 0·1) 형질 수.</summary>
    public int GetBadGenesCount()
    {
        int count = 0;
        foreach (var trait in Traits)
        {
            if (trait.genetics <= 1 &&
               (trait.traitType == TraitType.NaturalDeath ||
                trait.traitType == TraitType.Pest ||
                trait.traitType == TraitType.Wind ||
                trait.traitType == TraitType.Flood))
            {
                count++;
            }
        }
        return count;
    }

    // ───── 웨이브 통과 ─────

    /// <summary>
    /// 웨이브를 버틴 뒤의 변화. 버틴 횟수와 저항력 감소를 반영한다.
    /// 스프링클러와 돈나무의 웨이브 후 동작은 아직 각 컴포넌트에 있다.
    /// </summary>
    public void ResistWave(WaveType waveType)
    {
        if (!IsMovableKind)
        {
            // 특수(채식주의자): 고추와 네펜데스도 버틸 때마다 가치 증가
            if ((Kind == PlantKind.ChiliPepper || Kind == PlantKind.Nepenthes) && SpecialItemSystem.Has("vegetarian"))
                ResistWaveCount++;
            return;
        }

        if (IsFrozen) return;

        ResistWaveCount++;

        // 특수(세계여행): 낮 동안 옮긴 거리(처음 칸과 마지막 칸의 맨해튼 거리) 한 칸마다 배수 +0.1
        if (SpecialItemSystem.Has("world_travel") && DayStartGridIndex >= 0)
        {
            int dist = Math.Abs(GridIndex / 4 - DayStartGridIndex / 4) + Math.Abs(GridIndex % 4 - DayStartGridIndex % 4);
            if (dist > 0) TravelSellBonus += 0.1f * dist;
        }

        // 새벽과 저주(방사능): 웨이브를 지날 때마다 모든 저항력 감소. 황금 식물은 제외.
        float dawnDailyDecay = DawnSystem.Current.dailyResistanceDecayAddPercent / 100f;
        float curseDailyDecay = CurseState.RadiationDecayPercent / 100f;
        float totalDailyDecay = dawnDailyDecay + curseDailyDecay;
        bool isGoldPlant = IsGold;

        bool hasMatchingFertilizer = Field != null
            && Field.Soil.TryGetFertilizerType(GridIndex, out WaveType fertilizer)
            && (int)fertilizer == (int)waveType;

        if (totalDailyDecay > 0f && !isGoldPlant)
        {
            for (int i = 0; i < Wave.NumberOfWave; i++)
            {
                if (hasMatchingFertilizer && i == (int)waveType)
                    continue; // 이번 웨이브와 같은 비료가 깔려 있으면 그 저항력은 줄지 않는다
                ChangeResistance(i, -totalDailyDecay);
            }
        }

        // 저주(집중포화): 이번 웨이브에 대한 저항력 추가 감소
        if (CurseState.HeavyFire && CurseState.HeavyFireExtraDecayPercent > 0f && !isGoldPlant && !hasMatchingFertilizer)
            ChangeResistance((int)waveType, -CurseState.HeavyFireExtraDecayPercent / 100f);
    }

    /// <summary>
    /// 스프링클러: 웨이브 뒤 범위 안의 판매 가능한 식물 하나를 골라 판매 배수를 한 번 더 준다.
    /// 고른 식물과 주기 전 가격을 돌려준다. 대상이 없으면 false.
    /// </summary>
    public bool SprinklerAfterWave(out PlantState target, out int previousPrice)
    {
        target = null;
        previousPrice = 0;
        if (Kind != PlantKind.Sprinkler || Field == null) return false;

        var neighbors = new List<PlantState>();
        int maxIndex = Field.Board.MaxCol * 4;
        int range = 1 + Field.Modifiers.SprinklerRangeBonus;

        // 위, 아래, 왼쪽, 오른쪽 순서. 순서가 난수로 고르는 대상을 정하므로 바꾸지 않는다.
        for (int i = 1; i <= range; i++)
            if (GridIndex % 4 >= i) AddSprinklerTarget(GridIndex - i, neighbors);
        for (int i = 1; i <= range; i++)
            if (GridIndex % 4 <= 3 - i) AddSprinklerTarget(GridIndex + i, neighbors);
        for (int i = 1; i <= range; i++)
            if (GridIndex - 4 * i >= 0) AddSprinklerTarget(GridIndex - 4 * i, neighbors);
        for (int i = 1; i <= range; i++)
            if (GridIndex + 4 * i < maxIndex) AddSprinklerTarget(GridIndex + 4 * i, neighbors);

        if (neighbors.Count == 0) return false;

        PlantState lucky = neighbors[GameRun.Rng.Field.Range(0, neighbors.Count)];
        if (!lucky.IsMovableKind) return false; // 완두·땅콩만 골드 배수를 받는다

        target = lucky;
        previousPrice = lucky.GetSellingPrice();
        // 특수(마법부여자): 밤낮 전환 발동형 아이템 수치 2배
        lucky.BonusGoldMultiplierCount += SpecialItemSystem.Has("enchanter") ? 2 : 1;
        return true;
    }

    private void AddSprinklerTarget(int idx, List<PlantState> list)
    {
        // 살아 있고 판매 가능한 식물만 (스프링클러, 돈나무 등 제외)
        if (Field.Board.States.TryGetValue(idx, out PlantState plant)
            && plant != null && !plant.IsDying && plant.GetSellingPrice() > 0)
            list.Add(plant);
    }

    public const int MoneyTreeRequiredTurns = 5;
    public const int MoneyTreeRewardGold = 5000;

    /// <summary>돈나무: 상하좌우에 완두나 땅콩이 있는가. 없으면 죽는다.</summary>
    public bool HasAdjacentBreedablePlant()
    {
        if (Field == null) return false;

        int maxIndex = Field.Board.MaxCol * 4;
        int[] offsets = { -4, 4, -1, 1 };
        foreach (int offset in offsets)
        {
            int adjacent = GridIndex + offset;
            if (adjacent < 0 || adjacent >= maxIndex) continue;

            if (offset == -4 || offset == 4)
            {
                if (GridIndex % 4 != adjacent % 4) continue;
            }
            else if (GridIndex / 4 != adjacent / 4) continue; // 세로줄 경계를 넘지 않게

            if (Field.Board.States.TryGetValue(adjacent, out PlantState plant) && plant.IsMovableKind)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 돈나무: 웨이브를 하나 버틴 뒤의 결과. 죽이기와 보상 지급은 호출한 쪽이 한다.
    /// </summary>
    public MoneyTreeResult MoneyTreeAfterWave()
    {
        SurvivedTurns++;
        if (!HasAdjacentBreedablePlant()) return MoneyTreeResult.DiesAlone;
        if (SurvivedTurns >= MoneyTreeRequiredTurns) return MoneyTreeResult.Matured;
        return MoneyTreeResult.Growing;
    }

    // ───── 판매가 ─────

    public int GetSellingPrice()
    {
        switch (Kind)
        {
            case PlantKind.Pea:
            {
                int basePrice = BasePriceByTaste(PeaBasePrices);
                if (Field == null) return basePrice;

                // 특수(임시완두B): 주변 4칸의 서로 다른 식물 1종마다 기본 가격 +50
                if (SpecialItemSystem.Has("pea_special_8"))
                    basePrice += 50 * Field.Soil.CountDistinctNeighborSpecies(GridIndex);

                int price = CalculateSellingPrice(basePrice, Field.Modifiers.GetAdditionalPlantGoldMultiplier());

                // 특수(임시완두A): 보유 형질의 저항력 평균만큼 가격 추가 증가 (곱적용)
                if (SpecialItemSystem.Has("pea_special_4") && Traits.Count > 0)
                {
                    float sum = 0f;
                    foreach (var t in Traits) sum += GetResistanceValue((int)t.traitType);
                    price = (int)Math.Round(price * (1f + sum / Traits.Count));
                }
                return price;
            }
            case PlantKind.Peanut:
            {
                int basePrice = BasePriceByTaste(PeanutBasePrices);
                if (Field == null) return basePrice;
                return CalculateSellingPrice(basePrice, Field.Modifiers.GetAdditionalPlantGoldMultiplier());
            }
            case PlantKind.ChiliPepper:
                // 특수(채식주의자): 다른 식물과 같은 가격 상승 공식 적용
                if (SpecialItemSystem.Has("vegetarian") && Field != null)
                    return CalculateSellingPrice(VegetarianBasePrice, Field.Modifiers.GetAdditionalPlantGoldMultiplier());
                return 500;
            case PlantKind.Nepenthes:
                if (SpecialItemSystem.Has("vegetarian") && Field != null)
                    return CalculateSellingPrice(VegetarianBasePrice, Field.Modifiers.GetAdditionalPlantGoldMultiplier());
                return 0;
            default:
                return 0; // 스프링클러, 돈나무는 판매 불가
        }
    }

    private int BasePriceByTaste(int[] table)
        => (Taste >= 0 && Taste < table.Length) ? table[Taste] : 0;

    private int CalculateSellingPrice(int basePrice, float multiplier)
    {
        if (Field == null) return 0;
        GridModifiers modifiers = Field.Modifiers;

        int totalMultiplierCount = ResistWaveCount + BonusGoldMultiplierCount;
        int badGeneBonus = modifiers.BadGuyMoreRiceLevel * 5 * GetBadGenesCount();

        multiplier += TravelSellBonus; // 특수(세계여행)
        multiplier += Field.Soil.GetColumnGoldMulBonus(GridIndex); // 특수(땅부자)

        // 특수(알록달록): 땅에 적용 중인 효과 1개당 배수 +0.1
        if (SpecialItemSystem.Has("colorful"))
            multiplier += 0.1f * Field.Soil.CountTileEffects(GridIndex);

        // 완두커피: 자유시간이 지난 횟수만큼 배수 추가
        float freeTimeBonus = modifiers.PeaCoffeeMultiplier * FreeTimePassedCount;

        int price = (int)((basePrice + modifiers.AdditionalPlantGold + badGeneBonus)
                          * (1f + (multiplier * totalMultiplierCount) + freeTimeBonus));

        // 땅과 콩: 뿌리내린 식물의 가격 증가 (다른 효과와 곱적용)
        if (IsMovableKind && IsRooted)
            price = (int)(price * modifiers.GetRootedPriceMultiplier());

        return price;
    }

    // ───── 그 밖의 상태 ─────

    /// <summary>급속 냉각기. 옮길 수 있는 종류만 언다.</summary>
    public void SetFrozen(bool freeze)
    {
        if (!IsMovableKind) return;
        IsFrozen = freeze;
        if (freeze) FrozenPrice = GetSellingPrice();
    }

    public void SetTaste(int value)
    {
        if (IsFrozen) return; // 얼어 있으면 맛(가격) 변동 없음
        Taste = value;
    }

    /// <summary>플레이어가 옮길 수 있는가. 페트병이나 황금 비료 위의 식물은 못 옮긴다.</summary>
    public bool CanMove()
    {
        if (!IsMovable) return false;
        if (Field != null && Field.Soil.HasPetBottle(GridIndex)) return false;
        if (Field != null && Field.Soil.HasGoldSoil(GridIndex)) return false;
        return true;
    }
}

public enum MoneyTreeResult
{
    /// <summary>아직 자라는 중.</summary>
    Growing,
    /// <summary>주변에 완두나 땅콩이 없어 죽는다.</summary>
    DiesAlone,
    /// <summary>다 자라 보상을 주고 사라진다.</summary>
    Matured,
}
