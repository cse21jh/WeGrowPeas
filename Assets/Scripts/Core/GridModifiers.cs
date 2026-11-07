/// <summary>
/// 아이템과 능력이 쌓은 밭 전체 보정치. 씬 참조 없이 숫자만 들고 있다.
/// 값을 바꾸면서 식물이나 표시도 갱신해야 하는 항목은 <see cref="Grid"/>의 Add 메서드를 거친다.
/// </summary>
public class GridModifiers
{
    public float BugSpeedDecreasement = 0f;
    public float BugSpawnIntervalIncreasement = 0f;
    public float LadybugSpawnProbability = 0f;
    public int MaxLadybugCount = 0; // 최대 무당벌레 수 (0 = 무제한)
    public int AdditionalLadybugGoldPerUnit = 0; // 무당벌레당 골드 (웨이브 종료 시)
    public float AdditionalLadybugResistancePerUnit = 0f; // 무당벌레당 저항력 증가
    public int AdditionalBugGold = 0;
    public int AdditionalNepenthesGold = 0; // 네펜데스가 벌레 먹을 때 추가 골드
    public bool HasNepenthesPheromone = false; // 네펜데스 페로몬 활성화 여부
    public float AdditionalNepenthesPheromoneSizeMultiplier = 0f; // 네펜데스 페로몬 범위 증가 배수 (합적용, 0 = 기본값, 0.2 = +20%)
    public float NepenthesSpawnProbability = 0f; // 네펜데스 등장 확률 (rotation weight 증가로 구현)
    public float WeakGeneticsResistanceBonus = 0f; // 약한 유전자(열성이 아닌 형질, 최초 저항력 80%가 아닌 형질) 저항력 증가 보너스
    public float StrongGeneticsResistanceBonus = 0f; // 강한 유전자(우성, 최초 저항력 80%인 형질) 저항력 증가 보너스
    public float GoldenGeneticsProbabilityBonus = 0f; // 황금 유전자 확률 보너스 (genetics = 1에서 우수한 형질이 나올 확률 증가)
    public float AdditionalPlantGoldMultiplier = 0.2f; // 기본값 0.2f (웨이브 저항 횟수당 골드 배수)
    public float AdditionalPestResistance = 0f;
    public float ResistanceBonus = 0f; // 기본 저항력 보너스
    public int AdditionalPlantGold = 0; // 기본 식물 가격 보너스
    public int SprinklerRangeBonus = 0; // 스프링클러 범위 보너스
    public float SprinklerFertilizerSynergyBonus = 0f; // 스프링클러 비료 시너지 보너스
    public int TimeIsGoldLevel = 0; // 시간은 금이다 레벨
    public int BadGuyMoreRiceLevel = 0; // 미운 놈 떡 하나 더 준다 레벨
    public float ResistanceDecayReduction = 0f; // 웨이브를 버텼을 때, 감소하는 저항력의 감소량
    public float ResistanceAdaptation = 0f; // 웨이브를 버텼을 때, 해당 웨이브의 저항력 증가량
    public float AdditionalPeanutCopyProbability = 0f; // 땅콩 복사 확률 증가
    public float BonusRatioWhenDie = 0f; // 죽었을 때, 추가 골드
    public int AdditionalInheritance = 0;
    public int PetBottleInitialStockBonus = 0; // 페트병 일일 최대 구매 횟수 보너스
    public int PetBottlePriceReduction = 0; // 페트병 가격 감소량
    public float PetBottleSpawnProbability = 0f; // 페트병 등장 확률 (rotation weight 증가로 구현)
    public int ChiliPepperRangeLevel = 0; // 고추 영향 범위 레벨 (0: 가로 2칸, 1: 십자 4칸, 2: 대각선까지 8칸)
    public float ChiliPepperSpawnProbability = 0f; // 고추 등장 확률 (rotation weight 증가로 구현)
    public float ChiliPepperHealPercent = 0f; // 치료형 캡사이신 회복 퍼센트 (구매 횟수 * 3%)
    public float CreditCardRefundPercent = 0f;
    public float TwinBreedProbability = 0f;
    public float PeaCoffeeMultiplier = 0f; // 완두커피: 자유시간이 지날 때마다 붙는 판매 골드 배수
    public float SuperMutationChanceBonus = 0f; // 슈퍼 변종: 변종 발생 확률 가산(%p)
    public bool HasSuperMutation = false; // 슈퍼 변종: 양성/악성 변종 확률 반전
    public float ActiveShellProbability = 0f; // 활성형 껍질: 미교배 식물의 자가번식 확률 가산
    public float SuccessionInheritRatio = 0f; // 왕위 계승: 자가번식 시 계승되는 가격 배율 비율
    public int LandAndBeanLevel = 0; // 땅과 콩: 웨이브 후 뿌리 확률 +10%p/구매 + 뿌리내린 식물 가격 +10%p/구매

    /// <summary><see cref="Load"/>와 짝이다. 저장 파일의 필드 이름은 바꾸지 않는다.</summary>
    public void Save(GridSave save)
    {
        save.bugSpeedDecreasement = BugSpeedDecreasement;
        save.bugSpawnIntervalIncreasement = BugSpawnIntervalIncreasement;
        save.ladybugSpawnProbability = LadybugSpawnProbability;
        save.maxLadybugCount = MaxLadybugCount;
        save.additionalLadybugGoldPerUnit = AdditionalLadybugGoldPerUnit;
        save.additionalLadybugResistancePerUnit = AdditionalLadybugResistancePerUnit;
        save.additionalBugGold = AdditionalBugGold;
        save.additionalNepenthesGold = AdditionalNepenthesGold;
        save.hasNepenthesPheromone = HasNepenthesPheromone;
        save.additionalNepenthesPheromoneSizeMultiplier = AdditionalNepenthesPheromoneSizeMultiplier;
        save.nepenthesSpawnProbability = NepenthesSpawnProbability;
        save.weakGeneticsResistanceBonus = WeakGeneticsResistanceBonus;
        save.strongGeneticsResistanceBonus = StrongGeneticsResistanceBonus;
        save.goldenGeneticsProbabilityBonus = GoldenGeneticsProbabilityBonus;
        save.additionalPlantGoldMultiplier = AdditionalPlantGoldMultiplier;
        save.additionalPestResistance = AdditionalPestResistance;
        save.resistanceBonus = ResistanceBonus;
        save.additionalPlantGold = AdditionalPlantGold;
        save.sprinklerRangeBonus = SprinklerRangeBonus;
        save.sprinklerFertilizerSynergyBonus = SprinklerFertilizerSynergyBonus;
        save.timeIsGoldLevel = TimeIsGoldLevel;
        save.badGuyMoreRiceLevel = BadGuyMoreRiceLevel;
        save.resistanceDecayReduction = ResistanceDecayReduction;
        save.resistanceAdaptation = ResistanceAdaptation;
        save.additionalPeanutCopyProbability = AdditionalPeanutCopyProbability;
        save.bonusRatioWhenDie = BonusRatioWhenDie;
        save.additionalInheritance = AdditionalInheritance;
        save.petBottleInitialStockBonus = PetBottleInitialStockBonus;
        save.petBottlePriceReduction = PetBottlePriceReduction;
        save.petBottleSpawnProbability = PetBottleSpawnProbability;
        save.chiliPepperRangeLevel = ChiliPepperRangeLevel;
        save.chiliPepperSpawnProbability = ChiliPepperSpawnProbability;
        save.chiliPepperHealPercent = ChiliPepperHealPercent;
        save.creditCardRefundPercent = CreditCardRefundPercent;
        save.twinBreedProbability = TwinBreedProbability;
        save.peaCoffeeMultiplier = PeaCoffeeMultiplier;
        save.superMutationChanceBonus = SuperMutationChanceBonus;
        save.hasSuperMutation = HasSuperMutation;
        save.activeShellProbability = ActiveShellProbability;
        save.successionInheritRatio = SuccessionInheritRatio;
        save.landAndBeanLevel = LandAndBeanLevel;
    }

    public void Load(GridSave saveData)
    {
        BugSpeedDecreasement = saveData.bugSpeedDecreasement;
        BugSpawnIntervalIncreasement = saveData.bugSpawnIntervalIncreasement;
        LadybugSpawnProbability = saveData.ladybugSpawnProbability;
        MaxLadybugCount = saveData.maxLadybugCount;
        AdditionalLadybugGoldPerUnit = saveData.additionalLadybugGoldPerUnit;
        AdditionalLadybugResistancePerUnit = saveData.additionalLadybugResistancePerUnit;
        AdditionalBugGold = saveData.additionalBugGold;
        AdditionalNepenthesGold = saveData.additionalNepenthesGold;
        HasNepenthesPheromone = saveData.hasNepenthesPheromone;
        AdditionalNepenthesPheromoneSizeMultiplier = saveData.additionalNepenthesPheromoneSizeMultiplier;
        NepenthesSpawnProbability = saveData.nepenthesSpawnProbability;
        WeakGeneticsResistanceBonus = saveData.weakGeneticsResistanceBonus;
        StrongGeneticsResistanceBonus = saveData.strongGeneticsResistanceBonus;
        GoldenGeneticsProbabilityBonus = saveData.goldenGeneticsProbabilityBonus;
        AdditionalPlantGoldMultiplier = saveData.additionalPlantGoldMultiplier;
        AdditionalPestResistance = saveData.additionalPestResistance;
        ResistanceBonus = saveData.resistanceBonus;
        AdditionalPlantGold = saveData.additionalPlantGold;
        SprinklerRangeBonus = saveData.sprinklerRangeBonus;
        SprinklerFertilizerSynergyBonus = saveData.sprinklerFertilizerSynergyBonus;
        TimeIsGoldLevel = saveData.timeIsGoldLevel;
        BadGuyMoreRiceLevel = saveData.badGuyMoreRiceLevel;
        ResistanceDecayReduction = saveData.resistanceDecayReduction;
        ResistanceAdaptation = saveData.resistanceAdaptation;
        AdditionalPeanutCopyProbability = saveData.additionalPeanutCopyProbability;
        BonusRatioWhenDie = saveData.bonusRatioWhenDie;
        AdditionalInheritance = saveData.additionalInheritance;
        PetBottleInitialStockBonus = saveData.petBottleInitialStockBonus;
        PetBottlePriceReduction = saveData.petBottlePriceReduction;
        PetBottleSpawnProbability = saveData.petBottleSpawnProbability;
        ChiliPepperRangeLevel = saveData.chiliPepperRangeLevel;
        ChiliPepperSpawnProbability = saveData.chiliPepperSpawnProbability;
        ChiliPepperHealPercent = saveData.chiliPepperHealPercent;
        CreditCardRefundPercent = saveData.creditCardRefundPercent;
        TwinBreedProbability = saveData.twinBreedProbability;
        PeaCoffeeMultiplier = saveData.peaCoffeeMultiplier;
        SuperMutationChanceBonus = saveData.superMutationChanceBonus;
        HasSuperMutation = saveData.hasSuperMutation;
        ActiveShellProbability = saveData.activeShellProbability;
        SuccessionInheritRatio = saveData.successionInheritRatio;
        LandAndBeanLevel = saveData.landAndBeanLevel;
    }
    public void AddBugSpeedDcreasement(float value)
    {
        BugSpeedDecreasement += value;
    }

    public void AddBugSpawnIntervalIncreasement(float value)
    {
        BugSpawnIntervalIncreasement += value;
    }

    public void AddLadybugSpawnProbability(float value)
    {
        LadybugSpawnProbability += value;
    }

    public void AddMaxLadybugCount(int value)
    {
        MaxLadybugCount += value;
    }

    public void AddAdditionalLadybugGoldPerUnit(int value)
    {
        AdditionalLadybugGoldPerUnit += value;
    }

    public void AddAdditionalLadybugResistancePerUnit(float value)
    {
        AdditionalLadybugResistancePerUnit += value;
    }

    public void AddAdditionalBugGold(int value)
    {
        AdditionalBugGold += value;
    }

    public void AddAdditionalNepenthesGold(int value)
    {
        AdditionalNepenthesGold += value;
    }

    public void AddGoldenGeneticsProbabilityBonus(float value)
    {
        GoldenGeneticsProbabilityBonus += value;
    }

    public void AddNepenthesSpawnProbability(float value)
    {
        NepenthesSpawnProbability += value;
    }

    public void AddSprinklerRangeBonus(int val)
    {
        SprinklerRangeBonus += val;
    }

    public void AddTimeIsGoldLevel(int val)
    {
        TimeIsGoldLevel += val;
    }

    public void AddBadGuyMoreRiceLevel(int val)
    {
        BadGuyMoreRiceLevel += val;
        //ShowAllPriceSign(); // 가격 변동 갱신
    }

    public void AddAdditionalInheritance(int value)
    {
        AdditionalInheritance += value;
        return;
    }

    public void AddPetBottleInitialStockBonus(int value)
    {
        PetBottleInitialStockBonus += value;
    }

    public void AddPetBottlePriceReduction(int value)
    {
        PetBottlePriceReduction += value;
    }

    public void AddPetBottleSpawnProbability(float value)
    {
        PetBottleSpawnProbability += value;
    }

    public void AddChiliPepperSpawnProbability(float value)
    {
        ChiliPepperSpawnProbability += value;
    }

    public void AddChiliPepperHealPercent(float value)
    {
        ChiliPepperHealPercent += value;
    }

    public void AddAdditionalPeanutCopyProbability(float value)
    {
        AdditionalPeanutCopyProbability += value;
    }

    /// <summary>신용카드: 3슬롯 이상 구매한 날의 소모 비용 환급 비율 증가.</summary>
    public void AddCreditCardRefundPercent(float value)
    {
        CreditCardRefundPercent += value;
    }

    /// <summary>쌍둥이: 교배 시 식물이 하나 더 생성될 확률 증가.</summary>
    public void AddTwinBreedProbability(float value)
    {
        TwinBreedProbability += value;
    }

    /// <summary>슈퍼 변종: 양성/악성 변종 확률을 반전시키고 변종 발생 확률(%p)을 증가.</summary>
    public void AddSuperMutation(float chanceBonusPercent)
    {
        HasSuperMutation = true;
        SuperMutationChanceBonus += chanceBonusPercent;
    }

    /// <summary>활성형 껍질: 한 번도 교배를 시도하지 않은 식물의 자가번식 확률 증가.</summary>
    public void AddActiveShellProbability(float value)
    {
        ActiveShellProbability += value;
    }

    /// <summary>왕위 계승: 자가번식 시 계승되는 가격 배율 비율 증가.</summary>
    public void AddSuccessionInheritRatio(float value)
    {
        SuccessionInheritRatio += value;
    }

    public void AddResistanceDecayReduction(float value)
    {
        ResistanceDecayReduction += value;
    }

    public void AddResistanceAdaptation(float value)
    {
        ResistanceAdaptation += value;
    }

    public void AddBonusRatioWhenDie(float value)
    {
        BonusRatioWhenDie += value;
    }

    public float GetAdditionalPlantGoldMultiplier()
    {
        // 특수(마법부여자): 밤낮 전환 발동형 아이템(고속 숙성 등)의 수치 2배 — 기본값(0.2) 초과분만 2배
        const float baseMul = 0.2f;
        float added = AdditionalPlantGoldMultiplier - baseMul;
        if (added > 0f && SpecialItemSystem.Has("enchanter"))
            return baseMul + added * 2f;
        return AdditionalPlantGoldMultiplier;
    }

    public float GetEffectiveNepenthesPheromoneSizeMultiplier()
    {
        return 1f + AdditionalNepenthesPheromoneSizeMultiplier; // 0.2 = +20% = 1.2배
    }

    /// <summary>땅과 콩: 웨이브가 지나간 후 식물이 뿌리를 내릴 확률(0~1). 구매 1회당 +10%p.</summary>
    public float GetLandAndBeanRootChance()
    {
        return 0.1f * LandAndBeanLevel;
    }

    /// <summary>땅과 콩: 뿌리내린 식물의 가격 배수(구매 1회당 +10%p, 다른 효과와 곱적용).</summary>
    public float GetRootedPriceMultiplier()
    {
        return 1f + 0.1f * LandAndBeanLevel;
    }
}
