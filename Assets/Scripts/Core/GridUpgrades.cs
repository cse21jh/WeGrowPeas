using System.Collections.Generic;
using UnityEngine;

/// <summary>값을 올리면서 밭 위 식물과 가격 표시도 함께 갱신하는 강화. 단순 수치는 <see cref="GridModifiers"/>가 맡는다.</summary>
public class GridUpgrades
{
    private readonly Grid grid;
    private readonly PlantBoard board;
    private int mostExpensivePlant = 0;

    public bool showingAllPrice = false;
    public int MostExpensivePlant => mostExpensivePlant;

    public GridUpgrades(Grid grid)
    {
        this.grid = grid;
        board = grid.Board;
    }

    public void LoadMostExpensivePlant(int value)
    {
        mostExpensivePlant = value;
    }

    public void AddAdditionalResistanceInGrid(TraitType traitType, float value, bool byUpgrade = false)
    {
        for (int idx = 0; idx < board.MaxCol * 4; idx++) // grid에 있는 식물들 저항력 증가
        {
            if (board.Plants.ContainsKey(idx))
            {
                Plant plant = board.Plants[idx];
                plant.AddAdditionalResistance(traitType, value, byUpgrade);
            }
        }
        return;
    }
    public void AddAdditionalPestResistance(float value)
    {
        grid.Modifiers.AddAdditionalPestResistance(value);
        AddAdditionalResistanceInGrid(TraitType.Pest, value);
    }
    public void SetNepenthesPheromoneEnabled(bool enabled)
    {
        grid.Modifiers.SetNepenthesPheromone(enabled);
        // 기존 네펜데스들의 페로몬 활성화/비활성화
        foreach (var plant in board.Plants.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromone();
            }
        }
    }
    public void AddAdditionalNepenthesPheromoneSizeMultiplier(float value)
    {
        grid.Modifiers.AddAdditionalNepenthesPheromoneSizeMultiplier(value);
        // 기존 네펜데스들의 페로몬 크기 업데이트
        foreach (var plant in board.Plants.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromoneSize();
            }
        }
    }
    public void AddResistanceBonus(float value)
    {
        grid.Modifiers.AddResistanceBonus(value);
        // 기존 식물들에도 적용
        ApplyResistanceBonusToExistingPlants(value);
    }
    private void ApplyResistanceBonusToExistingPlants(float bonus)
    {
        for (int idx = 0; idx < board.MaxCol * 4; idx++)
        {
            if (board.Plants.ContainsKey(idx))
            {
                Plant plant = board.Plants[idx];
                if (plant is MovablePlant)
                {
                    plant.IncreaseResistance(bonus);
                }
            }
        }
    }
    public void AddWeakGeneticsResistanceBonus(float value)
    {
        grid.Modifiers.AddWeakGeneticsResistanceBonus(value);
        // 기존 식물들에도 적용
        ApplyWeakGeneticsResistanceBonusToExistingPlants(value);
    }
    private void ApplyWeakGeneticsResistanceBonusToExistingPlants(float bonus)
    {
        for (int idx = 0; idx < board.MaxCol * 4; idx++)
        {
            if (board.Plants.ContainsKey(idx))
            {
                Plant plant = board.Plants[idx];
                if (plant is MovablePlant)
                {
                    plant.IncreaseWeakGeneticsResistance(bonus);
                }
            }
        }
    }
    public void AddStrongGeneticsResistanceBonus(float value)
    {
        grid.Modifiers.AddStrongGeneticsResistanceBonus(value);
        // 기존 식물들에도 적용
        ApplyStrongGeneticsResistanceBonusToExistingPlants(value);
    }
    private void ApplyStrongGeneticsResistanceBonusToExistingPlants(float bonus)
    {
        for (int idx = 0; idx < board.MaxCol * 4; idx++)
        {
            if (board.Plants.ContainsKey(idx))
            {
                Plant plant = board.Plants[idx];
                if (plant is MovablePlant)
                {
                    plant.IncreaseStrongGeneticsResistance(bonus);
                }
            }
        }
    }
    public void AddSprinklerFertilizerSynergyBonus(float val)
    {
        grid.Modifiers.AddSprinklerFertilizerSynergyBonus(val);
        grid.Scouter.UpdateResistanceScouterImageInGrid(grid.Enemy.CurrentWave.WaveType);
    }
    public void AddChiliPepperRangeLevel(int value)
    {
        grid.Modifiers.AddChiliPepperRangeLevel(value);
        grid.Scouter.UpdateResistanceScouterImageInGrid(grid.Enemy.CurrentWave.WaveType);
    }
    public Dictionary<Plant, int> CapturePlantSellingPrices()
    {
        var previousPrices = new Dictionary<Plant, int>();
        foreach (Plant plant in board.Plants.Values)
        {
            if (plant == null || plant.isDying)
                continue;

            previousPrices[plant] = plant.GetSellingPrice();
        }

        return previousPrices;
    }
    public void RefreshPlantSellingPricesAndNotify(
        Dictionary<Plant, int> previousPrices,
        PlantValueChangeReason reason)
    {
        if (previousPrices == null)
            return;

        foreach (KeyValuePair<Plant, int> entry in previousPrices)
        {
            Plant plant = entry.Key;
            if (plant == null || plant.isDying)
                continue;

            plant.RefreshSellingPriceAndNotifyExternal(entry.Value, reason);
        }
    }
    public void RefreshAllPlantSellingPriceDisplays()
    {
        foreach (Plant plant in board.Plants.Values)
        {
            if (plant != null && !plant.isDying)
                plant.RefreshSellingPriceDisplay();
        }
    }
    public void AddAdditionalPlantGold(int value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        grid.Modifiers.AddAdditionalPlantGold(value);
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }
    public void AddAdditionalPlantGoldMultiplier(float value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        grid.Modifiers.AddAdditionalPlantGoldMultiplier(value);
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }
    /// <summary>완두커피: 자유시간이 지날 때마다 붙는 판매 골드 배수 증가.</summary>
    public void AddPeaCoffeeMultiplier(float value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        grid.Modifiers.AddPeaCoffeeMultiplier(value);
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }
    /// <summary>땅과 콩: 웨이브 후 뿌리 확률 +10%p/구매 + 뿌리내린 식물 가격 +10%p/구매.</summary>
    public void AddLandAndBeanLevel(int value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        grid.Modifiers.AddLandAndBeanLevel(value);
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }
    /// <summary>
    /// 땅과 콩: 웨이브가 지나간 후, 아직 뿌리내리지 않은 식물이 확률적으로 뿌리를 내려 이동 불가가 된다.
    /// (GameManager.BreedEndRoutine에서 웨이브 정산 시 호출)
    /// </summary>
    public void ProcessLandAndBeanRooting()
    {
        float chance = grid.Modifiers.GetLandAndBeanRootChance();
        if (chance <= 0f) return;

        foreach (var p in board.Plants.Values)
        {
            if (p is MovablePlant mp && mp.IsMovable && !mp.isDying)
            {
                if (Random.value < chance)
                    mp.SetMovable(false, PlantValueChangeReason.Rooted); // TODO: 뿌리 시각효과
            }
        }
    }
    public void ShowAllPriceSign()
    {
        showingAllPrice = true;
        foreach (var p in board.Plants)
        {
            p.Value.ShowPriceSign();
        }
    }
    public void HideAllPriceSign()
    {
        showingAllPrice = false;
        foreach (var p in board.Plants)
        {
            p.Value.HidePriceSign();
        }
    }
    public void TryUpdateMostExpensivePlant(int price)
    {
        if (mostExpensivePlant < price)
        {
            mostExpensivePlant = price;
        }
        return;
    }
}
