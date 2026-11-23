using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PET Bottle (페트병)", fileName = "PetBottleItemData")]
public class PetBottleItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

    private void OnEnable()
    {
        if (Price <= 0) Price = 500;

        // 기본 재고 3회
        IsStackable = true;

        OnePerShopIfNotStackable = false;
        FlowType = ShopFlowType.SelectExistingPlant;
        metaRequiredDawnStage = 9;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        // 페트병 등장 확률 증가 적용
        int baseWeight = rotationWeight;
        if (ctx?.Field != null)
        {
            float probabilityBonus = ctx.Modifiers.PetBottleSpawnProbability;
            // 확률을 가중치로 변환 (예: 0.02 = 2% -> 가중치 2 증가)
            int weightBonus = Mathf.RoundToInt(probabilityBonus * 100);
            return baseWeight + weightBonus;
        }
        return baseWeight;
    }

    public override void InitializePrice(ShopContext ctx)
    {
        // 동적 가격 계산 (기본 가격 - 가격 감소량)
        int basePrice = 500;
        if (ctx?.Field != null)
        {
            Price = Mathf.Max(1, basePrice - ctx.Modifiers.PetBottlePriceReduction);
        }
        else
        {
            Price = basePrice;
        }

        // 동적 재고 계산 (기본 재고 + 재고 보너스)
        int baseStock = 3;
        if (ctx?.Field != null)
        {
            InitialStock = baseStock + ctx.Modifiers.PetBottleInitialStockBonus;
        }
        else
        {
            InitialStock = baseStock;
        }
    }

    public override int GetDisplayPrice()
    {
        // InitializePrice가 호출되기 전에는 기본 가격 반환
        return Price;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (!ValidateGrid(ctx, out reason))
            return false;
        reason = null;
        return true;
    }

    public override bool ValidateTarget(ShopContext ctx, PlantState target, out string reason)
    {
        reason = null;
        if (target == null) reason = Loc.T("shop.error.no_plant_selected");
        else if (ctx.Soil.HasPetBottle(target.GridIndex)) reason = Loc.T("shop.error.bottle_exists");
        else if (!target.IsMovable) reason = Loc.T("shop.error.invalid_plant");
        return reason == null;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (!TakeSelectedCell(ctx, out int cell)) return;
        ctx.World.PlacePetBottle(cell);
        ctx.ShowInfo?.Invoke(Loc.T("shop.info.placed", ("name", LocalizedName)));
    }
}
