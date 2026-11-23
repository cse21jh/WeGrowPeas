/// <summary>하루 진행(<see cref="DayFlow"/>)을 실제 게임에 잇는다.</summary>
public sealed class GameDayWorld : IDayWorld
{
    private readonly GameManager game;

    public GameDayWorld(GameManager game)
    {
        this.game = game;
        Actions = new GridFieldActions(game.grid);
    }

    public FieldState Field => game.grid.Field;
    public WaveState Waves => game.enemyController.Waves;
    public ModState Mods => game.modManager.State;
    public CurseBoard Curses => CurseManager.Instance.Board;
    public RequestBoard Requests => game.requestManager.Board;
    public TaxState Tax => TaxManager.Instance.State;
    public IFieldActions Actions { get; }

    public bool CursesActive => GameRun.Dawn.curseLevel > 0; // 새벽 저주만. 무한 모드 자체의 상시 저주는 없다
    public bool RequestsActive => true;
    public bool SpecialItemsActive => true;
    public int RemainingBreeds => game.grid.Breeder.LastRemainingBreeds;

    public void StartRequests() => game.requestManager.StartNewCycle(game.stage);

    public void AddRefund(int gold)
    {
        if (gold <= 0) return;
        game.economyManager.AddGold(gold);
        UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.card_refund", ("gold", gold)), 1);
    }

    public void CountFreeTime(PlantState plant) => game.grid.Board.Plants[plant.GridIndex].OnFreeTimePassed();
    public void RaiseDayEndedWithRemainingBreeds(int remaining) => GameEvents.RaiseDayEndedWithRemainingBreeds(remaining);
    public void RaiseQuestDayPassed() => GameEvents.RaiseQuestDayPassed();
}
