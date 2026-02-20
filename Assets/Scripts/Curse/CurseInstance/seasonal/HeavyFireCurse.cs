using UnityEngine;

public class HeavyFireCurse : CurseInstance
{
    public HeavyFireCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("집중포화 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("집중포화 끝");
    }
}
