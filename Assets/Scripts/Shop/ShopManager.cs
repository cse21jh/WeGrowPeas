using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    [Header("UI")]
    [SerializeField] private ShopUI shopUI;

    [Header("Open Rule")]
    [SerializeField] private int shopOpenDay = 1; // n일마다 오픈

    [Header("Inventory (Serialized here)")]
    [SerializeField] private ItemData[] fixedItems = new ItemData[3];  // 상단 고정 3종
    [SerializeField] private List<ItemData> rotationPool = new(); // 하단 로테이션 풀
    [SerializeField] private int rotationCount = 3;                    // 하단 슬롯 개수

    // itemId → 구매 개수
    private Dictionary<string, int> purchaseHistory = new Dictionary<string, int>();

    public class ShopInventory
    {
        public List<ItemData> Fixed = new();
        public List<ItemData> Rotation = new();
    }

    // ShopUI가 호출해 쓰는 진입점: 현재 스테이지/컨텍스트로 인벤토리 생성
    public ShopInventory GenerateInventory(ShopContext ctx, int currentDay)
    {
        var inv = new ShopInventory();

        // 상단: 고정
        foreach (var it in fixedItems)
            if (it) inv.Fixed.Add(it);

        // 하단: 로테이션 (ItemData가 해금/가중치 제공)
        var candidates = new List<ItemData>();
        foreach (var it in rotationPool) // 이제 rotationPool은 List<ItemData>
        {
            if (!it) continue;
            if (!it.IsRotationUnlockOk(ctx)) continue;           // 해금 조건(각 아이템에서 override)
            if (it.GetRotationWeight(ctx) <= 0) continue;        // 가중치 0 이하는 제외
            candidates.Add(it);
        }

        // 가중치 기반 중복 없이 N개 추첨 (WeightedRandom 유틸 사용 시)
        inv.Rotation = Game.Util.WeightedRandom.PickWithoutReplacement(
            candidates,
            it => Mathf.Max(0, it.GetRotationWeight(ctx)),
            rotationCount
        );

        return inv;
    }

    public bool TryPurchase(ShopContext ctx, ItemData data, out string error)
    {
        error = null;

        if (data == null)
        {
            error = "아이템 없음";
            return false;
        }

        // 골드 체크
        if (!ctx?.Economy?.HasGold(data.Price) ?? true)
        {
            error = "골드 부족";
            return false;
        }

        // 결제
        ctx.Economy.SpendGold(data.Price);

        // 효과 확정 반영
        data.Commit(ctx);

        // 히스토리(종류별 개수 집계) : 이름(or asset name) 기준
        var key = string.IsNullOrEmpty(data.DisplayName) ? data.name : data.DisplayName;
        if (purchaseHistory.ContainsKey(key)) purchaseHistory[key]++;
        else purchaseHistory[key] = 1;

        return true;
    }


    public IEnumerator ShopPhase()
    {
        // n일마다만 상점 오픈
        if (GameManager.Instance.stage % shopOpenDay != 0)
            yield break;

        // UI 열기
        shopUI.Open();

        // UI에서 닫힐 때까지 대기
        bool closed = false;
        shopUI.OnShopClosed += () => closed = true;

        while (!closed)
            yield return null;
    }
}
