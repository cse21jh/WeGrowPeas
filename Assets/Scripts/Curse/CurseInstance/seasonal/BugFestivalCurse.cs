using UnityEngine;

public class BugFestivalCurse : CurseInstance
{
    public BugFestivalCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("벌레 대발생 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("벌레 대발생 끝");
    }
}
