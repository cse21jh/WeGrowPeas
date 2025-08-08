using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Slot Parents")]
    [SerializeField] private Transform fixedParent;     // 트럭 상단 3개
    [SerializeField] private Transform rotationParent;  // 트럭 하단 3개

    [Header("Prefabs")]
    [SerializeField] private ItemSlot itemSlotPrefab;

    [Header("Footer")]
    [SerializeField] private TMP_Text footerText;       // 화면 하단 정보/에러 표기 텍스트
    [SerializeField] private string cannotBuyMessage = "구매 불가";

    [Header("Config")]
    [SerializeField] private ItemData[] fixedItems = new ItemData[3]; // 고정 3종(비어있지 않게 세팅)
    [SerializeField] private List<ItemData> rotationPool;             // 로테이션 후보 리스트
    [SerializeField] private int rotationCount = 3;

    // 이번 상점 세션에서 이미 “한 번만 구매 가능한” 아이템을 샀는지 체크
    private HashSet<ItemData> purchasedOnceSet = new();

    // 생성된 슬롯들 (갱신 시 접근용)
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

        // 상단: 고정 아이템 3개
        for (int i = 0; i < 3 && i < fixedItems.Length; i++)
        {
            var data = fixedItems[i];
            if (data == null) continue;

            var slot = Instantiate(itemSlotPrefab, fixedParent);
            bool oneTime = !data.isStackable; // “개수 있는 물품 제외” → stackable 아니면 1회 제한
            int initialStock = data.isStackable ? Mathf.Max(1, data.initialStock) : 1;

            slot.Init(this, data, oneTime, initialStock);
            currentSlots.Add(slot);
        }

        // 하단: 로테이션 아이템 3개(중복 없이)
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

    // 구매 시도: 규칙 체크 + MarketManager에게 결제 위임
    public bool TryBuy(ItemData data, bool oneTimeOnly)
    {
        // 1) 1회 제한인 경우 이미 샀으면 불가
        if (oneTimeOnly && purchasedOnceSet.Contains(data))
        {
            ShowError(cannotBuyMessage);
            return false;
        }

        // 2) 돈 있는지 확인 및 결제
        if (!ShopManager.Instance.TryBuy(data))
        {
            ShowError(cannotBuyMessage);
            return false;
        }

        // 3) 성공 → 1회 제한이면 구매 기록
        if (oneTimeOnly)
            purchasedOnceSet.Add(data);

        ClearInfo(); // 성공 시 푸터 정리(원하면 “구매 성공” 같은 문구 넣어도 됨)
        return true;
    }

    // 하단 정보/에러 텍스트
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
