using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PET Bottle (페트병)", fileName = "PetBottleItemData")]
public class PetBottleItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    // 배치 확정 시 사용할 그리드 인덱스
    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "페트병";
        if (Price <= 0) Price = 500;

        // 기본 재고 3회
        IsStackable = true;
        InitialStock = 3;

        OnePerShopIfNotStackable = false;
        FlowType = ShopFlowType.SelectExistingPlant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        // 페트병 등장 확률 증가 적용
        int baseWeight = rotationWeight;
        if (ctx?.Grid != null)
        {
            float probabilityBonus = ctx.Grid.PetBottleSpawnProbability;
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
        if (ctx?.Grid != null)
        {
            Price = Mathf.Max(1, basePrice - ctx.Grid.PetBottlePriceReduction);
        }
        else
        {
            Price = basePrice;
        }

        // 동적 재고 계산 (기본 재고 + 재고 보너스)
        int baseStock = 3;
        if (ctx?.Grid != null)
        {
            InitialStock = baseStock + ctx.Grid.PetBottleInitialStockBonus;
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
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 객체가 없습니다 (ShopContext.Grid 주입 필요)";
            return false;
        }
        reason = null;
        return true;
    }

    public override bool ValidateTarget(ShopContext ctx, Plant target, out string reason)
    {
        reason = null;

        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 객체가 없습니다";
            return false;
        }
        if (target == null)
        {
            reason = "선택된 식물이 없습니다";
            return false;
        }
        int idx = target.gridIndex;

        // 이미 페트병이 위치한 칸 불가
        if (ctx.Grid.HasPetBottle(idx))
        {
            reason = "이미 페트병이 위치한 칸입니다";
            return false;
        }

        if(!target.IsMovable)
        {
            reason = "유효한 식물이 아닙니다";
            return false;
        }

        pendingIndex = idx;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
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

        ctx.Grid.PlacePetBottle(pendingIndex.Value);
        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
