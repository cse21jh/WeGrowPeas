// Assets/Scripts/Shop/Items/ItemData_SignPost.cs
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Item_SignPost", menuName = "Shop/Item/SignPost")]
public class ItemData_SignPost : ItemData
{
    [Header("SignPost")]
    public WaveType targetWave = WaveType.Wind;     // 이 팻말이 억제할 웨이브
    [Range(0f, 1f)] public float reducePercent = 0.75f; // 75% 감소 → x0.25
    [Min(1)] public int durationDays = 4;           // 다음 4일간

    [Header("Shop Appear Rules")]
    [Min(1)] public int unlockStageDay = 5;         // (stage+1) >= unlockStageDay 부터 등장
    [Min(0)] public int rotationWeight = 2;         // 로테이션 등장 가중치

    // 로테이션 후보 필터 (해금 시점 체크)
    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;      // 컨텍스트에 stage가 없으니 전역 사용
        return (stage + 1) >= unlockStageDay;
    }

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        // 추가 제약이 있으면 여기서 체크(예: 이미 같은 웨이브에 팻말이 과도하게 걸렸는지 등)
        return true;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        // 즉시형이라 준비 과정 없음
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        ModManager.Instance.AddTimedMultiplier(
            StatId.WaveWeightMul,
            (int)targetWave,                 // 어떤 웨이브를 눌러줄지
            1f - reducePercent,             // 75% 감소 -> multiplier 0.25f
            durationDays,                   // 기본 4일 등
            $"SignPost_{targetWave}"        // 추적용 태그
        );
        ctx.ShowInfo?.Invoke($"{DisplayName} 사용 → {targetWave} {durationDays}일간 {(int)(reducePercent * 100)}% 감소");
    }

    public override void Cancel(ShopContext ctx) { /* 즉시형: 취소 없음 */ }

    // 즉시형이므로 배치/선택형 API는 사용하지 않지만, 안전하게 거부만 해둠
    public override bool ValidatePosition(ShopContext ctx, Vector3 worldPos, out string reason) { reason = null; return false; }
    public override void SetPlacedPosition(Vector3 worldPos) { }
    public override bool ValidateTarget(ShopContext ctx, Plant target, out string reason) { reason = null; return false; }
    public override void SetSelectedPlant(Plant plant) { }

    // 인스펙터에서 실수 방지: 즉시형/비스택 강제
    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        if (string.IsNullOrEmpty(DisplayName))
            DisplayName = $"팻말: {targetWave}";
        if (Price <= 0)
            Price = 1000;
    }
}
