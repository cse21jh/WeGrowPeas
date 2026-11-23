using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Nepenthes", fileName = "NepenthesItemData")]
public class NepenthesItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

    // 배치 확정 시 사용할 그리드 인덱스

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
        // 네펜데스 등장 확률 증가 적용
        int baseWeight = rotationWeight;
        if (ctx?.Field != null)
        {
            float probabilityBonus = ctx.Modifiers.NepenthesSpawnProbability;
            // 확률을 가중치로 변환 (예: 0.01 = 1% -> 가중치 1 증가)
            int weightBonus = Mathf.RoundToInt(probabilityBonus * 100);
            return baseWeight + weightBonus;
        }
        return baseWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        return CheckHasEmptyGrid(ctx, out reason);
    }

    // 배치 모드 진입: 별도 준비 없음
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
            ctx.World.PlantFixed(PlantKind.Nepenthes, cell);
    }
}
