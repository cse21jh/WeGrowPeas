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
    public static GameRun Current => current ??= new GameRun(NewSeed());

    /// <summary>게임플레이 난수. <c>GameRun.Rng.Breed.Range(0, 100)</c>처럼 쓴다.</summary>
    public static GameRng Rng => Current.Random;

    public GameRng Random { get; private set; }

    private GameRun(int seed)
    {
        Random = new GameRng(seed);
    }

    /// <summary>새 판을 시작한다. 시드를 주면 같은 난수열이 다시 나온다.</summary>
    public static GameRun StartNew(int? seed = null)
    {
        current = new GameRun(seed ?? NewSeed());
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
