using System;
using System.Collections.Generic;

/// <summary>웨이브 추첨. 해금된 웨이브의 가중치를 들고 다음 웨이브를 뽑는다.</summary>
public sealed class WaveDeck
{
    private static readonly WaveType[] AllTypes = (WaveType[])Enum.GetValues(typeof(WaveType));

    // 뽑을 때 넣은 순서대로 훑으므로 순서가 결과에 영향을 준다. 항상 enum 순서로 채운다.
    private readonly Dictionary<WaveType, float> baseWeights = new Dictionary<WaveType, float>();

    /// <summary>웨이브별 가중치 배수(표지판 아이템 등). 없으면 1.</summary>
    public Func<WaveType, float> WeightMultiplier { get; set; }

    /// <summary>새 게임과 불러오기: 해금 일차까지 2일 이내인 웨이브를 켠다.</summary>
    public void Init(int stage)
    {
        foreach (WaveType type in AllTypes)
            baseWeights[type] = type != WaveType.None && stage + 2 >= WaveSchedule.GetUnlockStage(type) ? 1f : 0f;
    }

    /// <summary>날이 바뀔 때: 이번에 해금 일차 2일 전이 된 웨이브를 켠다.</summary>
    public void Unlock(int stage)
    {
        foreach (WaveType type in AllTypes)
        {
            if (type != WaveType.None && stage + 2 == WaveSchedule.GetUnlockStage(type))
                baseWeights[type] = 1f;
        }
    }

    /// <param name="stage">오늘 일차. 일차가 없는 씬(예전 튜토리얼)이면 null이고 여름으로 본다.</param>
    public Dictionary<WaveType, float> GetEffectiveWeights(int? stage)
    {
        var map = new Dictionary<WaveType, float>(baseWeights);

        // 지금 뽑는 웨이브가 닥치는 때(모레)의 계절에 안 나오는 웨이브는 뺀다
        Season season = stage.HasValue ? WaveSchedule.GetSeasonByStage(stage.Value + 2) : Season.Summer;

        foreach (WaveType type in AllTypes)
        {
            if (type == WaveType.None || !map.ContainsKey(type)) continue;
            if (!WaveSchedule.IsSeasonAllowed(type, season)) map[type] = 0f;
            if (WeightMultiplier != null) map[type] *= WeightMultiplier(type);
        }

        map[WaveType.None] = 0f;
        return map;
    }

    public WaveType PickNext(WaveType? current, int? stage)
    {
        // 저주(집중포화): 같은 웨이브 고정
        if (CurseState.HeavyFire && current.HasValue && current.Value != WaveType.None)
            return current.Value;

        Dictionary<WaveType, float> map = GetEffectiveWeights(stage);

        float sum = 0f;
        foreach (var kv in map) sum += kv.Value;
        if (sum <= 0f) return WaveType.Aging; // 뽑을 것이 없으면 자연사

        float r = GameRun.Rng.Wave.Range(0f, sum);
        float acc = 0f;
        foreach (var kv in map)
        {
            acc += kv.Value;
            if (r <= acc) return kv.Key;
        }
        return WaveType.Aging;
    }

    /// <summary>다음 웨이브를 정할 때 함께 정해 두는 두 번째 웨이브. 이중 웨이브가 아니면 null.</summary>
    public WaveType? PickNextSecond(WaveType next, int? stage) => CurseState.DoubleWave ? PickSecond(next, stage) : null;

    /// <summary>
    /// 저주(이중 웨이브): exclude와 다르고 이미 정식 등장 중인 웨이브 하나. 없으면 null.
    /// 가중치는 등장 2일 전부터 켜지므로(예고용) 정식 해금 여부를 한 번 더 거른다.
    /// </summary>
    public WaveType? PickSecond(WaveType exclude, int? stage)
    {
        var candidates = new List<WaveType>();
        foreach (var kv in GetEffectiveWeights(stage))
        {
            if (kv.Value <= 0f || kv.Key == exclude || kv.Key == WaveType.None) continue;
            if ((stage ?? 0) < WaveSchedule.GetUnlockStage(kv.Key)) continue;
            candidates.Add(kv.Key);
        }
        if (candidates.Count == 0) return null;
        return candidates[GameRun.Rng.Wave.Range(0, candidates.Count)];
    }
}
