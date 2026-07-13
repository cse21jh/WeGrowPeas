using UnityEngine;

// 104 기상이변: 웨이브가 무작위로 바뀌고 유형 확인 불가. EnemyController/UI가 CurseState.WaveBlind를 읽어 반영.
// NOTE: Notion상 며칠(days)간 지속이나, 현재 단발형은 1일 제거 모델 → 다일 지속은 단발형 duration 카운터 추가 시 반영(후속).
public class WaveBlindCurse : CurseInstance
{
    public WaveBlindCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.WaveBlind = true;
    }

    public override void Deactivate()
    {
        CurseState.WaveBlind = false;
    }
}
