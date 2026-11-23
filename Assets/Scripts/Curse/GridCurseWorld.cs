using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

/// <summary>저주의 효과를 실제 게임에 일으킨다.</summary>
public sealed class GridCurseWorld : ICurseWorld
{
    private readonly Action onTilesChanged;

    public GridCurseWorld(Action onTilesChanged)
    {
        this.onTilesChanged = onTilesChanged;
    }

    private static Grid Grid => GameManager.Instance.grid;

    public FieldState Field => Grid.Field;

    public void Steal(PlantState plant)
    {
        Plant view = Grid.Board.Plants[plant.GridIndex];
        Grid.ClearGridIndex(plant.GridIndex); // 밭에서 빼고 게임 오버를 판정한다
        Object.Destroy(view.gameObject); // TODO: 식물 날아가는 이펙트
    }

    public void Rearrange(List<(PlantState plant, int cell)> moves)
    {
        var views = new List<Plant>();
        foreach (var (plant, _) in moves)
        {
            views.Add(Grid.Board.Plants[plant.GridIndex]);
            Grid.Board.Remove(plant.GridIndex);
        }
        for (int i = 0; i < moves.Count; i++)
        {
            Grid.Board.Place(moves[i].cell, views[i]);
            views[i].SetGridIndex(moves[i].cell);
            views[i].transform.position = Grid.GetSoilTransform(moves[i].cell).position;
        }
    }

    public void SetBreedable(PlantState plant, bool breedable) => Grid.Board.Plants[plant.GridIndex].SetBreedable(breedable);

    public void PrepareSecondWave() => GameManager.Instance.enemyController?.UpdateSecondWaveIfNull();

    public void OnTilesChanged() => onTilesChanged?.Invoke();

    public void ShowEffect(string curseId, bool on)
    {
        CurseEffectManager effects = CurseEffectManager.Instance;
        if (effects == null) return;

        switch (curseId)
        {
            case "101": if (on) effects.SetReverseCurse(true); break;
            case "102": effects.SetFogCurse(on); break;
            case "103": if (on) effects.SetThiefCurse(true); break;
            case "104": effects.SetWaveBlind(on); break;
            case "105": effects.SetMushroomCurse(on); break;
            case "106": if (on) effects.PlayMadnessCurse(); break;
            case "107": if (on) effects.PlayAppearParticle(); break;
            case "108": effects.SetDoubleWaveCurse(on); break;
            case "109": effects.SetEMPCurse(on); break;
            case "201": effects.SetBugFestival(on); break;
            case "203": effects.SetRadioActive(on); break;
            case "204": effects.SetPollenLost(on); break; // 전용 파티클 + 교배 불가 식물 색 변화
            case "207": effects.SetWatermelon(on); break;
        }
    }
}
