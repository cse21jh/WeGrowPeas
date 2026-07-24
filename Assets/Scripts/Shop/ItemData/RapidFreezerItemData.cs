using UnityEngine;

[CreateAssetMenu(fileName = "RapidFreezerItemData", menuName = "Shop/Items/RapidFreezer")]
public class RapidFreezerItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4; // 희귀 등급

    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "급속 냉각기";
        if (Price <= 0) Price = 2000;
        Rarity = ItemRarity.Rare; // 희귀 등급

        IsStackable = false;
        InitialStock = 1; // 일일 구매 제한 1회
        OnePerShopIfNotStackable = false; // 소모품이므로 상점에 여러 번 뜰 수 있음 (일일 제한은 별도)
        MaxPurchaseCount = -1; // 최대 구매 제한 없음
        FlowType = ShopFlowType.PlaceOnTile;
        metaRequiredEventId = UnlockManager.Ids.WinterReached;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        return rotationWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        // 구매 자체는 항상 가능 (타일 지정 시 사용)
        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidatePosition(ShopContext ctx, Vector3 pos, out string reason)
    {
        reason = null;
        if (!TryGetGridIndexFromPosition(ctx, pos, out int? idx, out reason))
            return false;

        // 해당 타일에 식물이 있는지 여부는 상관없음 (빈 땅을 중심으로도 얼릴 수 있음)
        // 유효한 그리드 인덱스인지만 확인 (TryGetGridIndexFromPosition에서 이미 확인됨)

        pendingIndex = idx.Value;
        return true;
    }

    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        
        // pendingIndex 검증 (ItemData에는 ValidatePendingIndex 메서드가 없을 수 있음. 직접 확인)
        if (pendingIndex == null) return;

        // 실제 효과 적용
        ctx.Grid.ApplyRapidFreezer(pendingIndex.Value);
        Debug.Log($"[RapidFreezer] Committed on index {pendingIndex.Value}");

        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
