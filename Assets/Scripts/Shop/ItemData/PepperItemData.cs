using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/ChiliPepper (고추)", fileName = "ChiliPepperItemData")]
public class ChiliPepperItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

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
        if (ctx?.Field != null)
        {
            float probabilityBonus = ctx.Modifiers.ChiliPepperSpawnProbability;
            // 확률을 가중치로 변환 (예: 0.02 = 2% -> 가중치 2 증가)
            int weightBonus = Mathf.RoundToInt(probabilityBonus * 100);
            return baseWeight + weightBonus;
        }
        return baseWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx == null || ctx.Field == null)
        {
            reason = Loc.T("shop.error.no_game_state");
            return false;
        }
        if (!ctx.Board.HasEmptyCell())
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

    public override bool ValidateCell(ShopContext ctx, int cell, out string reason)
    {
        bool occupied = ctx.Board.States.ContainsKey(cell);
        reason = occupied ? Loc.T("shop.error.occupied") : null;
        return !occupied;
    }

    public override void Commit(ShopContext ctx)
    {
        if (TakeSelectedCell(ctx, out int cell))
            ctx.World.PlantFixed(PlantKind.ChiliPepper, cell);
    }
}
