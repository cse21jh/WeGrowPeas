using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Nepenthes Pheromone Range Upgrade (네펜데스 페로몬 범위 증가)", fileName = "NepenthesPheromoneRangeUpgradeItemData")]
public class NepenthesPheromoneRangeUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float rangeMultiplierIncrease = 0.2f; // 페로몬 범위 +20% (합적용)
    [SerializeField] private float spawnProbabilityIncrease = 0.02f; // 등장 확률 2% 증가

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = PestWave.BugAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "네펜데스 페로몬 범위 증가";
        if (string.IsNullOrEmpty(Description)) Description = "네펜데스의 페로몬 범위가 증가합니다.";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Rare; // 희귀 등급
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 3; // 최대 구매 제한 3회
        FlowType = ShopFlowType.Instant;
        
        // 벌레 등장 스테이지에 맞춰 해금 스테이지 자동 설정
        unlockStageDay = PestWave.BugAppearStage;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        // 벌레가 실제로 나오기 시작하는 시점부터 해금 (PestWave.BugAppearStage 상수 사용)
        // 페로몬 생성이 활성화되어 있어야 함 (선행 조건)
        if (ctx?.Grid == null) return false;
        return stage >= unlockStageDay && ctx.Grid.HasNepenthesPheromone;
    }

    public override int GetRotationWeight(ShopContext ctx) => 4;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        // 페로몬 생성이 활성화되어 있어야 함 (선행 조건)
        if (ctx?.Grid == null || !ctx.Grid.HasNepenthesPheromone)
        {
            reason = "먼저 네펜데스 페로몬 생성을 구매해야 합니다.";
            return false;
        }

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

        // 네펜데스 페로몬 범위 20% 증가 (중첩 시 합적용)
        ctx.Grid.AddAdditionalNepenthesPheromoneSizeMultiplier(rangeMultiplierIncrease);
        // 네펜데스 등장 확률 2% 증가
        ctx.Grid.AddNepenthesSpawnProbability(spawnProbabilityIncrease);
    }
}
