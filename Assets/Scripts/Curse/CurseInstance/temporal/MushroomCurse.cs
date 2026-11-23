using UnityEngine;

// 105 버섯 발생: 무작위 타일에 버섯 생성 + 해당 위치 식물 피해(페트병은 방어). CurseManager가 처리.
public class MushroomCurse : CurseInstance
{
    public MushroomCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        int count = Mathf.RoundToInt(Lv != null ? Lv.valueA : 0f);
        Board.SpawnMushrooms(count);
        World.ShowEffect(Data.curseId, true);
    }

    public override void Deactivate()
    {
        Board.ClearMushrooms();
        World.ShowEffect(Data.curseId, false);
    }
}
