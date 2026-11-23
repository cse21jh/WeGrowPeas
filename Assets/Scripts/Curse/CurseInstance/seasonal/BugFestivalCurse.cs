using UnityEngine;

// 201 벌레 대발생: 벌레가 2마리씩, 등장 딜레이 감소(초). 스폰 로직(Grid)이 CurseState를 읽어 반영.
public class BugFestivalCurse : CurseInstance
{
    public BugFestivalCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        GameRun.Curse.BugFestival = true;
        GameRun.Curse.BugFestivalDelayReduce = Lv != null ? Lv.valueA : 0f;
        World.ShowEffect(Data.curseId, true);
    }

    public override void Deactivate()
    {
        GameRun.Curse.BugFestival = false;
        GameRun.Curse.BugFestivalDelayReduce = 0f;
        World.ShowEffect(Data.curseId, false);
    }
}
