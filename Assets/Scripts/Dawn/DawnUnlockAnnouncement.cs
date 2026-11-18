/// <summary>
/// 이번 런에서 새로 열린 새벽 단계 범위. 런이 끝난 뒤 결과 화면에서 해금 연출로 한 번 보여 준다.
/// 씬을 넘어가야 하므로 정적 값으로 들고 있는다.
/// </summary>
public static class DawnUnlockAnnouncement
{
    public static bool HasPending { get; private set; }
    /// <summary>해금 전 최고 단계. 0이면 새벽 모드가 처음 열린 것이다.</summary>
    public static int From { get; private set; }
    public static int To { get; private set; }

    /// <summary>해금이 실제로 일어났을 때만 기록한다. 여러 번 열리면 처음 값부터 마지막 값까지로 합친다.</summary>
    public static void Record(int before, int after)
    {
        if (after <= before) return;
        if (!HasPending) From = before;
        To = after;
        HasPending = true;
    }

    /// <summary>보여 줄 해금이 있으면 꺼내고 비운다.</summary>
    public static bool TryConsume(out int from, out int to)
    {
        from = From;
        to = To;
        if (!HasPending) return false;
        Clear();
        return true;
    }

    public static void Clear()
    {
        HasPending = false;
        From = To = 0;
    }
}
