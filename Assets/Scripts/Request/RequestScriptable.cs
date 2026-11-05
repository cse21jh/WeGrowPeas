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
[LocGroup(LocPrefix)]
public class RequestScriptable : ScriptableObject
{
    private const string LocPrefix = "request";

    public string requestId;

    public string npcName;

    public RequestDifficulty requestDifficulty;

    public List<RewardEntry> rewards = new List<RewardEntry>();

    [TextArea, LocText("title")] public string requestTitle;

    [TextArea, LocText("description")] public string requestDescription;

    /// <summary>화면에 보여줄 제목. 자리표시자 문법은 <see cref="LocFormat"/> 참고.</summary>
    public string GetTitle(params (string name, object value)[] args)
        => Loc.Or(LocKey.For(LocPrefix, name, "title"), requestTitle, args);

    /// <summary>화면에 보여줄 설명.</summary>
    public string GetDescription(params (string name, object value)[] args)
        => Loc.Or(LocKey.For(LocPrefix, name, "description"), requestDescription, args);
}
