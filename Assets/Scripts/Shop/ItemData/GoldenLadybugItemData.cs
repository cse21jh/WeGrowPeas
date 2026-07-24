using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Golden Ladybug (황금 무당벌레)", fileName = "GoldenLadybugItemData")]
public class GoldenLadybugItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int goldPerUnit = 100; // 무당벌레당 100골드
    [SerializeField] private float spawnProbabilityIncrease = 0.01f; // 등장 확률 1% 증가

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = BugSchedule.DefaultAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "황금 무당벌레";
        if (string.IsNullOrEmpty(Description)) Description = "웨이브가 끝날 때 무당벌레의 수만큼 100골드를 추가로 획득합니다.";
        if (Price <= 0) Price = 2000;
        Rarity = ItemRarity.Rare; // 희귀 등급
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 4; // 최대 구매 제한 4회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 1;
        
        // 벌레 등장 스테이지에 맞춰 해금 스테이지 자동 설정
        unlockStageDay = BugSchedule.DefaultAppearStage;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        // 벌레가 실제로 나오기 시작하는 시점부터 해금 (새벽 1단계 클리어 조건은 metaRequiredDawnStage)
        return stage >= BugSchedule.AppearStage;
    }

    public override int GetRotationWeight(ShopContext ctx) => 4;

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

        // 웨이브 종료 시 무당벌레당 100골드 획득 (중첩 시 합적용)
        ctx.Grid.AddAdditionalLadybugGoldPerUnit(goldPerUnit);
        // 무당벌레 등장 확률 1% 증가
        ctx.Grid.AddLadybugSpawnProbability(spawnProbabilityIncrease);
    }
}
