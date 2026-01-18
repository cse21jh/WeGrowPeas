using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Bug Processing Upgrade (벌레 가공 기술 향상)", fileName = "BugProcessingUpgradeItemData")]
public class BugProcessingUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int goldBonus = 50;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = PestWave.BugAppearStage; // 벌레가 실제로 등장하는 스테이지

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "벌레 가공 기술 향상";
        if (string.IsNullOrEmpty(Description)) Description = "벌레의 기본 가격이 50골드 증가합니다.";
        if (Price <= 0) Price = 1000;
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 4; // 최대 구매 제한 4회
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

        // 벌레 처치 시 골드 획득량 50골드 증가
        ctx.Grid.AddAdditionalBugGold(goldBonus);
    }
}
