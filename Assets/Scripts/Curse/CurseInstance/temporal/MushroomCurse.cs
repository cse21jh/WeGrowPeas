using UnityEngine;

public class MushroomCurse : CurseInstance
{
    public MushroomCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("버섯 발생 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("버섯 발생 끝");
    }
}
