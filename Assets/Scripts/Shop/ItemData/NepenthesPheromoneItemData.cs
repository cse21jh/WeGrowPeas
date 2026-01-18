using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Nepenthes Pheromone (네펜데스 페로몬 생성)", fileName = "NepenthesPheromoneItemData")]
public class NepenthesPheromoneItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float spawnProbabilityIncrease = 0.05f; // 등장 확률 5% 증가

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = PestWave.BugAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "네펜데스 페로몬 생성";
        if (string.IsNullOrEmpty(Description)) Description = "네펜데스에게 페로몬 효과를 추가합니다.";
        if (Price <= 0) Price = 3000;
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 1; // 최대 구매 제한 1회
        FlowType = ShopFlowType.Instant;
        
        // 벌레 등장 스테이지에 맞춰 해금 스테이지 자동 설정
        unlockStageDay = PestWave.BugAppearStage;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        // 벌레가 실제로 나오기 시작하는 시점부터 해금 (PestWave.BugAppearStage 상수 사용)
        // 이미 페로몬이 활성화되어 있으면 표시하지 않음
        if (ctx?.Grid != null && ctx.Grid.HasNepenthesPheromone)
        {
            return false;
        }
        return stage >= unlockStageDay;
    }

    public override int GetRotationWeight(ShopContext ctx) => 1;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        // 이미 페로몬이 활성화되어 있으면 구매 불가
        if (ctx?.Grid != null && ctx.Grid.HasNepenthesPheromone)
        {
            reason = "이미 페로몬이 활성화되어 있습니다.";
            return false;
        }

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

        // 네펜데스 페로몬 활성화
        ctx.Grid.SetNepenthesPheromoneEnabled(true);
        // 네펜데스 등장 확률 5% 증가
        ctx.Grid.AddNepenthesSpawnProbability(spawnProbabilityIncrease);
    }
}
