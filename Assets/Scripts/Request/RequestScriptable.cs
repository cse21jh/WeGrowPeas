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
public class RequestScriptable : ScriptableObject
{
    public string requestId;

    public string npcName;

    public RequestDifficulty requestDifficulty;

    public List<RewardEntry> rewards = new List<RewardEntry>();

    [TextArea] public string requestTitle;

    [TextArea] public string requestDescription;
    
}
