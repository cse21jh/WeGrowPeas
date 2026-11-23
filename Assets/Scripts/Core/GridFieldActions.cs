using System.Collections.Generic;

/// <summary>하루 규칙의 결과를 실제 게임 화면에 일으킨다.</summary>
public sealed class GridFieldActions : IFieldActions
{
    private readonly Grid grid;

    public GridFieldActions(Grid grid)
    {
        this.grid = grid;
    }

    private Plant ViewOf(PlantState plant) => grid.Board.Plants[plant.GridIndex];

    public void Survive(PlantState plant, WaveType wave) => ViewOf(plant).ResistWave(wave);

    public bool Kill(PlantState plant) => ViewOf(plant).Die();

    public void OnWaveHit(PlantState plant, WaveType wave, bool isSecondWave)
    {
        if (!isSecondWave) grid.Enemy.RecordWaveHit(wave);
        GameEvents.RaisePeaDied();
    }

    public void ResolveMushrooms() => CurseManager.Instance?.ResolveMushroomWave();

    public void Unfreeze(PlantState plant) => ViewOf(plant).SetFrozen(false);

    public void AddGold(int amount) => GameManager.Instance.economyManager.AddGold(amount);

    public void Root(PlantState plant)
        => ((MovablePlant)ViewOf(plant)).SetMovable(false, PlantValueChangeReason.Rooted); // TODO: 뿌리 시각효과

    public void RefreshTraits(PlantState plant) => ViewOf(plant).SetTrait(plant.Traits);

    public PlantState SpawnCopy(List<GeneticTrait> traits, int cell, bool benignMutation)
    {
        Plant child = grid.Spawner.AddMovablePlant(traits, cell);
        if (benignMutation) CurseEffectManager.PlayMutation(child, benign: true);
        grid.Breeder.totalPeanutBreedCount++;
        return child.State;
    }

    public void AddBonusMultiplier(PlantState plant, int amount) => ViewOf(plant).AddBonusGoldMultiplier(amount);
}
