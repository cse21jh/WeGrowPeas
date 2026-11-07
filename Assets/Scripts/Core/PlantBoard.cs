using System;
using System.Collections.Generic;

/// <summary>밭의 칸과 식물, 무당벌레 데이터. Unity 오브젝트 없이 다룰 수 있게 판정만 모아 둔다.</summary>
public class PlantBoard
{
    public const int RowCount = 4;

    public Dictionary<int, Plant> Plants { get; } = new Dictionary<int, Plant>();
    public List<Ladybug> Ladybugs { get; } = new List<Ladybug>();
    public int MaxCol { get; set; } = 4;
    public int CellCount => MaxCol * RowCount;

    /// <summary>식물 배치, 제거, 이동처럼 밭 상태가 바뀌었을 때 발생한다.</summary>
    public event Action Changed;

    public void NotifyChanged() => Changed?.Invoke();

    public bool HasEmptyCell()
    {
        for (int idx = 0; idx < CellCount; idx++)
        {
            if (!Plants.ContainsKey(idx))
                return true;
        }
        return false;
    }

    /// <summary>게임 오버 판정에 쓰는 교배 가능 식물 수.</summary>
    public int CountLivingPlants()
    {
        int count = 0;
        for (int idx = 0; idx < CellCount; idx++)
        {
            if (HasBreedablePlantAt(idx))
                count++;
        }
        return count;
    }

    public bool HasBreedablePlantAt(int idx)
    {
        if (!Plants.TryGetValue(idx, out Plant plant) || plant == null)
            return false;
        return plant.GetType() == typeof(Pea) || plant.GetType() == typeof(Peanut);
    }

    public int CountNoTraitPlant(WaveType wave)
    {
        int count = 0;
        for (int idx = 0; idx < CellCount; idx++)
        {
            if (Plants.TryGetValue(idx, out Plant plant) && !plant.HasTrait(wave))
                count++;
        }
        return count;
    }
}
