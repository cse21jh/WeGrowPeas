using UnityEngine;

// 109 통신장애: 낮 시간의 일정 비율 동안 휴대폰 확인 불가. PhoneManager가 CurseState.EmpBlockRatio를 읽어 반영.
public class EMPCurse : CurseInstance
{
    public EMPCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.EmpBlockRatio = (Lv != null ? Lv.valueA : 0f) / 100f;
    }

    public override void Deactivate()
    {
        CurseState.EmpBlockRatio = 0f;
    }
}
