using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/ChiliPepper (����)", fileName = "ChiliPepperItemData")]
public class ChiliPepperItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    // ��ġ Ȯ�� �� ����� �׸��� �ε���
    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "����";
        if (Price <= 0) Price = 1500;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.PlaceOnTile;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        // 고추 등장 확률 증가 적용
        int baseWeight = rotationWeight;
        if (ctx?.Grid != null)
        {
            float probabilityBonus = ctx.Grid.ChiliPepperSpawnProbability;
            // 확률을 가중치로 변환 (예: 0.02 = 2% -> 가중치 2 증가)
            int weightBonus = Mathf.RoundToInt(probabilityBonus * 100);
            return baseWeight + weightBonus;
        }
        return baseWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid ������ �����ϴ� (ShopContext.Grid ���� �ʿ�)";
            return false;
        }
        if (!ctx.Grid.HasEmptyGrid())
        {
            reason = "��ġ�� �� �ִ� ��ĭ�� �����ϴ�";
            return false;
        }
        reason = null;
        return true;
    }

    // ��ġ ��� ����: ���� �غ� ����
    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidatePosition(ShopContext ctx, Vector3 pos, out string reason)
    {
        reason = null;
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid ������ �����ϴ�";
            return false;
        }

        // ��ũ�� ��ǥ �� �׸��� �ε���
        int? idx = ctx.Grid.GetGridIndexFromPosition(pos);
        if (!idx.HasValue)
        {
            reason = "��ȿ�� ����� �ƴմϴ�";
            return false;
        }

        // �� ĭ���� Ȯ��
        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "�̹� �Ĺ��� �ִ� ĭ�Դϴ�";
            return false;
        }

        // ���� ������ Ȯ�� �ĺ� ����
        pendingIndex = idx.Value;
        return true;
    }
    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (ctx == null || ctx.Grid == null)
        {
            ctx?.ShowError?.Invoke("Grid ������ �����ϴ�");
            return;
        }
        if (!pendingIndex.HasValue)
        {
            ctx.ShowError?.Invoke("��ġ ��ġ�� ��ȿ���� �ʽ��ϴ�");
            return;
        }

        // ���� ��ġ
        ctx.Grid.AddChiliPepper(pendingIndex.Value);

        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
