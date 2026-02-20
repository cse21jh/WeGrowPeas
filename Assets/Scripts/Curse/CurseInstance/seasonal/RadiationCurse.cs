using UnityEngine;

public class RadiationCurse : CurseInstance
{
    public RadiationCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("방사능 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("방사능 끝");
    }
}
