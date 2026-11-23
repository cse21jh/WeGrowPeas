using System.Collections.Generic;

/// <summary>상점 아이템의 효과를 실제 게임에 일으킨다.</summary>
public sealed class GridShopWorld : IShopWorld
{
    private static GameManager Game => GameManager.Instance;
    private static Grid Grid => Game != null ? Game.grid : null;

    public FieldState Field => Grid != null ? Grid.Field : null;
    public ModState Mods => ModManager.Instance != null ? ModManager.Instance.State : null;
    public WaveType CurrentWave => Game != null && Game.enemyController != null && Game.enemyController.CurrentWave != null
        ? Game.enemyController.CurrentWave.WaveType : WaveType.None;
    public string Species => Game != null ? Game.currentPlant : null;
    public bool IsNight => WaveSchedule.IsNightNow;

    public bool TrySpend(int gold)
    {
        EconomyManager economy = Game != null ? Game.economyManager : null;
        if (economy == null || !economy.HasGold(gold)) return false;
        economy.SpendGold(gold, GoldFeedbackReason.ShopPurchase);
        return true;
    }

    public void OnPurchased(ItemData item)
    {
        GameEvents.RaiseShopBought(item);
        if (Game.Tutorial == null) CodexProgress.Discover(CodexProgress.Category.Item, item.UnlockId);
    }

    public float RollResistance(TraitType trait, int genetics) => Plant.GetResistanceBasedOnGenetics(trait, genetics);

    public void PlantSeedling(List<GeneticTrait> traits) => Grid.Spawner.AddMovablePlant(traits);

    public void PlantFixed(PlantKind kind, int cell)
    {
        switch (kind)
        {
            case PlantKind.Nepenthes: Grid.Spawner.AddNepenthes(cell); break;
            case PlantKind.ChiliPepper: Grid.Spawner.AddChiliPepper(cell); break;
            case PlantKind.Sprinkler: Grid.Spawner.AddSprinkler(cell); break;
            case PlantKind.MoneyTree: Grid.Spawner.AddMoneyTree(cell); break;
        }
    }

    public void RefreshTraits(PlantState plant) => Grid.Board.Plants[plant.GridIndex].SetTrait(plant.Traits);

    public void ExpandField() => Grid.AddSoil();
    public void AddLandRichColumn() => Grid.Soil.AddLandRichColumn();
    public bool PlaceFertilizer(int cell, WaveType wave) => Grid.Soil.TryPlaceFertilizer(cell, wave);
    public bool PlaceGoldSoil(int cell) => Grid.Soil.TryPlaceGoldSoil(cell);
    public void PlacePetBottle(int cell) => Grid.Soil.PlacePetBottle(cell);
    public void AddAbsorbFertilizer(int cell) => Grid.Soil.AddAbsorbFertilizer(cell);
    public void FreezeArea(int cell) => Grid.Soil.ApplyRapidFreezer(cell);

    public void AddBreedAttempts(int count)
    {
        BreedingController breeder = Grid.Breeder;
        bool counterWasActive = breeder.GetIsBreeding();
        int before = breeder.GetEffectiveRemainingBreedCount();
        breeder.AddMaxBreedCount(count);
        GameEvents.RaiseBreedCountFeedback(
            new BreedCountFeedbackData(count, before, breeder.GetEffectiveRemainingBreedCount(), counterWasActive));
    }

    public void AddBreedSeconds(int seconds) => Grid.Breeder.AddMaxBreedTimer(seconds);
    public void AddWaveSkips(int count) => Game.enemyController.AddWaveSkipCount(count);

    public void AddTimedMultiplier(StatId stat, int param, float multiplier, int days, string sourceTag)
        => ModManager.Instance.AddTimedMultiplier(stat, param, multiplier, days, sourceTag);

    public void AddWeakGeneticsResistanceBonus(float value) => Grid.Upgrades.AddWeakGeneticsResistanceBonus(value);
    public void AddStrongGeneticsResistanceBonus(float value) => Grid.Upgrades.AddStrongGeneticsResistanceBonus(value);
    public void AddSprinklerFertilizerSynergyBonus(float value) => Grid.Upgrades.AddSprinklerFertilizerSynergyBonus(value);
    public void AddChiliPepperRangeLevel(int value) => Grid.Upgrades.AddChiliPepperRangeLevel(value);
    public void EnableNepenthesPheromone() => Grid.Upgrades.SetNepenthesPheromoneEnabled(true);
    public void AddNepenthesPheromoneSize(float value) => Grid.Upgrades.AddAdditionalNepenthesPheromoneSizeMultiplier(value);
    public void AddAdditionalPlantGold(int value) => Grid.Upgrades.AddAdditionalPlantGold(value);
    public void AddAdditionalPlantGoldMultiplier(float value) => Grid.Upgrades.AddAdditionalPlantGoldMultiplier(value);
    public void AddPeaCoffeeMultiplier(float value) => Grid.Upgrades.AddPeaCoffeeMultiplier(value);
    public void AddLandAndBeanLevel(int value) => Grid.Upgrades.AddLandAndBeanLevel(value);
}
