using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Nepenthes Digestion Upgrade (네펜데스 소화액 개량)", fileName = "NepenthesDigestionUpgradeItemData")]
public class NepenthesDigestionUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int goldBonus = 80; // 벌레 먹을 시 추가 골드
    [SerializeField] private float spawnProbabilityIncrease = 0.01f; // 등장 확률 1% 증가

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = BugSchedule.DefaultAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "네펜데스 소화액 개량";
        if (string.IsNullOrEmpty(Description)) Description = "네펜데스가 벌레를 먹을 때 80골드를 더 획득합니다.";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Common; // 일반 등급
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 5; // 최대 구매 제한 5회
        FlowType = ShopFlowType.Instant;
        
        // 벌레 등장 스테이지에 맞춰 해금 스테이지 자동 설정
        unlockStageDay = BugSchedule.DefaultAppearStage;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        // 벌레가 실제로 나오기 시작하는 시점부터 해금 (BugSchedule.DefaultAppearStage 상수 사용)
        return stage >= BugSchedule.AppearStage;
    }

    public override int GetRotationWeight(ShopContext ctx) => 8;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        return CheckMaxPurchaseLimit(out reason);
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;

        // 네펜데스가 벌레 먹을 시 추가 골드 80 증가 (중첩 시 합적용)
        ctx.Grid.AddAdditionalNepenthesGold(goldBonus);
        // 네펜데스 등장 확률 1% 증가
        ctx.Grid.AddNepenthesSpawnProbability(spawnProbabilityIncrease);
    }
}
