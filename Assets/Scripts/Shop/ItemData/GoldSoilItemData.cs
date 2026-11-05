using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Gold Soil (황금 비료)", fileName = "GoldSoilItemData")]
public class GoldSoilItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 1;

    // 배치 확정 시 사용할 그리드 인덱스
    private int? pendingIndex;

    private void OnEnable()
    {
        if (Price <= 0) Price = 5000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.PlaceOnTile;
        metaRequiredEventId = UnlockManager.Ids.GoldenPlantCreated;
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
        // 화면 좌표를 그리드 인덱스로 변환
        if (!TryGetGridIndexFromPosition(ctx, pos, out int? idx, out reason))
            return false;

        // 이미 황금 비료가 있는 칸인지 확인
        if (ctx.Grid.HasGoldSoil(idx.Value))
        {
            reason = "이미 황금 비료가 있는 칸입니다";
            return false;
        }

        // 식물이 있는 칸인지 확인
        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "식물이 있는 칸에는 황금 비료를 뿌릴 수 없습니다";
            return false;
        }

        pendingIndex = idx.Value;
        return true;
    }

    public override void SetPlacedPosition(Vector3 worldPos)
    {
        // ValidatePosition에서 이미 처리됨
    }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        if (!ValidatePendingIndex(pendingIndex, ctx, out _))
            return;

        if (!ctx.Grid.TryPlaceGoldSoil(pendingIndex.Value))
        {
            ctx.ShowError?.Invoke("황금 비료를 뿌릴 수 없습니다");
            return;
        }

        ctx.ShowInfo?.Invoke($"{DisplayName} 배치 완료");
        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
