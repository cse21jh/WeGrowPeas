using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Ladybug Collection Box (무당벌레 채집통)", fileName = "LadybugCollectionBoxItemData")]
public class LadybugCollectionBoxItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int maxCountIncrease = 1; // 최대 수 1 증가
    [SerializeField] private float spawnProbabilityIncrease = 0.01f; // 등장 확률 1% 증가

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = PestWave.BugAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "무당벌레 채집통";
        if (string.IsNullOrEmpty(Description)) Description = "농장에 존재할 수 있는 무당벌레의 수가 1마리 증가합니다.";
        if (Price <= 0) Price = 1000;
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 4; // 최대 구매 제한 4회
        FlowType = ShopFlowType.Instant;
        
        // 벌레 등장 스테이지에 맞춰 해금 스테이지 자동 설정
        unlockStageDay = PestWave.BugAppearStage;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        // 벌레가 실제로 나오기 시작하는 시점부터 해금 (PestWave.BugAppearStage 상수 사용)
        return stage >= unlockStageDay;
    }

    public override int GetRotationWeight(ShopContext ctx) => 1;

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

        // 최대 무당벌레 수 1 증가 (최대 5개까지)
        ctx.Grid.AddMaxLadybugCount(maxCountIncrease);
        // 무당벌레 등장 확률 1% 증가
        ctx.Grid.AddLadybugSpawnProbability(spawnProbabilityIncrease);
    }
}
