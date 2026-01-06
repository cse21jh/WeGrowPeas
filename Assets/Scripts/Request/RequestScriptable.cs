using UnityEngine;

public enum RequestDifficulty { Easy, Normal, Hard }

[CreateAssetMenu(menuName = "Request/RequestItem")]
public class RequestScriptable : ScriptableObject
{
    //[Header()]
    public string requestId;

    public string npcName;

    public RequestDifficulty requestDifficulty;

    public int reward;

    [TextArea] public string requestDefinition;
    
}
