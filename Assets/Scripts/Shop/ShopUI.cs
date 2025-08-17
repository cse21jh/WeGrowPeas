using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using DG.Tweening.Core.Easing;
using UnityEngine.Rendering;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class ShopUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShopServices services;
    [SerializeField] private Transform fixedParent;     // 트럭 상단 3개
    [SerializeField] private Transform rotationParent;  // 트럭 하단 3개
    [SerializeField] private ItemSlot itemSlotPrefab;
    [SerializeField] private TMP_Text footerText;       // 화면 하단 정보/에러 표기 텍스트

    [Header("Config")]
    [SerializeField] private ItemData[] fixedItems = new ItemData[3]; // 고정 3종(비어있지 않게 세팅)
    [SerializeField] private List<RotationEntry> rotationPool;           // 로테이션 후보 리스트
    [SerializeField] private int rotationCount = 3;

    // 생성된 슬롯들 (갱신 시 접근용)
    private readonly List<ItemSlot> slots = new();

    [System.Serializable]
    public class RotationEntry
    {
        public ItemData data;
        public int weight = 1;     // 0이하면 무시
        public int unlockDay = 0;  // 게임 진행 일수/턴 등과 비교해서 해금
    }

    private ShopContext ctx;
    private ShopSession session;

    private void Awake()
    {
        session = new ShopSession();
        ctx = new ShopContext
        {
            //Player = services.Player,
            Grid = services.Grid,
            //Wave = services.Wave,
            //Bugs = services.Bugs,
            Economy = services.Economy,
            Session = session,
            ShowInfo = ShowInfo,
            ShowError = ShowError
        };
    }

    private void OnEnable()
    {
        BuildShop();
        ClearInfo();
    }

    public void BuildShop()
    {
        ClearChildren(fixedParent);
        ClearChildren(rotationParent);
        slots.Clear();


        // 상단: 고정 아이템 3개
        for (int i = 0; i < 3 && i < fixedItems.Length; i++)
        {
            var data = fixedItems[i];
            if (data == null) continue;
            MakeSlot(fixedParent, data);
        }

        // 하단: 로테이션 아이템 3개(중복 없이)
        var chosen = PickRotationUnique(rotationPool, rotationCount, GameManager.Instance.stage);
        foreach (var data in chosen)
            MakeSlot(rotationParent, data);
    }

    private void MakeSlot(Transform parent, ItemData data)
    {
        var slot = Instantiate(itemSlotPrefab, parent);
        slot.Bind(this, data);
        slots.Add(slot);
    }

    public void OnClickBuy(ItemData data, ItemSlot slot)
    {
        // 상점 세션 1회 제한 처리
        if (data.OnePerShopIfNotStackable && !data.IsStackable && session.WasBought(data))
        {
            ShowError("구매 불가");
            return;
        }

        // 구매 가능 체크 (중복 상태/해금 등)
        if (!data.CanPurchase(ctx, out string why))
        {
            ShowError(why ?? "구매 불가");
            return;
        }

        // 효과 시작 → 플로우 분기
        data.StartEffect(ctx, onReady: () =>
        {
            switch (data.FlowType)
            {
                case ShopFlowType.Instant:
                    TryChargeAndCommit(data, slot);
                    break;

                case ShopFlowType.PlaceOnTile:
                    services.Placement.BeginTilePlacement(
                        validate: (pos) => data.ValidatePosition(ctx, pos, out _),
                        onConfirm: (pos) =>
                        {
                            data.SetPlacedPosition(pos);
                            TryChargeAndCommit(data, slot);
                        },
                        onCancel: () => data.Cancel(ctx)
                    );
                    break;

                case ShopFlowType.SelectExistingPlant:
                    services.Placement.BeginPlantSelection(
                        validate: (plant) => data.ValidateTarget(ctx, plant, out _),
                        onConfirm: (plant) =>
                        {
                            data.SetSelectedPlant(plant);
                            TryChargeAndCommit(data, slot);
                        },
                        onCancel: () => data.Cancel(ctx)
                    );
                    break;
            }
        },
        onError: (err) => ShowError(err ?? "구매 불가"));
    }
    private void TryChargeAndCommit(ItemData data, ItemSlot slot)
    {
        if (!services.Economy.HasGold(data.Price))
        {
            ShowError("구매 불가");
            data.Cancel(ctx);
            return;
        }

        services.Economy.SpendGold(data.Price);
        data.Commit(ctx);

        if (!data.IsStackable) session.MarkBought(data);
        slot.OnPurchased(data.IsStackable ? 1 : int.MaxValue); // 스택형: 1 감소, 비스택형: 즉시 품절
        ShowInfo($"{data.DisplayName} 구매 완료");
    }
    public void ClearInfo()
    {
        if (footerText == null) return;
        footerText.text = "";
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private List<ItemData> PickRotationUnique(List<RotationEntry> pool, int count, int currentDay)
    {
        var candidates = new List<RotationEntry>();
        foreach (var e in pool)
        {
            if (e?.data == null) continue;
            if (e.weight <= 0) continue;
            if (currentDay < e.unlockDay) continue; // 해금 이전 제외
            if (!e.data.IsRotationUnlockOk(ctx)) continue; // 효과 자체의 해금 조건(웨이브 해금 등)
            candidates.Add(e);
        }

        var result = new List<ItemData>();
        // 가중치 중복 없는 추출
        for (int k = 0; k < count; k++)
        {
            if (candidates.Count == 0) break;
            int total = 0;
            foreach (var c in candidates) total += c.weight;

            int r = Random.Range(0, total);
            int acc = 0;
            int idx = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += candidates[i].weight;
                if (r < acc) { idx = i; break; }
            }
            result.Add(candidates[idx].data);
            candidates.RemoveAt(idx); // 중복 방지
        }
        return result;
    }

    private void ShowInfo(string msg) { if (footerText) { footerText.color = Color.white; footerText.text = msg; } }
    private void ShowError(string msg) { if (footerText) { footerText.color = Color.red; footerText.text = msg; } }
    private class ShopSession
    {
        private HashSet<ItemData> once = new();
        public bool WasBought(ItemData e) => once.Contains(e);
        public void MarkBought(ItemData e) => once.Add(e);
        public void ClearThisShop() => once.Clear();
    }

}
