using UnityEngine;

public class InsomniaCurse : CurseInstance
{
    public InsomniaCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("불면증 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("불면증 끝");
    }
}
