using UnityEngine;

public class ThiefCurse : CurseInstance
{
    public ThiefCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        int count = Mathf.RoundToInt(Lv != null ? Lv.valueA : 0f);
        Board.StealPlants(count);
        World.ShowEffect(Data.curseId, true);
    }

    public override void Deactivate()
    {
        // 단발성: 해제 시 별도 처리 없음
    }
}
