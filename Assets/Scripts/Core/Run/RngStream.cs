/// <summary>
/// 시드로 재현되는 난수열 하나. 상태가 64비트 값 하나라서 그대로 저장하고 되돌릴 수 있다.
/// Unity 없이 돌아가야 하므로 UnityEngine을 쓰지 않는다. (SplitMix64)
/// </summary>
public sealed class RngStream
{
    private ulong state;

    public RngStream(ulong seed)
    {
        state = seed;
    }

    /// <summary>저장용 상태. JsonUtility가 다루기 쉬운 long으로 주고받는다.</summary>
    public long State
    {
        get => unchecked((long)state);
        set => state = unchecked((ulong)value);
    }

    public ulong NextULong()
    {
        unchecked
        {
            ulong z = (state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>0 이상 1 미만.</summary>
    public float Value => (NextULong() >> 40) * (1f / 16777216f);

    /// <summary>min 이상 max 미만의 정수. UnityEngine.Random.Range(int, int)와 같은 규칙.</summary>
    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        ulong span = (ulong)((long)maxExclusive - minInclusive);
        ulong offset = ((NextULong() >> 32) * span) >> 32;
        return (int)(minInclusive + (long)offset);
    }

    /// <summary>min 이상 max 미만의 실수.</summary>
    public float Range(float min, float max) => min + (max - min) * Value;
}
