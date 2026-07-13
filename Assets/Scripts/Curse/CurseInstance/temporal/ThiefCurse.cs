using UnityEngine;

public class ThiefCurse : CurseInstance
{
    public ThiefCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        int count = Mathf.RoundToInt(Lv != null ? Lv.valueA : 0f);
        GameManager.Instance?.grid?.StealPlants(count);
    }

    public override void Deactivate()
    {
        // 단발성: 해제 시 별도 처리 없음
    }
}
