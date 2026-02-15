using UnityEngine;

public class ThiefCurse : CurseInstance
{
    public ThiefCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("도둑이야! 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("도둑이야! 끝");
    }
}
