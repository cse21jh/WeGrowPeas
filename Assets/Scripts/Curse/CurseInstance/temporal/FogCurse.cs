using UnityEngine;

public class FogCurse : CurseInstance
{
    public FogCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("안개 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("안개 끝");
    }
}
