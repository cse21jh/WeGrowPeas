using UnityEngine;

// 203 방사능: 매일 모든 저항력 추가 감소 %p. Plant의 일일 저항 감소(ResistWave)가 CurseState를 읽어 반영.
public class RadiationCurse : CurseInstance
{
    public RadiationCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.RadiationDecayPercent = Lv != null ? Lv.valueA : 0f;
        CurseEffectManager.Instance?.SetRadioActive(true);
    }

    public override void Deactivate()
    {
        CurseState.RadiationDecayPercent = 0f;
        CurseEffectManager.Instance?.SetRadioActive(false);
    }
}
