using System.Collections.Generic;
using UnityEngine;

/// <summary>저주가 밭과 식물에 주는 효과. <see cref="CurseInstance"/>와 <see cref="CurseEffectManager"/>가 호출한다.</summary>
public class GridCurseActions
{
    private readonly Grid grid;

    public GridCurseActions(Grid grid)
    {
        this.grid = grid;
    }

    /// <summary>저주(꽃가루 실종): 필드 식물 중 ratio(0~1)만큼 무작위로 교배 불가. 나머지는 복구.</summary>
    public void ApplyPollenLost(float ratio)
    {
        var plants = new List<Plant>(grid.plantGrid.Values);
        foreach (var p in plants) if (p != null) p.SetBreedable(true);
        if (ratio <= 0f || plants.Count == 0) return;

        int count = Mathf.FloorToInt(plants.Count * ratio); // 소수점 버림
        ShuffleList(plants);
        for (int i = 0; i < count && i < plants.Count; i++)
            if (plants[i] != null) plants[i].SetBreedable(false);

        // 대상이 새로 굴려졌으므로 교배 불가 식물의 색 표시도 갱신
        CurseEffectManager.Instance?.RefreshPollenPlants(true);
    }
    /// <summary>저주(도둑): 페트병으로 보호되지 않은 무작위 식물 count개 제거.</summary>
    public void StealPlants(int count)
    {
        if (count <= 0) return;
        var candidates = new List<int>();
        foreach (var kv in grid.plantGrid)
            if (kv.Value != null && !grid.HasPetBottle(kv.Key)) candidates.Add(kv.Key);
        ShuffleList(candidates);

        int removed = 0;
        for (int i = 0; i < candidates.Count && removed < count; i++)
        {
            int idx = candidates[i];
            if (grid.plantGrid.TryGetValue(idx, out Plant p) && p != null)
            {
                grid.plantGrid.Remove(idx);
                Object.Destroy(p.gameObject); // TODO: 식물 날아가는 이펙트
                removed++;
            }
        }
        if (removed > 0)
        {
            grid.NotifyGridStateChanged();
            if (grid.CheckGameOver()) grid.StartCoroutine(GameManager.Instance.GameOver());
        }
    }
    /// <summary>저주(대격변): 이동 가능하고 페트병 없는 식물 중 ratio만큼을 무작위로 위치 교환.</summary>
    public void RearrangePlants(float ratio)
    {
        if (ratio <= 0f) return;
        var idxs = new List<int>();
        foreach (var kv in grid.plantGrid)
            if (kv.Value != null && kv.Value.IsMovable && !grid.HasPetBottle(kv.Key)) idxs.Add(kv.Key);
        if (idxs.Count < 1) return;

        int affected = Mathf.FloorToInt(idxs.Count * ratio);
        if (affected < 1) affected = 1;
        affected = Mathf.Min(affected, idxs.Count);

        ShuffleList(idxs);
        var selectedPlantsIdx = idxs.GetRange(0, affected);
        var plantsToMove = new List<Plant>();
        foreach (var idx in selectedPlantsIdx) plantsToMove.Add(grid.plantGrid[idx]);

        // Remove from grid temporarily to free up space
        foreach (var idx in selectedPlantsIdx)
        {
            grid.plantGrid.Remove(idx);
        }

        // Find all empty spaces including the ones we just vacated
        var emptySpaces = new List<int>();
        for (int i = 0; i < grid.maxCol * 4; i++)
        {
            if (!grid.plantGrid.TryGetValue(i, out Plant p) || p == null)
            {
                emptySpaces.Add(i);
            }
        }

        ShuffleList(emptySpaces);

        // Place them in new random empty spaces
        for (int i = 0; i < plantsToMove.Count; i++)
        {
            int newIdx = emptySpaces[i];
            Plant p = plantsToMove[i];
            grid.plantGrid[newIdx] = p;
            p.SetGridIndex(newIdx);
            p.transform.position = grid.GetSoilTransform(newIdx).position;
        }
        grid.NotifyGridStateChanged();
    }
    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    public void RefreshCurseEffects()
    {
        foreach (var plant in grid.plantGrid.Values)
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
        foreach (var plant in grid.plantGrid.Values)
        {
            if (plant != null) plant.PlayMadnessEffect();
        }
    }
}
