using System;
using System.Collections.Generic;

/// <summary>
/// 한 판의 게임플레이 난수. 시드 하나에서 용도별 난수열을 따로 뽑는다.
///
/// 용도를 나눈 이유: 교배를 몇 번 했는지, 벌레가 몇 마리 나왔는지에 따라
/// 다음 웨이브 추첨 결과가 달라지면 같은 시드로 두 판을 비교할 수 없다.
/// 난수열이 따로면 한쪽을 얼마나 썼든 다른 쪽 결과는 그대로다.
///
/// 연출용 난수(번개 간격, 코인 흩뿌리기 등)는 여기 넣지 않고 UnityEngine.Random을 그대로 쓴다.
/// </summary>
public sealed class GameRng
{
    // 순서가 곧 저장 순서다. 새 난수열은 맨 뒤에만 추가한다.
    private enum Id { Wave, Breed, Survival, Field, Curse, Item, Bug, Count }

    private readonly RngStream[] streams = new RngStream[(int)Id.Count];

    public int Seed { get; }

    /// <summary>다음 웨이브 추첨.</summary>
    public RngStream Wave => streams[(int)Id.Wave];
    /// <summary>교배 성공 여부, 자식 형질과 저항력, 변종.</summary>
    public RngStream Breed => streams[(int)Id.Breed];
    /// <summary>웨이브를 버티는지 판정.</summary>
    public RngStream Survival => streams[(int)Id.Survival];
    /// <summary>밭에서 일어나는 일. 뿌리내림, 땅콩 자가번식, 스프링클러 대상, 맛.</summary>
    public RngStream Field => streams[(int)Id.Field];
    /// <summary>저주 추첨과 저주가 고르는 대상.</summary>
    public RngStream Curse => streams[(int)Id.Curse];
    /// <summary>상점 시드, 특수 아이템 후보, 아이템 효과.</summary>
    public RngStream Item => streams[(int)Id.Item];
    /// <summary>벌레 종류, 등장 위치, 이동 목표.</summary>
    public RngStream Bug => streams[(int)Id.Bug];

    public GameRng(int seed)
    {
        Seed = seed;
        var seeder = new RngStream(unchecked((ulong)(uint)seed));
        for (int i = 0; i < streams.Length; i++)
            streams[i] = new RngStream(seeder.NextULong());
    }

    public void Save(RngSave save)
    {
        save.seed = Seed;
        save.states.Clear();
        foreach (var stream in streams)
            save.states.Add(stream.State);
    }

    /// <summary>저장된 상태가 없으면(이 기능 이전의 세이브) null.</summary>
    public static GameRng FromSave(RngSave save)
    {
        if (save == null || save.states == null || save.states.Count == 0)
            return null;

        var rng = new GameRng(save.seed);
        int count = Math.Min(save.states.Count, rng.streams.Length);
        for (int i = 0; i < count; i++)
            rng.streams[i].State = save.states[i];
        return rng;
    }
}

[Serializable]
public class RngSave
{
    public int seed;
    public List<long> states = new();
}
