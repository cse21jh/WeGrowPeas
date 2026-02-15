using UnityEngine;

public class DoubleWaveCurse : CurseInstance
{
    public DoubleWaveCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("이중 웨이브 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("이중 웨이브 끝");
    }
}
