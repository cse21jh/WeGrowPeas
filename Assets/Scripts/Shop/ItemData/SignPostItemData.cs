// Assets/Scripts/Shop/Items/ItemData_SignPost.cs
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Shop/Items/Sign Post (팻말)", fileName = "SignPostItemData")]
public class SignPostItemData : ItemData
{
    [Header("SignPost")]
    [Range(0f, 1f)] public float reducePercent = 0.75f; // 75% 감소 -> x0.25
    [Min(1)] public int durationDays = 5;           // 다음 5일간

    [Header("Shop Appear Rules")]
    [Min(1)] public int unlockStageDay = 5;         // 웨이브별 해금 시기
    [Min(0)] public int rotationWeight = 8;         // 로테이션 풀 가중치

    private WaveType? pendingWave = null;

    private void OnEnable()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음
        Rarity = ItemRarity.Common; // 일반 등급

        if (string.IsNullOrEmpty(DisplayName))
            DisplayName = "팻말";
        if (string.IsNullOrEmpty(Description))
            Description = "다음 5일간 선택한 웨이브가 나타날 확률을 크게 감소시킵니다.";
        if (Price <= 0) Price = 500;
    }

    // 로테이션 풀 해금 조건 (해금 시기 체크)
    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;      // 컨텍스트의 stage를 사용해야 할 수도 있음
        return stage >= unlockStageDay;
    }

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        // 추가 검증이 필요하면 여기서 체크(예: 이미 해당 웨이브에 팻말이 설정되어 있는지 등)
        return true;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        // 형질 선택 UI를 재사용하여 웨이브 선택 UI 표시
        if (TraitSelectionUIController.Instance == null)
        {
            onError?.Invoke("웨이브 선택 UI를 찾을 수 없습니다");
            return;
        }

        TraitSelectionUIController.Instance.ShowWaveSelection(
            onConfirm: (selectedWave) => {
                pendingWave = selectedWave;
                onReady?.Invoke(); // 웨이브 선택 후 즉시 적용
            },
            onCancel: () => {
                pendingWave = null;
                onError?.Invoke("구매 취소");
            },
            title: "팻말: 웨이브를 선택하세요"
        );
    }

    public override void Commit(ShopContext ctx)
    {
        if (!pendingWave.HasValue)
        {
            Debug.LogError("[SignPost] Selected wave is null.");
            return;
        }

        ModManager.Instance.AddTimedMultiplier(
            StatId.WaveWeightMul,
            (int)pendingWave.Value,                 // 어떤 웨이브를 감소시킬지
            1f - reducePercent,             // 75% 감소 -> multiplier 0.25f
            durationDays,                   // 다음 5일간
            $"SignPost_{pendingWave.Value}"        // 모드 소스 태그
        );
        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: {pendingWave.Value} {durationDays}일간 {(int)(reducePercent * 100)}% 감소");
        pendingWave = null;
    }

    public override void Cancel(ShopContext ctx) 
    { 
        pendingWave = null;
    }

    // Instant 타입이므로 위치/대상 API는 사용하지 않음
    public override bool ValidatePosition(ShopContext ctx, Vector3 worldPos, out string reason) { reason = null; return false; }
    public override void SetPlacedPosition(Vector3 worldPos) { }
    public override bool ValidateTarget(ShopContext ctx, Plant target, out string reason) { reason = null; return false; }
    public override void SetSelectedPlant(Plant plant) { }
}
