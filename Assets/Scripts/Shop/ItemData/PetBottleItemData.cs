using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PET Bottle (페트병)", fileName = "PetBottleItemData")]
public class PetBottleItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

    // 배치 확정 시 사용할 그리드 인덱스
    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "페트병";
        if (Price <= 0) Price = 500;
        Rarity = ItemRarity.Common; // 일반 등급

        // 기본 재고 3회
        IsStackable = true;
        InitialStock = 3;

        OnePerShopIfNotStackable = false;
        FlowType = ShopFlowType.SelectExistingPlant;
        metaRequiredDawnStage = 9;
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
        if (!ValidateGrid(ctx, out reason))
            return false;
        reason = null;
        return true;
    }

    public override bool ValidateTarget(ShopContext ctx, Plant target, out string reason)
    {
        reason = null;

        if (!ValidateGrid(ctx, out reason))
            return false;
        
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

        if (!target.IsMovable)
        {
            reason = "유효한 식물이 아닙니다";
            return false;
        }

        return true;
    }

    public override void SetSelectedPlant(Plant plant)
    {
        if (plant != null)
        {
            pendingIndex = plant.gridIndex;
        }
        else
        {
            pendingIndex = null;
        }
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        if (!ValidatePendingIndex(pendingIndex, ctx, out _))
            return;

        try
        {
            int idx = pendingIndex.Value;
            
            // 유효한 인덱스 범위 체크
            int maxIdx = ctx.Grid.maxCol * 4;
            if (idx < 0 || idx >= maxIdx)
            {
                Debug.LogError($"[PetBottle] Commit: Invalid idx={idx}, maxIdx={maxIdx}");
                ctx.ShowError?.Invoke($"유효하지 않은 위치입니다 (idx={idx})");
                pendingIndex = null;
                return;
            }

            ctx.Grid.PlacePetBottle(idx);
            ctx.ShowInfo?.Invoke($"{DisplayName} 배치 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PetBottle] Commit error: {e.Message}\n{e.StackTrace}");
            ctx.ShowError?.Invoke($"페트병 배치 중 오류가 발생했습니다: {e.Message}");
        }
        finally
        {
            pendingIndex = null;
        }
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
