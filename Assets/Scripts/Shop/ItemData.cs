using System.Collections.Generic;
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

/// <summary>
/// 아이템의 성격 태그. 등급(<see cref="ItemRarity"/>)과는 별개로,
/// 이 아이템이 어떤 식으로 동작하는지를 상점 슬롯에서 한눈에 보여준다.
/// </summary>
public enum ItemTag
{
    /// <summary>설치: 토양을 클릭해 새로운 오브젝트를 배치한다.</summary>
    Install,
    /// <summary>선택: 이미 있는 식물을 골라야 한다.</summary>
    Select,
    /// <summary>발동형: 효과가 발동하면 사라진다.</summary>
    Trigger,
    /// <summary>기간제: 일정 기간 동안만 지속된다.</summary>
    Timed,
    /// <summary>수익성: 골드와 직접적인 연관이 있다.</summary>
    Profit,
    /// <summary>강화: 이미 있는 아이템의 성능을 올린다.</summary>
    Upgrade
}

public static class ItemTagExtensions
{
    /// <summary>상점 슬롯에 찍히는 이름.</summary>
    public static string ToDisplayName(this ItemTag tag)
    {
        switch (tag)
        {
            case ItemTag.Install: return "설치";
            case ItemTag.Select: return "선택";
            case ItemTag.Trigger: return "발동형";
            case ItemTag.Timed: return "기간제";
            case ItemTag.Profit: return "수익성";
            case ItemTag.Upgrade: return "강화";
            default: return tag.ToString();
        }
    }

    /// <summary>태그에 마우스를 올렸을 때 보여줄 설명.</summary>
    public static string ToDescription(this ItemTag tag)
    {
        switch (tag)
        {
            case ItemTag.Install: return "토양을 클릭해 새로운 오브젝트를 배치합니다.";
            case ItemTag.Select: return "이미 있는 식물을 선택해야 합니다.";
            case ItemTag.Trigger: return "효과가 발동하면 사라집니다.";
            case ItemTag.Timed: return "일정 기간 동안만 지속됩니다.";
            case ItemTag.Profit: return "골드와 직접적인 연관이 있습니다.";
            case ItemTag.Upgrade: return "이미 있는 아이템의 성능을 향상시킵니다.";
            default: return "";
        }
    }

    /// <summary>표시 이름으로 태그를 되찾는다. UI가 문자열만 들고 있을 때 쓴다.</summary>
    public static bool TryParseDisplayName(string displayName, out ItemTag tag)
    {
        foreach (ItemTag t in System.Enum.GetValues(typeof(ItemTag)))
        {
            if (t.ToDisplayName() == displayName)
            {
                tag = t;
                return true;
            }
        }

        tag = default;
        return false;
    }
}

[BalanceGroup("Shop", "Items")] // 서브클래스 전부 Shop/Items.csv 한 파일로
public abstract class ItemData : ScriptableObject
{
    [Header("Meta")]
    public string DisplayName;
    public Sprite Icon;
    [TextArea] public string Description;
    [Balance("가격")] public int Price;

    [Header("Rarity")]
    [Balance("등급")] public ItemRarity Rarity = ItemRarity.Common;
    
    [Header("Grade Tag")]
    [Tooltip("등급 태그 텍스트 (예: S, A, B, C)")]
    public string GradeTagText;
    [Tooltip("등급 태그 이미지")]
    public Sprite GradeTagImage;

    [Header("Tags")]
    [Tooltip("아이템 성격 태그. 상점 슬롯 하단에 표시된다. 없는 아이템도 있다.")]
    public List<ItemTag> Tags = new List<ItemTag>();

    [Header("Rule")]
    public bool IsStackable = false;
    [Balance("초기 재고")] public int InitialStock = 1;
    public bool OnePerShopIfNotStackable = true;
    [Balance("최대 구매")]
    [Tooltip("-1 = ������, 0 �̻� = ���� ��ü���� �ִ� ���� Ƚ��")]
    public int MaxPurchaseCount = -1;

    [Header("Flow")]
    public ShopFlowType FlowType;

    /// <summary>
    /// 설치형 아이템 구매 후, 위치/식물을 고를 때까지 계속 표시할 안내 문구.
    /// 선택을 완료하거나 취소하면 사라진다. 필요하면 아이템별로 override.
    /// </summary>
    public virtual string GetPlacementGuide()
    {
        switch (FlowType)
        {
            case ShopFlowType.PlaceOnTile: return "토양을 선택해주세요 (좌클릭=확정, 우클릭/ESC=취소)";
            case ShopFlowType.SelectExistingPlant: return "식물을 선택해주세요 (좌클릭=확정, 우클릭/ESC=취소)";
            default: return null;
        }
    }

    /// <summary>
    /// 구매 전에 상세 패널의 드롭다운으로 고를 수 있는 선택지.
    /// null/빈 배열이면 드롭다운을 숨긴다. (예: 전용 비료 = 웨이브 목록)
    /// 선택 결과는 <see cref="SetSelectedOption"/>로 전달된다.
    /// </summary>
    public virtual string[] GetSelectableOptions() => null;

    /// <summary>드롭다운에서 고른 항목의 인덱스를 전달. 옵션이 있는 아이템만 override.</summary>
    public virtual void SetSelectedOption(int index) { }

    [Header("Unlock")]
    [Tooltip("체크 시 해금되기 전까지 상점에 뜨지 않음. UnlockManager.Unlock(...)로 해금.")]
    public bool requiresUnlock = false;
    [Tooltip("해금 식별 id. 비우면 에셋 이름을 사용.")]
    public string unlockId;
    public string UnlockId => string.IsNullOrEmpty(unlockId) ? name : unlockId;

    [Header("Meta Unlock (런과 무관한 영구 해금 조건)")]
    [Tooltip("해금에 필요한 새벽 클리어 단계. 0 = 조건 없음.")]
    public int metaRequiredDawnStage = 0;
    [Tooltip("새벽 단계를 클리어해야 하는 식물(\"완두콩\"/\"땅콩\"). 비우면 어느 식물로 클리어하든 인정.")]
    public string metaRequiredDawnPlant;
    [Tooltip("해금에 필요한 인게임 사건 id. 비우면 조건 없음. UnlockManager.Ids 참고.")]
    public string metaRequiredEventId;

    /// <summary>
    /// 메타 진행(새벽 클리어·인게임 사건)만으로 판정한 해금 여부.
    /// 런 중에만 성립하는 조건(현재 스테이지·재배 중인 식물 종류 등)은 <see cref="IsRotationUnlockOk"/>가 담당한다.
    /// 결과창의 "새로 해금된 아이템" 판정도 이 값을 기준으로 한다.
    /// </summary>
    /// <summary>도감/안내용: 이 아이템이 어떻게 해금되는지 한국어로 설명. 조건이 없으면 빈 문자열.</summary>
    public string GetUnlockConditionText()
    {
        // 사건 해금이 우선(황금 식물·겨울·비료 4줄 등)
        if (!string.IsNullOrEmpty(metaRequiredEventId))
            return UnlockManager.GetEventDescription(metaRequiredEventId);

        // 새벽 단계 해금
        if (metaRequiredDawnStage > 0)
        {
            string p = string.IsNullOrEmpty(metaRequiredDawnPlant) ? "" : metaRequiredDawnPlant + "(으)로 ";
            return $"{p}새벽 {metaRequiredDawnStage}단계 클리어 시 해금됩니다.";
        }

        return "";
    }

    public bool IsMetaUnlocked()
    {
        // 해금 조건이 아예 없으면 항상 사용 가능.
        if (metaRequiredDawnStage <= 0 && string.IsNullOrEmpty(metaRequiredEventId))
            return true;

        // 조건이 있는 아이템은 UnlockManager에 실제 해금 기록이 있어야 한다.
        // (새벽 단계 조건 → 40일 클리어 시 UnlockGrants가 기록,
        //  사건 조건 → 사건 발생 시 UnlockGrants가 기록. 판정은 여기 한 곳으로 통일.)
        return UnlockManager.IsUnlocked(UnlockId);
    }

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
        // 새벽 상점 가격 배수 적용(표시·차감 공통). 가격을 override 하는 아이템은 각자 반영 필요.
        float mul = DawnSystem.Current.shopPriceMultiplier;
        float price = Price * (mul > 0f ? mul : 1f);

        // 저주(독점시장): 품목별 가격이 무작위 배율(하루 동안 고정)로 변동
        if (CurseState.ShopMonopoly)
        {
            int day = GameManager.Instance != null ? GameManager.Instance.stage : 0;
            float h = StableHash01(name + "#" + day);
            price *= Mathf.Lerp(CurseState.ShopPriceMinMul, CurseState.ShopPriceMaxMul, h);
        }
        return Mathf.RoundToInt(price);
    }

    // 문자열 → 0~1 결정적 해시(같은 품목·같은 날엔 항상 동일 → 가격 깜빡임 없음)
    private static float StableHash01(string s)
    {
        unchecked
        {
            uint h = 2166136261u;
            foreach (char c in s) { h ^= c; h *= 16777619u; }
            return (h % 10000u) / 10000f;
        }
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
    /// 전용 아이템용: 이번 런에서 키우는 식물이 지정한 종인지 확인. (예: "완두콩", "땅콩")
    /// </summary>
    protected static bool IsCurrentPlant(string speciesName)
    {
        return GameManager.Instance != null && GameManager.Instance.currentPlant == speciesName;
    }

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