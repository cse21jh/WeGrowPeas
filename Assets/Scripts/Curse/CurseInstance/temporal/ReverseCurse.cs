using UnityEngine;

public class ReverseCurse : CurseInstance
{
    public ReverseCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("반란 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("반란 끝");
    }
}
