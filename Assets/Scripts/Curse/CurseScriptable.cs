using UnityEngine;

public enum CurseType { Temporal, Seasonal }

[CreateAssetMenu(menuName = "Curse/CurseItem")]
public class CurseScriptable : ScriptableObject
{
    public string curseId;

    public CurseType curseType;

    public string title;

    [TextArea] public string description;
}
