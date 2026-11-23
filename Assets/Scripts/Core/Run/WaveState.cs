/// <summary>오늘과 다음 웨이브, 건너뛰기 횟수.</summary>
public sealed class WaveState
{
    public WaveDeck Deck { get; } = new WaveDeck();

    public WaveType? Last { get; set; }
    public WaveType Current { get; set; } = WaveType.Aging;
    public WaveType? Next { get; set; }
    /// <summary>저주(이중 웨이브): 다음 웨이브와 함께 올 두 번째 웨이브.</summary>
    public WaveType? Second { get; set; }
    public int Skips { get; set; }

    /// <summary>새 게임. 첫 웨이브는 항상 자연사다.</summary>
    public void Init(int? stage)
    {
        Deck.Init(1);
        Last = Current = WaveType.Aging;
        Next = Deck.PickNext(Current, stage);
        Second = Deck.PickNextSecond(Next.Value, stage);
    }

    /// <summary>웨이브가 지나간 뒤: 다음 웨이브를 당기고 그다음을 뽑는다.</summary>
    public void Advance(int? stage)
    {
        Last = Current;
        Current = Next ?? WaveType.None;
        Next = Deck.PickNext(Current, stage);
        Second = Deck.PickNextSecond(Next.Value, stage);
    }

    /// <summary>오늘 함께 올 두 번째 웨이브. 정해 둔 것이 없으면 지금 뽑는다.</summary>
    public WaveType? GetSecondToday(int? stage) => Second ?? Deck.PickSecond(Current, stage);

    public void PrepareSecond(int? stage)
    {
        if (CurseState.DoubleWave && Second == null && Next.HasValue) Second = Deck.PickSecond(Next.Value, stage);
    }

    /// <summary>타임 머신: 오늘 웨이브를 건너뛴다.</summary>
    public bool SkipToday()
    {
        if (Skips <= 0 || Current == WaveType.None) return false;
        Current = WaveType.None;
        Skips--;
        return true;
    }
}
