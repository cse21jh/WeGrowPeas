using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PET Bottle (페트병)", fileName = "PetBottleItemData")]
public class PetBottleItemData : ItemData
{
    // 배치 확정 시 사용할 그리드 인덱스
    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "페트병";
        if (Price <= 0) Price = 500;

        // 상점당 3회
        IsStackable = true;
        InitialStock = 3;

        OnePerShopIfNotStackable = false;
        FlowType = ShopFlowType.PlaceOnTile;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 참조가 없습니다 (ShopContext.Grid 주입 필요)";
            return false;
        }
        reason = null;
        return true;
    }

    // pos = 스크린 좌표 (Grid.GetGridIndexFromPosition 시그니처 기준)
    public override bool ValidatePosition(ShopContext ctx, Vector3 pos, out string reason)
    {
        reason = null;
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 참조가 없습니다";
            return false;
        }

        int? idx = ctx.Grid.GetGridIndexFromPosition(pos);
        if (!idx.HasValue)
        {
            reason = "유효한 토양이 아닙니다";
            return false;
        }

        // 식물 유무와 상관없이 설치 가능. 단, 이미 페트병이 있으면 불가
        if (ctx.Grid.HasPetBottle(idx.Value))
        {
            reason = "이미 페트병이 있습니다";
            return false;
        }

        pendingIndex = idx.Value;
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
            ctx?.ShowError?.Invoke("Grid 참조가 없습니다");
            return;
        }
        if (!pendingIndex.HasValue)
        {
            ctx.ShowError?.Invoke("배치 위치가 유효하지 않습니다");
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
