using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;

public class ShopUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform itemsParent;     // 모든 슬롯을 넣을 통합 부모
    [SerializeField] private ItemSlot itemSlotPrefab;
    [SerializeField] private TMP_Text footerText;       // 화면 하단 정보/에러 표기 텍스트

    // auto-resolved services
    private Grid grid;
    private EconomyManager economy;
    private PlacementController placement;

    // 생성된 슬롯들 (갱신 시 접근용)
    private readonly List<ItemSlot> slots = new();

    private ShopContext ctx;
    private ShopSession session;

    private ShopManager shopManager;
    private UIAnimationManager animationManager;

    private void Awake()
    {
        shopManager = ShopManager.Instance;
        animationManager = FindAnyObjectByType<UIAnimationManager>();
        session = new ShopSession();
        // Resolve services: prefer GameManager singletons, fallback to scene lookups
        if (GameManager.Instance != null)
        {
            grid = GameManager.Instance.grid;
            economy = GameManager.Instance.economyManager;
        }
        else
        {
            grid = FindAnyObjectByType<Grid>();
            economy = FindAnyObjectByType<EconomyManager>();
        }

        // PlacementController has no global singleton — try common locations first
        var placementObj = GameObject.Find("PlacementController");
        if (placementObj != null) placement = placementObj.GetComponent<PlacementController>();
        if (placement == null) placement = FindAnyObjectByType<PlacementController>();
        ctx = new ShopContext
        {
            Grid = grid,
            Economy = economy,
            Session = session,
            Shop = shopManager,
            ShowInfo = ShowInfo,
            ShowError = ShowError,
        };

        BuildShop();
    }

    public void BuildShop()
    {
        ClearChildren(itemsParent);
        slots.Clear();

        // ShopManager에서 인벤토리 생성
        var inv = shopManager.GenerateInventory(ctx, GameManager.Instance.stage);

        // 고정 + 로테이션 모두 통합 부모에 생성
        for (int i = 0; i < inv.Fixed.Count; i++)
        {
            var data = inv.Fixed[i];
            if (data == null) continue;
            MakeSlot(itemsParent, data);
        }

        foreach (var data in inv.Rotation)
            MakeSlot(itemsParent, data);
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

        if (data.GetDisplayPrice() > (economy != null ? economy.GetGold() : 0))
        {
            ShowError("구매 불가");
            return;
        }

        // 효과 시작 → 플로우 분기
        data.StartEffect(ctx, onReady: () =>
        {
            SoundManager.Instance.PlayEffect("Button");
            switch (data.FlowType)
            {
                case ShopFlowType.Instant:
                    TryChargeAndCommit(data, slot);
                    break;

                case ShopFlowType.PlaceOnTile:
                    placement?.BeginTilePlacement(
                        ctx,
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
                    placement?.BeginPlantSelection(
                        ctx,
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

        if (shopManager.TryPurchase(ctx, data, out var err))
        {
            if (!data.IsStackable) session.MarkBought(data);
            slot.OnPurchased();
            ShowInfo($"{data.DisplayName} 구매 완료");
        }
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

    private void ShowInfo(string msg)
    {
        if (footerText == null) return;
        footerText.color = Color.white;
        footerText.text = msg;
    }

    private void ShowError(string msg)
    {
        PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
        {
            title = "Error",
            message = msg,
            duration = 3.5f
        });
    }

    private class ShopSession
    {
        private HashSet<ItemData> once = new();
        public bool WasBought(ItemData e) => once.Contains(e);
        public void MarkBought(ItemData e) => once.Add(e);
        public void ClearThisShop() => once.Clear();
    }
}
