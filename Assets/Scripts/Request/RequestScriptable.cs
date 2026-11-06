using UnityEngine;
using System.Collections.Generic;

public enum RequestDifficulty { Easy, Normal, Hard }
public enum RewardType { Gold, Gene }

[System.Serializable]
public struct RewardEntry
{
    public RewardType type;
    public int amount;
}

[CreateAssetMenu(menuName = "Request/RequestItem")]
[LocGroup(LocPrefix, "title", "description")]
public class RequestScriptable : ScriptableObject
{
    private const string LocPrefix = "request";

    public string requestId;

    [Min(0), Tooltip("이 일차부터 후보에 오른다. 0이면 처음부터 나온다.")]
    public int minStage;

    public RequestDifficulty requestDifficulty;

    public List<RewardEntry> rewards = new List<RewardEntry>();



    /// <summary>화면에 보여줄 제목. 자리표시자 문법은 <see cref="LocFormat"/> 참고.</summary>
    public string GetTitle(params (string name, object value)[] args)
        => Loc.T(LocKey.For(LocPrefix, name, "title"), args);

    /// <summary>화면에 보여줄 설명.</summary>
    public string GetDescription(params (string name, object value)[] args)
        => Loc.Or(LocKey.For(LocPrefix, name, "description"), "", args);
}
