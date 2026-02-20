using UnityEngine;

public class SeedlessCurse : CurseInstance
{
    public SeedlessCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("꽃가루 실종 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("꽃가루 실종 끝");
    }
}
