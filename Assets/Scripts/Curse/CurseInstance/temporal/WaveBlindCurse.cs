using UnityEngine;

public class WaveBlindCurse : CurseInstance
{
    public WaveBlindCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("기상 이변 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("기상 이변 끝");
    }
}
