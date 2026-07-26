using UnityEngine;

// 204 꽃가루 실종: 매 턴 필드 식물 중 일정 비율이 교배 불가로 바뀜. Grid가 CurseState.PollenLostRatio를 읽어 반영.
public class PollenLostCurse : CurseInstance
{
    public PollenLostCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.PollenLostRatio = (Lv != null ? Lv.valueA : 0f) / 100f;
        // 매 턴(매일 Activate) 새로 굴려 필드 식물 일부를 교배 불가로.
        GameManager.Instance?.grid?.ApplyPollenLost(CurseState.PollenLostRatio);
        CurseEffectManager.Instance?.SetPollenLost(true); // 전용 파티클 + 교배 불가 식물 색 변화
    }

    public override void Deactivate()
    {
        CurseState.PollenLostRatio = 0f;
        GameManager.Instance?.grid?.ApplyPollenLost(0f); // 전부 복구
        CurseEffectManager.Instance?.SetPollenLost(false);
    }
}
