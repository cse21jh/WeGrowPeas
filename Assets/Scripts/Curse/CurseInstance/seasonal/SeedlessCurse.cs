using UnityEngine;

// 207 씨 없는 수박: 교배 시 실패할 확률. Grid의 교배 로직이 CurseState.SeedlessFailPercent를 읽어 반영.
public class SeedlessCurse : CurseInstance
{
    public SeedlessCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        CurseState.SeedlessFailPercent = Lv != null ? Lv.valueA : 0f;
        World.ShowEffect(Data.curseId, true);
    }

    public override void Deactivate()
    {
        CurseState.SeedlessFailPercent = 0f;
        World.ShowEffect(Data.curseId, false);
    }
}
