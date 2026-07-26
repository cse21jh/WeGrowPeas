using UnityEngine;

// 207 씨 없는 수박: 교배 시 실패할 확률. Grid의 교배 로직이 CurseState.SeedlessFailPercent를 읽어 반영.
public class SeedlessCurse : CurseInstance
{
    public SeedlessCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.SeedlessFailPercent = Lv != null ? Lv.valueA : 0f;
        CurseEffectManager.Instance?.SetWatermelon(true);
    }

    public override void Deactivate()
    {
        CurseState.SeedlessFailPercent = 0f;
        CurseEffectManager.Instance?.SetWatermelon(false);
    }
}
