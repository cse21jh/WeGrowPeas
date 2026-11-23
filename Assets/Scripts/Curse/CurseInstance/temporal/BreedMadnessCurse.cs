using UnityEngine;

// 106 광란: 이번 라운드 교배가 확률적으로 랜덤 교배가 됨. Grid의 교배 로직이 CurseState.BreedMadnessPercent를 읽어 반영.
public class BreedMadnessCurse : CurseInstance
{
    public BreedMadnessCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        CurseState.BreedMadnessPercent = Lv != null ? Lv.valueA : 0f;
        World.ShowEffect(Data.curseId, true);
    }

    public override void Deactivate()
    {
        CurseState.BreedMadnessPercent = 0f;
    }
}
