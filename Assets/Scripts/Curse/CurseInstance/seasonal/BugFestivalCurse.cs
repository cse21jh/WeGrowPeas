using UnityEngine;

// 201 벌레 대발생: 벌레가 2마리씩, 등장 딜레이 감소(초). 스폰 로직(Grid)이 CurseState를 읽어 반영.
public class BugFestivalCurse : CurseInstance
{
    public BugFestivalCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.BugFestival = true;
        CurseState.BugFestivalDelayReduce = Lv != null ? Lv.valueA : 0f;
    }

    public override void Deactivate()
    {
        CurseState.BugFestival = false;
        CurseState.BugFestivalDelayReduce = 0f;
    }
}
