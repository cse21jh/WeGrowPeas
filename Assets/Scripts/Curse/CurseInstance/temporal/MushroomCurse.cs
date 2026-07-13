using UnityEngine;

// 105 버섯 발생: 무작위 타일에 버섯 생성 + 해당 위치 식물 피해(페트병은 방어). CurseManager가 처리.
public class MushroomCurse : CurseInstance
{
    public MushroomCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        int count = Mathf.RoundToInt(Lv != null ? Lv.valueA : 0f);
        CurseManager.Instance?.SpawnMushroom(count);
    }

    public override void Deactivate()
    {
        CurseManager.Instance?.ClearMushroom();
    }
}
