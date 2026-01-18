using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Items/Gene Extractor (유전자 추출기)", fileName = "GeneExtractorItemData")]
public class GeneExtractorItemData : ItemData
{
    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1; // 1�������� ����
    [Min(0)] public int rotationWeight = 8;

    private Plant selected; // ����ڰ� ������ ���� �Ĺ�

    private void OnValidate()
    {
        FlowType = ShopFlowType.SelectExistingPlant; // �Ĺ� ���� �÷ο�
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "������ �����";
        if (Price <= 0) Price = 1000;
    }

    // ���� �����̼� �ĺ� ����/����ġ ����������������������������������������������������������
    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return (stage + 1) >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    // ���� ���� ���ɿ���(�̸�) ����������������������������������������������������������������������
    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        var g = ctx.Grid;
        if (!g) { reason = "Grid ����"; return false; }

        // �ּ� 1ĭ �̻� �� ĭ�� �־�� �ǹ� ����
        int maxSlots = g.maxCol * 4;
        int empty = maxSlots - g.plantGrid.Count;
        if (empty <= 0) { reason = "�� ĭ�� �����ϴ�"; return false; }

        // ���� ����� �� �׷絵 ���ٸ� ��� �Ұ�
        if (g.plantGrid.Count <= 0) { reason = "������ �Ĺ��� �����ϴ�"; return false; }

        return true;
    }

    // ���� ���� �÷ο� �� ��������������������������������������������������������������������������������
    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        // ShopUI�� BeginPlantSelection���� ������ �� �� �� ���⼱ �ٷ� ok
        onReady?.Invoke();
    }

    public override bool ValidateTarget(ShopContext ctx, Plant target, out string reason)
    {
        if (target == null) { reason = "�Ĺ��� �����ϼ���"; return false; }
        reason = null;
        return true;
    }

    public override void SetSelectedPlant(Plant plant)
    {
        selected = plant;
    }

    // ���� ����(Ȯ��) ����������������������������������������������������������������������������������������
    public override void Commit(ShopContext ctx)
    {
        if (!selected)
        {
            ctx.ShowError?.Invoke("���õ� �Ĺ��� �����ϴ�");
            return;
        }

        var g = ctx.Grid;
        if (!g)
        {
            Debug.LogError("[GeneExtractor] Grid not found");
            return;
        }

        // ���� ���� ���� ����
        List<GeneticTrait> genes = selected.GetGeneticTrait(); // Plant�� �̹� ����

        // ���� ������ �� = min(3, �� ĭ ��)
        int maxSlots = g.maxCol * 4;
        int empty = maxSlots - g.plantGrid.Count;
        int toSpawn = Mathf.Clamp(3, 0, empty); // ��ĭ�� 0~2�� �׸�ŭ��

        int spawned = 0;
        for (int i = 0; i < toSpawn; i++)
        {
            // �������� �Ĺ����� ���� ������ Ȯ���� ������ ���� (Pea/Peanut)
            // (Nepenthes/ChiliPepper�� SetTrait �帧�� �ٸ� �� �־��)
            if (UnityEngine.Random.Range(0, 2) == 0)
                g.AddPea(genes);
            else
                g.AddPeanut(genes);

            spawned++;
        }

        if (spawned > 0)
            ctx.ShowInfo?.Invoke($"{DisplayName}: {spawned}�� ���� �Ϸ�");
        else
            ctx.ShowError?.Invoke("�� ĭ�� ���� �������� �ʾҽ��ϴ�");

        // ����
        selected = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        selected = null;
    }
}