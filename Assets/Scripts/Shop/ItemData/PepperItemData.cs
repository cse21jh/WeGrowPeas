using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/ChiliPepper (고추)", fileName = "ChiliPepperItemData")]
public class ChiliPepperItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "고추";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Common;

        IsStackable = false;
        InitialStock = 1;
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
            reason = "배치할 수 있는 빈 칸이 없습니다";
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
            reason = "유효한 위치가 아닙니다";
            return false;
        }

        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "이미 식물이 있는 칸입니다";
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
            ctx.ShowError?.Invoke("위치 선택이 유효하지 않습니다");
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
