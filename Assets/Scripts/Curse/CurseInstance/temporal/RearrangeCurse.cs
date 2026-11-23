using UnityEngine;

public class RearrangeCurse : CurseInstance
{
    public RearrangeCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        float ratio = (Lv != null ? Lv.valueA : 0f) / 100f;
        if (ratio <= 0f) ratio = 1f; // 데이터 세팅 누락 시 전체(100%) 이동
        Board.RearrangePlants(ratio);
        World.ShowEffect(Data.curseId, true);
    }

    public override void Deactivate()
    {
        // 단발성: 해제 시 별도 처리 없음
    }
}
