using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Slot Parents")]
    [SerializeField] private Transform fixedParent;     // Ʈ�� ��� 3��
    [SerializeField] private Transform rotationParent;  // Ʈ�� �ϴ� 3��

    [Header("Prefabs")]
    [SerializeField] private ItemSlot itemSlotPrefab;

    [Header("Footer")]
    [SerializeField] private TMP_Text footerText;       // ȭ�� �ϴ� ����/���� ǥ�� �ؽ�Ʈ
    [SerializeField] private string cannotBuyMessage = "���� �Ұ�";

    [Header("Config")]
    [SerializeField] private ItemData[] fixedItems = new ItemData[3]; // ���� 3��(������� �ʰ� ����)
    [SerializeField] private List<ItemData> rotationPool;             // �����̼� �ĺ� ����Ʈ
    [SerializeField] private int rotationCount = 3;

    // �̹� ���� ���ǿ��� �̹� ���� ���� ���� �����ѡ� �������� ����� üũ
    private HashSet<ItemData> purchasedOnceSet = new();

    // ������ ���Ե� (���� �� ���ٿ�)
    private readonly List<ItemSlot> currentSlots = new();

    private void OnEnable()
    {
        BuildShop();
        ClearInfo();
    }

    public void BuildShop()
    {
        ClearChildren(fixedParent);
        ClearChildren(rotationParent);
        currentSlots.Clear();
        purchasedOnceSet.Clear();

        // ���: ���� ������ 3��
        for (int i = 0; i < 3 && i < fixedItems.Length; i++)
        {
            var data = fixedItems[i];
            if (data == null) continue;

            var slot = Instantiate(itemSlotPrefab, fixedParent);
            bool oneTime = !data.isStackable; // ������ �ִ� ��ǰ ���ܡ� �� stackable �ƴϸ� 1ȸ ����
            int initialStock = data.isStackable ? Mathf.Max(1, data.initialStock) : 1;

            slot.Init(this, data, oneTime, initialStock);
            currentSlots.Add(slot);
        }

        // �ϴ�: �����̼� ������ 3��(�ߺ� ����)
        var chosen = ChooseRandomUnique(rotationPool, rotationCount);
        foreach (var data in chosen)
        {
            var slot = Instantiate(itemSlotPrefab, rotationParent);
            bool oneTime = !data.isStackable;
            int initialStock = data.isStackable ? Mathf.Max(1, data.initialStock) : 1;

            slot.Init(this, data, oneTime, initialStock);
            currentSlots.Add(slot);
        }
    }

    // ���� �õ�: ��Ģ üũ + MarketManager���� ���� ����
    public bool TryBuy(ItemData data, bool oneTimeOnly)
    {
        // 1) 1ȸ ������ ��� �̹� ������ �Ұ�
        if (oneTimeOnly && purchasedOnceSet.Contains(data))
        {
            ShowError(cannotBuyMessage);
            return false;
        }

        // 2) �� �ִ��� Ȯ�� �� ����
        if (!ShopManager.Instance.TryBuy(data))
        {
            ShowError(cannotBuyMessage);
            return false;
        }

        // 3) ���� �� 1ȸ �����̸� ���� ���
        if (oneTimeOnly)
            purchasedOnceSet.Add(data);

        ClearInfo(); // ���� �� Ǫ�� ����(���ϸ� ������ ������ ���� ���� �־ ��)
        return true;
    }

    // �ϴ� ����/���� �ؽ�Ʈ
    public void ShowInfo(string msg)
    {
        if (footerText == null) return;
        footerText.text = msg;
        footerText.color = Color.white;
    }

    public void ShowError(string msg)
    {
        if (footerText == null) return;
        footerText.text = msg;
        footerText.color = Color.red;
    }

    public void ClearInfo()
    {
        if (footerText == null) return;
        footerText.text = "";
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            GameObject.Destroy(parent.GetChild(i).gameObject);
    }

    private static List<ItemData> ChooseRandomUnique(List<ItemData> pool, int count)
    {
        if (pool == null || pool.Count == 0) return new List<ItemData>();
        var list = pool.Where(x => x != null).Distinct().ToList();
        count = Mathf.Min(count, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list.GetRange(0, count);
    }
}
