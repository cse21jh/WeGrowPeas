using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.Rendering;

public enum ShopFlowType { Instant, PlaceOnTile, SelectExistingPlant }

public enum ItemRarity
{
    Common = 8,      // 일반 등급 (가중치 8)
    Rare = 4,        // 희귀 등급 (가중치 4)
    Special = 2,     // 특수 등급 (가중치 2)
    Legendary = 1    // 전설 등급 (가중치 1)
}

public abstract class ItemData : ScriptableObject
{
    [Header("Meta")]
    public string DisplayName;
    public Sprite Icon;
    [TextArea] public string Description;
    public int Price;
    
    [Header("Rarity")]
    public ItemRarity Rarity = ItemRarity.Common;
    
    [Header("Grade Tag")]
    [Tooltip("등급 태그 텍스트 (예: S, A, B, C)")]
    public string GradeTagText;
    [Tooltip("등급 태그 이미지")]
    public Sprite GradeTagImage;

    [Header("Rule")]
    public bool IsStackable = false;
    public int InitialStock = 1;
    public bool OnePerShopIfNotStackable = true;
    [Tooltip("-1 = ������, 0 �̻� = ���� ��ü���� �ִ� ���� Ƚ��")]
    public int MaxPurchaseCount = -1;

    [Header("Flow")]
    public ShopFlowType FlowType;

    // �����̼� �ĺ� ����(���̺� �ر� ��), �⺻ true
    public virtual bool IsRotationUnlockOk(ShopContext ctx) => true;

    public virtual int GetRotationWeight(ShopContext ctx) => 1;

    // ���� ���� ����(�̹� Ȱ��ȭ ������, ��� �ر� �ñ�, �ߺ� ���� ��)
    public abstract bool CanPurchase(ShopContext ctx, out string reason);

    // ���� ����(�̸�����/���� ��� ���� ��)
    public abstract void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError);

    // Ȯ��(Commit) �� ���� ����(��� ������ ShopUI�� ���� ������)
    public abstract void Commit(ShopContext ctx);

    // ���(���� ���/��ġ ��� ��)
    public virtual void Cancel(ShopContext ctx) { }

    // ��ġ/�������� ��ȿ�� �˻�(������� �ʿ� ����)
    public virtual bool ValidatePosition(ShopContext ctx, Vector3 worldPos, out string reason) { reason = null; return true; }
    public virtual bool ValidateTarget(ShopContext ctx, Plant target, out string reason) { reason = null; return true; }

    // ���� Ȯ���� �ʿ��� �ܺ� �Է� ����
    public virtual void SetPlacedPosition(Vector3 worldPos) { }
    public virtual void SetSelectedPlant(Plant plant) { }
    public virtual void InitializePrice(ShopContext ctx) { }

    public virtual int GetDisplayPrice()
    {
        return Price;
    }

    // ���� ��ü������ ���� Ƚ�� ��ȸ
    public int GetTotalPurchaseCount()
    {
        return ShopManager.Instance?.GetItemPurchaseCount(this) ?? 0;
    }

    // ���� ��ü������ ���� ���� ���� (MaxPurchaseCount üũ)
    public bool CanPurchaseByLimit()
    {
        if (MaxPurchaseCount < 0) return true; // -1 = ������
        return GetTotalPurchaseCount() < MaxPurchaseCount;
    }

    // === 공통 헬퍼 메서드 ===

    /// <summary>
    /// Grid null 체크 및 에러 처리 헬퍼
    /// </summary>
    protected bool ValidateGrid(ShopContext ctx, out string errorReason)
    {
        errorReason = null;
        if (ctx?.Grid == null)
        {
            errorReason = "Grid 객체가 없습니다";
            ctx?.ShowError?.Invoke(errorReason);
            return false;
        }
        return true;
    }

    /// <summary>
    /// MaxPurchaseCount 기반 구매 가능 여부 체크 (공통 패턴)
    /// </summary>
    protected bool CheckMaxPurchaseLimit(out string reason)
    {
        if (!CanPurchaseByLimit())
        {
            reason = "최대 구매 횟수를 초과했습니다.";
            return false;
        }
        reason = null;
        return true;
    }

    /// <summary>
    /// 빈 그리드가 있는지 확인하는 헬퍼
    /// </summary>
    protected bool CheckHasEmptyGrid(ShopContext ctx, out string reason)
    {
        reason = null;
        if (!ValidateGrid(ctx, out reason))
            return false;

        if (!ctx.Grid.HasEmptyGrid())
        {
            reason = "배치할 수 있는 공간이 없습니다";
            return false;
        }
        return true;
    }

    /// <summary>
    /// purchaseKey 기반 구매 횟수 가져오기
    /// </summary>
    protected int GetPurchaseCountByKey(ShopContext ctx, string purchaseKey)
    {
        if (ctx?.Shop?.PurchaseHistory == null)
            return 0;

        if (!ctx.Shop.PurchaseHistory.TryGetValue(purchaseKey, out int count))
            return 0;

        return count;
    }

    // === PlaceOnTile 플로우 헬퍼 메서드 ===

    /// <summary>
    /// ValidatePosition에서 위치를 그리드 인덱스로 변환하는 헬퍼
    /// </summary>
    protected bool TryGetGridIndexFromPosition(ShopContext ctx, Vector3 pos, out int? gridIndex, out string errorReason)
    {
        gridIndex = null;
        errorReason = null;

        if (!ValidateGrid(ctx, out errorReason))
            return false;

        int? idx = ctx.Grid.GetGridIndexFromPosition(pos);
        if (!idx.HasValue)
        {
            errorReason = "유효한 위치가 아닙니다";
            return false;
        }

        gridIndex = idx.Value;
        return true;
    }

    /// <summary>
    /// Commit에서 pendingIndex가 유효한지 체크하는 헬퍼
    /// </summary>
    protected bool ValidatePendingIndex(int? pendingIndex, ShopContext ctx, out string errorReason)
    {
        errorReason = null;
        if (!pendingIndex.HasValue)
        {
            errorReason = "위치 선택이 유효하지 않습니다";
            ctx?.ShowError?.Invoke(errorReason);
            return false;
        }
        return true;
    }
}

// ���Ե� ��Ÿ�� ���ؽ�Ʈ
public class ShopContext
{
    //public PlayerManager Player;
    public Grid Grid;
    //public WaveManager Wave;
    //public BugManager Bugs;
    public EconomyManager Economy;
    public ShopManager Shop;
    public object Session; // �ʿ��ϸ� �������̽��� Ȯ��

    public System.Action<string> ShowInfo;
    public System.Action<string> ShowError;
    public System.Action<string> ShowGuide;
}