using UnityEngine;

// 206 불면증: 밤 자유시간이 짧아짐(배율). 폰 타이머가 GameRun.Curse.InsomniaFreeTimeRatio를 읽어 반영.
public class InsomniaCurse : CurseInstance
{
    public InsomniaCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board) { }

    public override void Activate()
    {
        GameRun.Curse.InsomniaFreeTimeRatio = (Lv != null ? Lv.valueA : 100f) / 100f;
    }

    public override void Deactivate()
    {
        GameRun.Curse.InsomniaFreeTimeRatio = 1f;
    }
}
