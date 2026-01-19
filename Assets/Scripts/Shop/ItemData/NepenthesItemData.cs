using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Nepenthes", fileName = "NepenthesItemData")]
public class NepenthesItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

    // 배치 확정 시 사용할 그리드 인덱스
    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "네펜데스";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Common; // 일반 등급

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음 (X)
        FlowType = ShopFlowType.PlaceOnTile;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        // 네펜데스 등장 확률 증가 적용
        int baseWeight = rotationWeight;
        if (ctx?.Grid != null)
        {
            float probabilityBonus = ctx.Grid.NepenthesSpawnProbability;
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

    public override bool ValidatePosition(ShopContext ctx, Vector3 pos, out string reason)
    {
        // 스크린 좌표 → 그리드 인덱스
        if (!TryGetGridIndexFromPosition(ctx, pos, out int? idx, out reason))
            return false;

        // 빈 칸인지 확인
        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "이미 식물이 있는 칸입니다";
            return false;
        }

        // 문제 없으면 확정 후보 저장
        pendingIndex = idx.Value;
        return true;
    }

    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        if (!ValidatePendingIndex(pendingIndex, ctx, out _))
            return;

        // 실제 배치
        ctx.Grid.AddNepenthes(pendingIndex.Value);

        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
