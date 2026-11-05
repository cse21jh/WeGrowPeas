using System.Collections.Generic;

/// <summary>
/// 저장 형식 v1의 키를 지금의 고정 ID로 바꾸는 표.
///
/// v1 세이브는 일반 특성과 구매 이력을 그때의 표시 이름으로 저장했다. 지금은 둘 다 에셋 이름을 쓴다.
/// 왼쪽 값은 옛 파일에 이미 적혀 있는 문자열이라 에셋의 표시 이름을 바꿔도 고치지 않는다.
/// 에셋 이름을 바꿀 때만 오른쪽 값을 함께 고친다.
/// 표에 없는 값은 그대로 돌려준다. 이미 ID이거나, 지금은 없는 옛 이름이다.
/// </summary>
public static class LegacySaveKeys
{
    private static readonly Dictionary<string, string> GeneralAbilities = new()
    {
        ["교배 횟수 추가"] = "AddBreedCountAbility",
        ["기본 자금 증가"] = "AddGoldAbility",
        ["골드 스카우터"] = "GoldScouterAbility",
        ["저항력 스카우터"] = "ResistanceScouterAbility",
        ["일기예보"] = "WeatherForecastAbility",
    };

    private static readonly Dictionary<string, string> Items = new()
    {
        ["저항력 흡수 비료"] = "AbsorbFertilizerItemData",
        ["활성형 껍질"] = "ActiveShellItemData",
        ["땅문서"] = "AddSoilItem",
        ["아드레날린"] = "AdrenalineItemData",
        ["불량식품"] = "BadGuyMoreRiceItemData",
        ["교배 키트"] = "BreedCountItemData",
        ["벌레 유도장치"] = "BugLureDeviceItemData",
        ["벌레 가공 기계"] = "BugProcessingUpgradeItemData",
        ["벌레 스프레이"] = "BugSprayItemData",
        ["고추"] = "ChiliPepperItemData",
        ["매운 고추"] = "ChiliPepperRangeUpgradeItemData",
        ["냉각 방패"] = "CoolingShieldItemData",
        ["신용카드"] = "CreditCardItemData",
        ["유전자 추출기"] = "DNAExtraction",
        ["황금 비료"] = "GoldSoilItemData",
        ["황금 시계"] = "GoldenClockItem",
        ["황금 유전자"] = "GoldenGeneticsItemData",
        ["황금 무당벌레"] = "GoldenLadybugItemData",
        ["치료형 캡사이신"] = "HealingCapsaicinItemData",
        ["건강한 무당벌레"] = "HealthyLadybugItemData",
        ["고품질 식물"] = "HighQualityPlantItemData",
        ["벌레 방해용 선풍기"] = "Item_BugFan",
        ["전용 비료"] = "Item_Fertilizer",
        ["팻말"] = "Item_SignPost",
        ["무당벌레 채집통"] = "LadybugCollectionBoxItemData",
        ["무당벌레 유도장치"] = "LadybugLureDeviceItemData",
        ["땅과 콩"] = "LandAndBeanItemData",
        ["돈나무"] = "MoneyTreeItemData",
        ["네펜데스 소화액 개량"] = "NepenthesDigestionUpgradeItemData",
        ["네펜데스"] = "NepenthesItemData",
        ["네펜데스 페로몬 생성"] = "NepenthesPheromoneItemData",
        ["네펜데스 페로몬 강화"] = "NepenthesPheromoneRangeUpgradeItemData",
        ["완두커피"] = "PeaCoffeeItemData",
        ["식물 모종"] = "PeaItemData",
        ["고속 숙성"] = "PeaMaturationUpgradeItemData",
        ["풍미 증진"] = "PeaTasteUpgradeItemData",
        ["페트병"] = "PetBottleItemData",
        ["페트병 재질 강화"] = "PetBottleMaterialUpgradeItemData",
        ["페트병 원가 감소"] = "PetBottlePriceReductionItemData",
        ["페트병 납품량 증가"] = "PetBottleSupplyIncreaseItemData",
        ["확률 증가 이벤트"] = "ProbabilityEventItemData",
        ["급속 냉각기"] = "RapidFreezerItemData",
        ["왕위 계승"] = "RoyalSuccessionItemData",
        ["상점 연락처"] = "ShopContactItemData",
        ["스프링클러 용액 개조"] = "SprinklerFertilizerSynergyItemData",
        ["스프링클러"] = "SprinklerItemData",
        ["스프링클러 성능 향상"] = "SprinklerRangeUpgradeItemData",
        ["강자생존"] = "StrongGeneticsResistanceUpgradeItemData",
        ["슈퍼 변종"] = "SuperMutationItemData",
        ["시간은 금이다"] = "TimeIsGoldItemData",
        ["타임 머신"] = "TimeMachineItemData",
        ["쌍둥이"] = "TwinsItemData",
        ["약자생존"] = "WeakGeneticsResistanceUpgradeItemData",
    };

    public static string GeneralAbility(string saved) => Convert(GeneralAbilities, saved);

    public static string Item(string saved) => Convert(Items, saved);

    /// <summary>구매 이력 키 목록을 그 자리에서 바꾼다.</summary>
    public static void ConvertItems(IList<string> keys)
    {
        if (keys == null) return;
        for (int i = 0; i < keys.Count; i++)
            keys[i] = Item(keys[i]);
    }

    /// <summary>일반 특성 키 목록을 그 자리에서 바꾼다.</summary>
    public static void ConvertGeneralAbilities(IList<string> keys)
    {
        if (keys == null) return;
        for (int i = 0; i < keys.Count; i++)
            keys[i] = GeneralAbility(keys[i]);
    }

    /// <summary>회상 스냅샷의 구매 이력 키를 그 자리에서 바꾼다.</summary>
    public static void ConvertItems(IEnumerable<DaySnapshot> days)
    {
        if (days == null) return;
        foreach (var day in days)
            if (day != null) ConvertItems(day.itemNames);
    }

    private static string Convert(Dictionary<string, string> table, string saved)
    {
        if (string.IsNullOrEmpty(saved)) return saved;
        return table.TryGetValue(saved, out string id) ? id : saved;
    }
}
