using System.Collections.Generic;

// 시뮬레이터의 저주, 의뢰, 특수 아이템. 규칙은 게임과 같은 코드이고 여기서는 봇의 선택과 상태 변화만 정한다.
public sealed partial class RunSimulator : ICurseWorld, IRequestWorld
{
    private const int MovesPerDay = 10; // 의뢰를 채우려고 하루에 옮기는 횟수

    private static CurseScriptable[] temporalCurses;
    private static CurseScriptable[] seasonalCurses;
    private static List<RequestScriptable> requestPool;

    private readonly RunEvents events = new RunEvents();
    private readonly RequestBoard requests;
    private readonly CurseBoard curses;
    private bool isNight;

    // ───── 저주 ─────

    private bool CursesActive => config.curses && GameRun.Dawn.curseLevel > 0;

    FieldState ICurseWorld.Field => field;

    void ICurseWorld.Steal(PlantState plant) => field.Board.Remove(plant.GridIndex);

    void ICurseWorld.Rearrange(List<(PlantState plant, int cell)> moves)
    {
        foreach (var (plant, _) in moves) field.Board.Remove(plant.GridIndex);
        foreach (var (plant, cell) in moves)
        {
            field.Board.PlaceState(cell, plant);
            plant.MoveTo(cell);
        }
    }

    void ICurseWorld.SetBreedable(PlantState plant, bool breedable) => plant.Breedable = breedable;

    void ICurseWorld.PrepareSecondWave()
    {
        waves.PrepareSecond(stage);
    }

    void ICurseWorld.OnTilesChanged() { }
    void ICurseWorld.ShowEffect(string curseId, bool on) { }

    // ───── 의뢰 ─────

    RunEvents IRequestWorld.Events => events;
    int IRequestWorld.PickTraitFromWaves() => (int)waves.Deck.PickNext(currentWave, stage);
    bool IRequestWorld.IsFreeTime => isNight;

    private void ClaimRequests()
    {
        foreach (RequestInstance request in requests.Active)
        {
            if (!request.TryGrant(out int gold)) continue;
            economy.Add(gold);
            result.RequestsCompleted++;
        }
    }

    // 식물 옮기기 의뢰가 있으면 옮길 수 있는 식물 하나를 빈 칸으로 왔다 갔다 옮긴다
    private void MovePlantsForRequest()
    {
        if (config.bot != SimBotKind.Planner || !HasOpenRequest<MovePlantRequest>()) return;

        for (int i = 0; i < MovesPerDay && HasOpenRequest<MovePlantRequest>(); i++)
        {
            PlantState plant = SortedPlants().Find(p => p.CanMove());
            int cell = field.FindCellFor();
            if (plant == null || cell < 0) return;

            MovePlant(plant, cell);
        }
    }

    // Grid.TryPlacePlant → MovablePlant.OnMouseUp과 같은 순서
    private void MovePlant(PlantState plant, int cell)
    {
        field.MovePlant(plant, cell);
        events.RaisePlantMoved();
        plant.TryRootByDawn();
    }

    private bool HasOpenRequest<T>() where T : RequestInstance
    {
        foreach (RequestInstance request in requests.Active)
            if (request is T && request.State == RequestState.InProgress) return true;
        return false;
    }

    // ───── 특수 아이템 ─────

    // 10일마다 받는 선물. 봇은 후보 중 아무거나 고른다(아이템별 효과를 견줄 수 있게).
    private void TakeSpecialItem()
    {
        if (GameRun.Special.PendingGifts == 0) return;

        List<SpecialItemData> candidates = GameRun.Special.RollCandidates(config.species, 3, data => config.allUnlocked || !data.plantSpecific);
        if (candidates.Count == 0) return;

        SpecialItemData picked = candidates[botRng.Range(0, candidates.Count)];
        GameRun.Special.Acquire(picked);
        result.SpecialItems.Add(picked.id);
    }
}
