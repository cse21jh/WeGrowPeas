using UnityEngine;

public class RearrangeCurse : CurseInstance
{
    public RearrangeCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("대격변 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("대격변 끝");
    }
}
