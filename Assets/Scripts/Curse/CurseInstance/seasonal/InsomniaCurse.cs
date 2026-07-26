using UnityEngine;

// 206 불면증: 밤 자유시간이 짧아짐(배율). 폰 타이머가 CurseState.InsomniaFreeTimeRatio를 읽어 반영.
public class InsomniaCurse : CurseInstance
{
    public InsomniaCurse(CurseScriptable data, int level) : base(data, level) { }

    public override void Activate()
    {
        CurseState.InsomniaFreeTimeRatio = (Lv != null ? Lv.valueA : 100f) / 100f;
    }

    public override void Deactivate()
    {
        CurseState.InsomniaFreeTimeRatio = 1f;
    }
}
