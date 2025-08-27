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
