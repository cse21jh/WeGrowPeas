using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/ChiliPepper (고추)", fileName = "ChiliPepperItemData")]
public class ChiliPepperItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

    private int? pendingIndex;

    private void OnEnable()
    {
        if (Price <= 0) Price = 1500;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.PlaceOnTile;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        // 고추 등장 확률 증가 적용
        int baseWeight = rotationWeight;
        if (ctx?.Grid != null)
        {
            float probabilityBonus = ctx.Grid.ChiliPepperSpawnProbability;
            // 확률을 가중치로 변환 (예: 0.02 = 2% -> 가중치 2 증가)
            int weightBonus = Mathf.RoundToInt(probabilityBonus * 100);
            return baseWeight + weightBonus;
        }
        return baseWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 객체가 없습니다 (ShopContext.Grid 필요)";
            return false;
        }
        if (!ctx.Grid.HasEmptyGrid())
        {
            reason = Loc.T("shop.error.no_empty_cell");
            return false;
        }
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
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 객체가 없습니다";
            return false;
        }

        int? idx = ctx.Grid.GetGridIndexFromPosition(pos);
        if (!idx.HasValue)
        {
            reason = Loc.T("shop.error.invalid_position");
            return false;
        }

        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = Loc.T("shop.error.occupied");
            return false;
        }

        pendingIndex = idx.Value;
        return true;
    }
    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (ctx == null || ctx.Grid == null)
        {
            ctx?.ShowError?.Invoke("Grid 객체가 없습니다");
            return;
        }
        if (!pendingIndex.HasValue)
        {
            ctx.ShowError?.Invoke(Loc.T("shop.error.invalid_selection"));
            return;
        }

        ctx.Grid.AddChiliPepper(pendingIndex.Value);

        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
