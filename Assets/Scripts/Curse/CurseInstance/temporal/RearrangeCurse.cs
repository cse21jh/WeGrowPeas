using UnityEngine;

public class RearrangeCurse : CurseInstance
{
    public RearrangeCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        float ratio = (Lv != null ? Lv.valueA : 0f) / 100f;
        if (ratio <= 0f) ratio = 1f; // 데이터 세팅 누락 시 전체(100%) 이동
        GameManager.Instance?.grid?.RearrangePlants(ratio);
        CurseEffectManager.Instance?.PlayAppearParticle();
    }

    public override void Deactivate()
    {
        // 단발성: 해제 시 별도 처리 없음
    }
}
