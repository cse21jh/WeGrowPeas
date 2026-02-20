using UnityEngine;

public class MutationCurse : CurseInstance
{
    public MutationCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("돌연변이 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("돌연변이 끝");
    }
}
