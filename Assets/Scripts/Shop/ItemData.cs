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