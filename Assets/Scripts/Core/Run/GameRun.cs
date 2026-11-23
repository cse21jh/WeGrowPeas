using System;

/// <summary>
/// 지금 진행 중인 한 판. 씬 오브젝트가 아니라서 씬 없이도 만들 수 있다.
/// 게임은 정원 씬이 시작될 때, 시뮬레이터는 매 판마다 <see cref="StartNew"/>로 새로 만든다.
/// 판마다 새 객체이므로 이전 판의 값이 남지 않는다.
/// </summary>
public sealed class GameRun
{
    private static GameRun current;

    /// <summary>정원 씬을 거치지 않은 곳(시작 화면의 완두 등)에서 불려도 동작하도록 없으면 만든다.</summary>
    public static GameRun Current => current ??= new GameRun(NewSeed(), DawnSystem.SelectedDawnStage);

    /// <summary>게임플레이 난수. <c>GameRun.Rng.Breed.Range(0, 100)</c>처럼 쓴다.</summary>
    public static GameRng Rng => Current.Random;

    public GameRng Random { get; private set; }

    /// <summary>이번 판의 상점.</summary>
    public ShopState Shop { get; } = new ShopState();

    /// <summary>지금 걸린 저주의 수치. <c>GameRun.Curse.DoubleWave</c>처럼 쓴다.</summary>
    public static CurseState Curse => Current.CurseEffects;
    public CurseState CurseEffects { get; } = new CurseState();

    /// <summary>이번 판의 특수 아이템. <c>GameRun.Special.Has("id")</c>처럼 쓴다.</summary>
    public static SpecialItemState Special => Current.SpecialItems;
    public SpecialItemState SpecialItems { get; } = new SpecialItemState();

    /// <summary>이번 판의 새벽 단계까지 누적된 제약. 저주는 여기서 처리하지 않는다.</summary>
    public static DawnCumulative Dawn => Current.DawnConstraints;
    // 이 객체는 리소스를 읽을 수 없는 때(직렬화 중)에도 만들어질 수 있어 처음 쓸 때 계산한다
    public DawnCumulative DawnConstraints => dawnConstraints ??= DawnSystem.GetCumulative(dawnStage);
    private DawnCumulative? dawnConstraints;

    /// <summary>이번 판의 새벽 단계. 0 = 새벽 모드 아님(일반).</summary>
    public int DawnStage
    {
        get => dawnStage;
        set
        {
            dawnStage = value;
            dawnConstraints = null;
        }
    }
    private int dawnStage;

    /// <summary>지금 일차.</summary>
    public int Stage { get; set; }

    private GameRun(int seed, int dawnStage)
    {
        Random = new GameRng(seed);
        DawnStage = dawnStage;
    }

#if UNITY_EDITOR
    /// <summary>자동 테스트용: 다음 판의 시드. 한 번 쓰면 지워진다.</summary>
    public static int? EditorNextSeed;
#endif

    /// <summary>새 판을 시작한다. 시드를 주면 같은 난수열이 다시 나온다.</summary>
    /// <param name="dawnStage">비우면 시작 화면에서 고른 새벽 단계.</param>
    public static GameRun StartNew(int? seed = null, int? dawnStage = null)
    {
#if UNITY_EDITOR
        seed ??= EditorNextSeed;
        EditorNextSeed = null;
#endif
        current = new GameRun(seed ?? NewSeed(), dawnStage ?? DawnSystem.SelectedDawnStage);
        return current;
    }

    /// <summary>이어하기: 저장해 둔 난수 상태로 되돌린다. 저장된 것이 없으면 그대로 둔다.</summary>
    public void RestoreRng(RngSave save)
    {
        var restored = GameRng.FromSave(save);
        if (restored != null) Random = restored;
    }

    private static int NewSeed() => Guid.NewGuid().GetHashCode();
}
