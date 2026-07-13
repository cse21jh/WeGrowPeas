using UnityEngine;

public class RearrangeCurse : CurseInstance
{
    public RearrangeCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        float ratio = (Lv != null ? Lv.valueA : 0f) / 100f;
        GameManager.Instance?.grid?.RearrangePlants(ratio);
    }

    public override void Deactivate()
    {
        // 단발성: 해제 시 별도 처리 없음
    }
}
