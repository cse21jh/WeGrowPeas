using UnityEngine;

// 102 안개: 무작위 타일에 안개 생성(해당 땅 식물 저항력 확인 불가). CurseManager가 프리팹 스폰.
public class FogCurse : CurseInstance
{
    public FogCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        int count = Mathf.RoundToInt(Lv != null ? Lv.valueA : 0f);
        CurseManager.Instance?.SpawnFog(count);
        CurseEffectManager.Instance?.SetFogCurse(true);
    }

    public override void Deactivate()
    {
        CurseManager.Instance?.ClearFog();
        CurseEffectManager.Instance?.SetFogCurse(false);
    }
}
