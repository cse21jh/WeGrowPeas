using System;
using System.Collections.Generic;

/// <summary>
/// 상점 아이템이 보고 바꾸는 세계. 게임은 <see cref="GridShopWorld"/>(화면 처리 포함),
/// 시뮬레이터는 상태만 바꾸는 구현을 쓴다. 아이템은 Grid나 GameManager를 직접 부르지 않는다.
/// </summary>
public interface IShopWorld
{
    FieldState Field { get; }
    ModState Mods { get; }
    /// <summary>다음에 닥칠 웨이브. (밤에는 내일 웨이브)</summary>
    WaveType CurrentWave { get; }
    string Species { get; }
    bool IsNight { get; }

    // 구매
    /// <summary>골드가 모자라면 쓰지 않고 false.</summary>
    bool TrySpend(int gold);
    /// <summary>결제한 직후, 효과가 나기 전.</summary>
    void OnPurchased(ItemData item);

    float RollResistance(TraitType trait, int genetics);

    // 심기
    void PlantSeedling(List<GeneticTrait> traits);
    void PlantFixed(PlantKind kind, int cell);
    void RefreshTraits(PlantState plant);

    // 땅
    void ExpandField();
    void AddLandRichColumn();
    bool PlaceFertilizer(int cell, WaveType wave);
    bool PlaceGoldSoil(int cell);
    void PlacePetBottle(int cell);
    void AddAbsorbFertilizer(int cell);
    void FreezeArea(int cell);

    // 하루
    void AddBreedAttempts(int count);
    void AddBreedSeconds(int seconds);
    void AddWaveSkips(int count);
    void AddTimedMultiplier(StatId stat, int param, float multiplier, int days, string sourceTag);

    // 값을 올리면서 밭 위 식물과 표시도 바뀌는 강화. 단순 수치는 Field.Modifiers를 바로 바꾼다.
    void AddWeakGeneticsResistanceBonus(float value);
    void AddStrongGeneticsResistanceBonus(float value);
    void AddSprinklerFertilizerSynergyBonus(float value);
    void AddChiliPepperRangeLevel(int value);
    void EnableNepenthesPheromone();
    void AddNepenthesPheromoneSize(float value);
    void AddAdditionalPlantGold(int value);
    void AddAdditionalPlantGoldMultiplier(float value);
    void AddPeaCoffeeMultiplier(float value);
    void AddLandAndBeanLevel(int value);
}

/// <summary>아이템에 넘기는 묶음. 세계와 상점 상태, 안내 문구 출력.</summary>
public class ShopContext
{
    private static ShopContext game;

    /// <summary>지금 게임의 컨텍스트. 아이템 설명 UI처럼 상점을 거치지 않는 곳에서 쓴다.</summary>
    public static ShopContext ForGame => game ??= new ShopContext(new GridShopWorld());

    public ShopContext(IShopWorld world)
    {
        World = world;
    }

    public IShopWorld World { get; }
    public FieldState Field => World.Field;
    public PlantBoard Board => World.Field.Board;
    public GridModifiers Modifiers => World.Field.Modifiers;
    public SoilState Soil => World.Field.Soil;
    public ShopState Shop => GameRun.Current.Shop;
    public int Stage => GameRun.Current.Stage;

    public Action<string> ShowInfo;
    public Action<string> ShowError;
}
