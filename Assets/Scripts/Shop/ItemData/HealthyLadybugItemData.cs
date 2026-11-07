using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Healthy Ladybug (건강한 무당벌레)", fileName = "HealthyLadybugItemData")]
public class HealthyLadybugItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float resistancePerUnit = 0.005f; // 무당벌레당 저항력 0.5% 증가
    [SerializeField] private float spawnProbabilityIncrease = 0.04f; // 등장 확률 4% 증가

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = BugSchedule.DefaultAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (Price <= 0) Price = 3000;
        
        IsStackable = false;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
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

    public override int GetRotationWeight(ShopContext ctx) => 2;

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

        // 무당벌레당 모든 기본저항력 0.5% 증가 (중첩 시 합적용)
        ctx.Grid.Modifiers.AddAdditionalLadybugResistancePerUnit(resistancePerUnit);
        // 무당벌레 등장 확률 4% 증가
        ctx.Grid.Modifiers.AddLadybugSpawnProbability(spawnProbabilityIncrease);
    }
}
