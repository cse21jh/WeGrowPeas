using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Ladybug Lure Device (무당벌레 유도장치)", fileName = "LadybugLureDeviceItemData")]
public class LadybugLureDeviceItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float spawnProbabilityIncrease = 0.03f; // 3% 증가

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = PestWave.BugAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "무당벌레 유도장치";
        if (string.IsNullOrEmpty(Description)) Description = "식물을 해치지 않는 무당벌레가 나올 확률이 증가합니다.";
        if (Price <= 0) Price = 1000;
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 5; // 최대 구매 제한 5회
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
        // 최대 구매 제한 확인
        if (!CanPurchaseByLimit())
        {
            reason = "최대 구매 횟수를 초과했습니다.";
            return false;
        }

        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (ctx?.Grid == null)
        {
            ctx?.ShowError?.Invoke("Grid 객체가 없습니다");
            return;
        }

        // 무당벌레 등장 확률 3% 증가 (중첩 시 합적용)
        ctx.Grid.AddLadybugSpawnProbability(spawnProbabilityIncrease);
    }
}
