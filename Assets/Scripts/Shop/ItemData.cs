using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.Rendering;

public enum ShopFlowType { Instant, PlaceOnTile, SelectExistingPlant }

public abstract class ItemData : ScriptableObject
{
    [Header("Meta")]
    public string DisplayName;
    public Sprite Icon;
    [TextArea] public string Description;
    public int Price;

    [Header("Rule")]
    public bool IsStackable = false;
    public int InitialStock = 1;
    public bool OnePerShopIfNotStackable = true;
    [Tooltip("-1 = 무제한, 0 이상 = 게임 전체에서 최대 구매 횟수")]
    public int MaxPurchaseCount = -1;

    [Header("Flow")]
    public ShopFlowType FlowType;

    // 로테이션 후보 필터(웨이브 해금 등), 기본 true
    public virtual bool IsRotationUnlockOk(ShopContext ctx) => true;

    public virtual int GetRotationWeight(ShopContext ctx) => 1;

    // 구매 가능 여부(이미 활성화 중인지, 잠금 해금 시기, 중복 금지 등)
    public abstract bool CanPurchase(ShopContext ctx, out string reason);

    // 구매 시작(미리보기/선택 모드 진입 등)
    public abstract void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError);

    // 확정(Commit) 시 실제 적용(골드 차감은 ShopUI가 여기 직전에)
    public abstract void Commit(ShopContext ctx);

    // 취소(선택 취소/배치 취소 등)
    public virtual void Cancel(ShopContext ctx) { }

    // 배치/선택형의 유효성 검사(즉시형은 필요 없음)
    public virtual bool ValidatePosition(ShopContext ctx, Vector3 worldPos, out string reason) { reason = null; return true; }
    public virtual bool ValidateTarget(ShopContext ctx, Plant target, out string reason) { reason = null; return true; }

    // 최종 확정에 필요한 외부 입력 보관
    public virtual void SetPlacedPosition(Vector3 worldPos) { }
    public virtual void SetSelectedPlant(Plant plant) { }
    public virtual void InitializePrice(ShopContext ctx) { }

    public int GetDisplayPrice()
    {
        return Price;
    }

    // 게임 전체에서의 구매 횟수 조회
    public int GetTotalPurchaseCount()
    {
        return ShopManager.Instance?.GetItemPurchaseCount(this) ?? 0;
    }

    // 게임 전체에서의 구매 가능 여부 (MaxPurchaseCount 체크)
    public bool CanPurchaseByLimit()
    {
        if (MaxPurchaseCount < 0) return true; // -1 = 무제한
        return GetTotalPurchaseCount() < MaxPurchaseCount;
    }
}

// 주입될 런타임 컨텍스트
public class ShopContext
{
    //public PlayerManager Player;
    public Grid Grid;
    //public WaveManager Wave;
    //public BugManager Bugs;
    public EconomyManager Economy;
    public ShopManager Shop;
    public object Session; // 필요하면 인터페이스로 확장

    public System.Action<string> ShowInfo;
    public System.Action<string> ShowError;
    public System.Action<string> ShowGuide;
}