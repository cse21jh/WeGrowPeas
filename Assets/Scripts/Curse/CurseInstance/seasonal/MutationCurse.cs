using UnityEngine;

// 202 돌연변이: 교배 시 변종 발생 확률 +%p. Plant의 변종 판정이 CurseState를 읽어 반영.
public class MutationCurse : CurseInstance
{
    public MutationCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        GameRun.Curse.MutationAddPercent = Lv != null ? Lv.valueA : 0f;
    }

    public override void Deactivate()
    {
        GameRun.Curse.MutationAddPercent = 0f;
    }
}
