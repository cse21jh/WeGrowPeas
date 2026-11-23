using System;
using System.Collections.Generic;

/// <summary>밭의 칸과 식물, 무당벌레 데이터. Unity 오브젝트 없이 다룰 수 있게 판정만 모아 둔다.</summary>
public class PlantBoard
{
    public const int RowCount = 4;

    private readonly Dictionary<int, Plant> plants = new Dictionary<int, Plant>();
    private readonly Dictionary<int, PlantState> states = new Dictionary<int, PlantState>();

    /// <summary>칸 번호 → 화면의 식물. 넣고 빼는 것은 <see cref="Place"/>, <see cref="Remove"/>, <see cref="Clear"/>로만 한다.</summary>
    public IReadOnlyDictionary<int, Plant> Plants => plants;

    /// <summary>
    /// 칸 번호 → 식물 상태. 규칙 계산은 이쪽만 본다.
    /// 게임에서는 <see cref="Plants"/>와 항상 같은 칸을 갖고, 시뮬레이터에서는 이쪽만 채운다.
    /// </summary>
    public IReadOnlyDictionary<int, PlantState> States => states;

    public List<Ladybug> Ladybugs { get; } = new List<Ladybug>();
    public int MaxCol { get; set; } = 4;
    public int CellCount => MaxCol * RowCount;

    /// <summary>식물 배치, 제거, 이동처럼 밭 상태가 바뀌었을 때 발생한다.</summary>
    public event Action Changed;

    public void NotifyChanged() => Changed?.Invoke();

    /// <summary>칸에 식물을 놓는다. 이미 있으면 바꿔 놓는다.</summary>
    public void Place(int idx, Plant plant)
    {
        plants[idx] = plant;
        states[idx] = plant.State;
    }

    /// <summary>씬 없이 상태만 놓는다. (시뮬레이터용)</summary>
    public void PlaceState(int idx, PlantState state)
    {
        states[idx] = state;
    }

    public bool Remove(int idx)
    {
        bool removed = states.Remove(idx);
        plants.Remove(idx);
        return removed;
    }

    public void Clear()
    {
        plants.Clear();
        states.Clear();
    }

    public bool HasEmptyCell()
    {
        for (int idx = 0; idx < CellCount; idx++)
        {
            if (!states.ContainsKey(idx))
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
        return states.TryGetValue(idx, out PlantState plant) && plant != null && plant.IsMovableKind;
    }

    public int CountNoTraitPlant(WaveType wave)
    {
        int count = 0;
        for (int idx = 0; idx < CellCount; idx++)
        {
            if (states.TryGetValue(idx, out PlantState plant) && !plant.HasTrait(wave))
                count++;
        }
        return count;
    }
}
