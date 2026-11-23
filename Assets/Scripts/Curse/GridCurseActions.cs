using System.Collections.Generic;
using UnityEngine;

/// <summary>저주가 밭과 식물에 주는 효과. <see cref="CurseInstance"/>와 <see cref="CurseEffectManager"/>가 호출한다.</summary>
public class GridCurseActions
{
    private readonly Grid grid;
    private readonly PlantBoard board;

    public GridCurseActions(Grid grid)
    {
        this.grid = grid;
        board = grid.Board;
    }

    public void RefreshCurseEffects()
    {
        foreach (var plant in board.Plants.Values)
        {
            if (plant != null)
            {
                bool isFogged = CurseManager.Instance != null && CurseManager.Instance.IsFogged(plant.gridIndex);
                bool isMushroom = CurseManager.Instance != null && CurseManager.Instance.IsMushroom(plant.gridIndex);
                plant.SetFogEffect(isFogged);
                plant.SetMushroomEffect(isMushroom);
            }
        }

        if (FenceUIManager.Instance != null)
        {
            FenceUIManager.Instance.RefreshCurseEffects();
        }
    }
    public void PlayAllPlantsMadness()
    {
        foreach (var plant in board.Plants.Values)
        {
            if (plant != null) plant.PlayMadnessEffect();
        }
    }
}
