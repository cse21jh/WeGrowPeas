using UnityEngine;

// 202 돌연변이: 교배 시 변종 발생 확률 +%p. Plant의 변종 판정이 CurseState를 읽어 반영.
public class MutationCurse : CurseInstance
{
    public MutationCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.MutationAddPercent = Lv != null ? Lv.valueA : 0f;
    }

    public override void Deactivate()
    {
        CurseState.MutationAddPercent = 0f;
    }
}
