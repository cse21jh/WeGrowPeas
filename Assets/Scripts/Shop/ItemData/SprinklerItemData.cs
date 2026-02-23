using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Sprinkler (스프링클러)", fileName = "SprinklerItemData")]
public class SprinklerItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4; // 희귀 등급

    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "스프링클러";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Rare; // 희귀 등급

        IsStackable = false;
        InitialStock = 1; // 일일 구매 제한 1회
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음 (여러 번 구매 가능)
        FlowType = ShopFlowType.PlaceOnTile;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        return rotationWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        return CheckHasEmptyGrid(ctx, out reason);
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

        // 빈 칸인지 확인
        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "이미 식물이 있는 칸입니다";
            return false;
        }

        // 문제 없으면 확정 후보 저장
        pendingIndex = idx.Value;
        return true;
    }

    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        if (!ValidatePendingIndex(pendingIndex, ctx, out _))
            return;

        // 실제 배치
        ctx.Grid.AddSprinkler(pendingIndex.Value);

        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
