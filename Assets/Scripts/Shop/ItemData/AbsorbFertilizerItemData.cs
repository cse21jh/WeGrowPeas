using UnityEngine;

[CreateAssetMenu(fileName = "AbsorbFertilizerItemData", menuName = "Shop/Items/AbsorbFertilizer")]
public class AbsorbFertilizerItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4; // 희귀 등급

    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "저항력 흡수 비료";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Rare;

        IsStackable = false;
        InitialStock = 1; // 일일 구매 제한 1회
        OnePerShopIfNotStackable = false; // 소모품
        MaxPurchaseCount = -1; // 최대 구매 제한 없음
        FlowType = ShopFlowType.PlaceOnTile;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
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

        // 이미 비료가 있는 곳에는 못 뿌리게? -> 일단 중복 허용하되 효과는 중복 안되게 Grid에서 처리 (HashSet 사용중이라 OK)
        // 하지만 사용자 경험상 이미 뿌려진 곳은 막는 게 좋을 수 있음.
        if (ctx.Grid.HasAbsorbFertilizer(idx.Value))
        {
             reason = "이미 저항력 흡수 비료가 뿌려져 있습니다.";
             return false;
        }

        pendingIndex = idx.Value;
        return true;
    }

    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        
        if (pendingIndex == null) return;

        // 실제 효과 적용
        ctx.Grid.AddAbsorbFertilizer(pendingIndex.Value);
        Debug.Log($"[AbsorbFertilizer] Committed on index {pendingIndex.Value}");

        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
