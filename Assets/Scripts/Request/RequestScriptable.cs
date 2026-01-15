using UnityEngine;

public enum RequestDifficulty { Easy, Normal, Hard }

[CreateAssetMenu(menuName = "Request/RequestItem")]
public class RequestScriptable : ScriptableObject
{
    public string requestId;

    public string npcName;

    public RequestDifficulty requestDifficulty;

    public int reward;

    [TextArea] public string requestTitle;

    [TextArea] public string requestDescription;
    
}
