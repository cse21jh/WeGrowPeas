using System;
using System.Collections.Generic;

/// <summary>
/// 저주가 바꾸는 세계. 게임은 <see cref="GridCurseWorld"/>(화면 처리 포함), 시뮬레이터는 상태만 바꾸는 구현을 쓴다.
/// 수치만 바꾸는 저주는 <see cref="CurseState"/>를 바로 고친다.
/// </summary>
public interface ICurseWorld
{
    FieldState Field { get; }
    /// <summary>식물을 밭에서 없앤다. 죽음이 아니라서 페트병 소모나 사망 보너스가 없다.</summary>
    void Steal(PlantState plant);
    void Rearrange(List<(PlantState plant, int cell)> moves);
    void SetBreedable(PlantState plant, bool breedable);
    void PrepareSecondWave();
    /// <summary>안개와 버섯 칸이 바뀌었다.</summary>
    void OnTilesChanged();
    /// <summary>저주의 연출을 켜고 끈다.</summary>
    void ShowEffect(string curseId, bool on);
}

/// <summary>진행 중인 저주와 선택 규칙. 단발형과 지속형이 하나씩 동시에 걸릴 수 있다.</summary>
public sealed class CurseBoard
{
    private const int SeasonInterval = 5; // 지속형 저주 단위(계절 = 5일)

    private readonly IReadOnlyList<CurseScriptable> temporalPool;
    private readonly IReadOnlyList<CurseScriptable> seasonalPool;

    public CurseBoard(IReadOnlyList<CurseScriptable> temporalPool, IReadOnlyList<CurseScriptable> seasonalPool, ICurseWorld world)
    {
        this.temporalPool = temporalPool;
        this.seasonalPool = seasonalPool;
        World = world;
    }

    public ICurseWorld World { get; }
    public CurseInstance Temporal { get; private set; }
    public CurseInstance Seasonal { get; private set; }
    public int RemainingTemporalDays { get; private set; }
    public int RemainingSeasonalDays { get; private set; }

    /// <summary>안개 낀 칸. 그 칸 식물의 저항력을 볼 수 없다.</summary>
    public List<int> FogTiles { get; } = new List<int>();
    /// <summary>버섯이 난 칸. 웨이브가 지나갈 때 그 칸 식물이 피해를 본다.</summary>
    public List<int> MushroomTiles { get; } = new List<int>();

    /// <summary>이번 판의 저주 레벨(1~3).</summary>
    public static int Level => Math.Max(1, DawnSystem.Current.curseLevel);

    // ───── 하루 ─────

    /// <summary>
    /// 밤: 내일 걸릴 저주를 정한다. 단발형은 매 계절 3일차, 지속형은 첫 계절을 빼고 매 계절 1일차부터 걸린다.
    /// </summary>
    public void Select(int stage)
    {
        int tomorrow = stage + 1;
        if (tomorrow % SeasonInterval == 3) SelectTemporal(stage);
        if (tomorrow > SeasonInterval && tomorrow % SeasonInterval == 1) SelectSeasonal(tomorrow);
    }

    private void SelectTemporal(int stage)
    {
        var valid = new List<CurseScriptable>();
        foreach (CurseScriptable curse in temporalPool)
            if (curse != null && stage >= curse.unlockStage) valid.Add(curse);
        if (valid.Count == 0) return;

        Force(valid[GameRun.Rng.Curse.Range(0, valid.Count)], Level);
    }

    // 내일부터 걸리므로 내일 일차로 해금을 따진다
    private void SelectSeasonal(int tomorrow)
    {
        var valid = new List<CurseScriptable>();
        foreach (CurseScriptable curse in seasonalPool)
            if (curse != null && tomorrow >= curse.unlockStage) valid.Add(curse);
        if (valid.Count == 0) return;

        Force(valid[GameRun.Rng.Curse.Range(0, valid.Count)], Level);
    }

    /// <summary>저주 하나를 건다. 효과는 <see cref="Apply"/>에서 시작된다.</summary>
    public CurseInstance Force(CurseScriptable data, int level)
    {
        CurseInstance curse = Create(data, level);
        if (data.curseType == CurseType.Temporal)
        {
            Temporal = curse;
            CurseLevel values = data.GetLevel(level);
            RemainingTemporalDays = Math.Max(1, values != null ? values.days : 0); // 여러 날 가는 것(기상이변, 이중 웨이브) 말고는 1일
        }
        else
        {
            Seasonal = curse;
            RemainingSeasonalDays = SeasonInterval;
        }
        return curse;
    }

    /// <summary>낮이 시작될 때: 걸린 저주를 발동한다. 여러 날 가는 저주는 매일 다시 발동한다.</summary>
    public void Apply()
    {
        Temporal?.Activate();
        Seasonal?.Activate();
    }

    /// <summary>웨이브 뒤: 남은 날을 줄이고 끝난 저주를 푼다.</summary>
    public void OnDayPassed()
    {
        if (Temporal != null && --RemainingTemporalDays <= 0)
        {
            Temporal.Deactivate();
            Temporal = null;
        }
        if (Seasonal != null && --RemainingSeasonalDays <= 0)
        {
            Seasonal.Deactivate();
            Seasonal = null;
        }
    }

    /// <summary>[디버그] 걸린 저주를 모두 푼다.</summary>
    public void Clear()
    {
        Temporal?.Deactivate();
        Seasonal?.Deactivate();
        Temporal = Seasonal = null;
        RemainingTemporalDays = RemainingSeasonalDays = 0;
    }

    // ───── 칸과 식물을 건드리는 저주 ─────

    public void SpawnFog(int count) => SetTiles(FogTiles, count);
    public void SpawnMushrooms(int count) => SetTiles(MushroomTiles, count);
    public void ClearFog() => SetTiles(FogTiles, 0);
    public void ClearMushrooms() => SetTiles(MushroomTiles, 0);

    private void SetTiles(List<int> tiles, int count)
    {
        tiles.Clear();
        if (count > 0)
        {
            var cells = new List<int>();
            for (int i = 0; i < World.Field.Board.CellCount; i++) cells.Add(i);
            Shuffle(cells);
            tiles.AddRange(cells.GetRange(0, Math.Min(count, cells.Count)));
        }
        World.OnTilesChanged();
    }

    /// <summary>도둑: 페트병이 없는 식물 중 count포기를 가져간다.</summary>
    public void StealPlants(int count)
    {
        var targets = new List<PlantState>();
        foreach (PlantState plant in World.Field.Board.StatesInOrder())
            if (!World.Field.Soil.HasPetBottle(plant.GridIndex)) targets.Add(plant);
        if (count <= 0 || targets.Count == 0) return;

        Shuffle(targets);
        int stolen = 0;
        foreach (PlantState plant in targets)
        {
            if (stolen >= count) break;
            // 앞 식물이 사라지면서 함께 죽었을 수 있다(돈나무)
            if (!World.Field.Board.States.TryGetValue(plant.GridIndex, out PlantState current) || current != plant) continue;
            World.Steal(plant);
            stolen++;
        }
        World.Field.Board.NotifyChanged();
    }

    /// <summary>대격변: 옮길 수 있고 페트병이 없는 식물 중 일부를 무작위 빈 칸으로 옮긴다.</summary>
    public void RearrangePlants(float ratio)
    {
        PlantBoard board = World.Field.Board;
        var targets = new List<PlantState>();
        foreach (PlantState plant in board.StatesInOrder())
            if (plant.IsMovable && !World.Field.Soil.HasPetBottle(plant.GridIndex)) targets.Add(plant);
        if (ratio <= 0f || targets.Count == 0) return;

        Shuffle(targets);
        int affected = Math.Min(targets.Count, Math.Max(1, (int)Math.Floor(targets.Count * ratio)));
        targets.RemoveRange(affected, targets.Count - affected);

        // 옮길 식물이 있던 칸도 빈 칸으로 친다
        var empty = new List<int>();
        for (int cell = 0; cell < board.CellCount; cell++)
            if (!board.States.TryGetValue(cell, out PlantState plant) || targets.Contains(plant)) empty.Add(cell);
        Shuffle(empty);

        var moves = new List<(PlantState, int)>();
        for (int i = 0; i < targets.Count; i++) moves.Add((targets[i], empty[i]));
        World.Rearrange(moves);
        board.NotifyChanged();
    }

    /// <summary>꽃가루 실종: 식물 중 일부를 교배 불가로 하고 나머지는 되돌린다.</summary>
    public void ApplyPollenLost(float ratio)
    {
        var plants = new List<PlantState>(World.Field.Board.StatesInOrder());
        foreach (PlantState plant in plants) World.SetBreedable(plant, true);
        if (ratio <= 0f) return;

        int count = (int)Math.Floor(plants.Count * ratio);
        Shuffle(plants);
        for (int i = 0; i < count && i < plants.Count; i++) World.SetBreedable(plants[i], false);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = GameRun.Rng.Curse.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ───── 생성과 저장 ─────

    private CurseInstance Create(CurseScriptable data, int level)
    {
        switch (data.curseId)
        {
            case "101": return new ReverseCurse(data, level, this);
            case "102": return new FogCurse(data, level, this);
            case "103": return new ThiefCurse(data, level, this);
            case "104": return new WaveBlindCurse(data, level, this);
            case "105": return new MushroomCurse(data, level, this);
            case "106": return new BreedMadnessCurse(data, level, this);
            case "107": return new RearrangeCurse(data, level, this);
            case "108": return new DoubleWaveCurse(data, level, this);
            case "109": return new EMPCurse(data, level, this);
            case "201": return new BugFestivalCurse(data, level, this);
            case "202": return new MutationCurse(data, level, this);
            case "203": return new RadiationCurse(data, level, this);
            case "204": return new PollenLostCurse(data, level, this);
            case "205": return new ShopMonopolyCurse(data, level, this);
            case "206": return new InsomniaCurse(data, level, this);
            case "207": return new SeedlessCurse(data, level, this);
            case "208": return new HeavyFireCurse(data, level, this);
            default: return null;
        }
    }

    public void Save(CurseSave save)
    {
        save.curseId[0] = Temporal?.Data.curseId;
        save.curseId[1] = Seasonal?.Data.curseId;
        save.remainTempCurseDay = RemainingTemporalDays;
        save.remainSeasonCurseDay = RemainingSeasonalDays;
    }

    public void Load(CurseSave save)
    {
        Temporal = Find(temporalPool, save.curseId[0]);
        Seasonal = Find(seasonalPool, save.curseId[1]);
        RemainingTemporalDays = save.remainTempCurseDay;
        RemainingSeasonalDays = save.remainSeasonCurseDay;
    }

    private CurseInstance Find(IReadOnlyList<CurseScriptable> pool, string id)
    {
        foreach (CurseScriptable data in pool)
            if (data != null && data.curseId == id) return Create(data, Level);
        return null;
    }
}
