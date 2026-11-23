using UnityEngine;

// 208 집중포화: 5턴간 하나의 웨이브만 등장하며 해당 웨이브 저항이 추가로 빠르게 감소.
// EnemyController(웨이브 선택)와 Plant(저항 감소)가 CurseState를 읽어 반영.
public class HeavyFireCurse : CurseInstance
{
    public HeavyFireCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        GameRun.Curse.HeavyFire = true;
        GameRun.Curse.HeavyFireExtraDecayPercent = Lv != null ? Lv.valueA : 0f;
    }

    public override void Deactivate()
    {
        GameRun.Curse.HeavyFire = false;
        GameRun.Curse.HeavyFireExtraDecayPercent = 0f;
    }
}
