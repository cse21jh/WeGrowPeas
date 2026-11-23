using UnityEngine;

public class EMPCurse : CurseInstance
{
    public EMPCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {
    }

    public override void Activate()
    {
        CurseState.EmpBlockRatio = (Lv != null ? Lv.valueA : 0f) / 100f;
        World.ShowEffect(Data.curseId, true);
    }

    public override void Deactivate()
    {
        CurseState.EmpBlockRatio = 0f;
        World.ShowEffect(Data.curseId, false);
    }
}
