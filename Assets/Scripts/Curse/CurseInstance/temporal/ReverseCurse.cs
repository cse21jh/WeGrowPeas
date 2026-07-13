using UnityEngine;

// 101 반란: 우성 저항 +%p, 열성 저항 -%p (실제 형질은 안 바뀜). Plant의 저항력 계산이 CurseState를 읽어 반영.
public class ReverseCurse : CurseInstance
{
    public ReverseCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.ReversePercent = Lv != null ? Lv.valueA : 0f;
    }

    public override void Deactivate()
    {
        CurseState.ReversePercent = 0f;
    }
}
