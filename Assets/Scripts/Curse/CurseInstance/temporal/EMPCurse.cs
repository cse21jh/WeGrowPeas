using UnityEngine;

public class EMPCurse : CurseInstance
{
    public EMPCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("통신 장애 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("통신 장애 끝");
    }
}
