using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Bug Lure Device (벌레 유도장치)", fileName = "BugLureDeviceItemData")]
public class BugLureDeviceItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float intervalReduction = 0.1f; // 10% 감소 (음수로 전달하여 간격 감소)

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = PestWave.BugAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "벌레 유도장치";
        if (string.IsNullOrEmpty(Description)) Description = "벌레가 더 자주 등장합니다.";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Common; // 일반 등급
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 5; // 최대 구매 제한 5회
        FlowType = ShopFlowType.Instant;
        
        // 벌레 등장 스테이지에 맞춰 해금 스테이지 자동 설정
        // PestWave.BugAppearStage = 6, unlockStageDay = 6
        // stage >= 6 조건으로 stage 6(6웨이브)부터 해금 (벌레 실제 등장 시점과 일치)
        unlockStageDay = PestWave.BugAppearStage;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        // 벌레가 실제로 나오기 시작하는 시점부터 해금 (PestWave.BugAppearStage 상수 사용)
        // PestWave.BugAppearStage = 6, unlockStageDay = 6
        // stage >= 6이므로 stage 6(6웨이브)부터 해금됨
        return stage >= unlockStageDay;
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

        // 벌레 스폰 시간 간격 10% 감소 (음수로 전달하여 감소)
        // Grid의 공식: effectiveBugSpawnTimeInterval * (1f + bugSpawnIntervalIncreasement)
        // -0.1f를 더하면 0.9배가 되어 10% 감소
        ctx.Grid.AddBugSpawnIntervalIncreasement(-intervalReduction);
    }
}
