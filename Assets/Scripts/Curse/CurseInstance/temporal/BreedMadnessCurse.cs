using UnityEngine;

public class BreedMadnessCurse : CurseInstance
{
    public BreedMadnessCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("광란 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("광란 끝");
    }
}
