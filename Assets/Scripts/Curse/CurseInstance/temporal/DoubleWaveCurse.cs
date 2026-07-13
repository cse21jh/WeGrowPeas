using UnityEngine;

// 108 이중 웨이브: 서로 다른 웨이브 2개 동시 발생. EnemyController가 CurseState.DoubleWave를 읽어 반영.
// NOTE: Notion상 며칠(days)간 지속이나, 현재 단발형은 1일 제거 모델 → 다일 지속은 단발형 duration 카운터 추가 시 반영(후속).
public class DoubleWaveCurse : CurseInstance
{
    public DoubleWaveCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.DoubleWave = true;
    }

    public override void Deactivate()
    {
        CurseState.DoubleWave = false;
    }
}
