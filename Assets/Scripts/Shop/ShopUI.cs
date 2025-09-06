using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class ShopUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShopServices services;
    [SerializeField] private Transform fixedParent;     // 트럭 상단 3개
    [SerializeField] private Transform rotationParent;  // 트럭 하단 3개
    [SerializeField] private ItemSlot itemSlotPrefab;
    [SerializeField] private TMP_Text footerText;       // 화면 하단 정보/에러 표기 텍스트
    [SerializeField] private TMP_Text guideText;

    [Header("Switch")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    public event Action OnShopClosed;

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
        ctx = new ShopContext
        {
            Grid = services.Grid,
            Economy = services.Economy,
            Session = session,
            ShowInfo = ShowInfo,
            ShowError = ShowError,
            ShowGuide = ShowGuide
        };

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    public void BuildShop()
    {
        ClearChildren(fixedParent);
        ClearChildren(rotationParent);
        slots.Clear();

        // ShopManager에서 인벤토리 생성
        var inv = shopManager.GenerateInventory(ctx, GameManager.Instance.stage);

        // 상단: 고정
        for (int i = 0; i < inv.Fixed.Count; i++)
        {
            var data = inv.Fixed[i];
            if (data == null) continue;
            MakeSlot(fixedParent, data);
        }

        // 하단: 로테이션
        foreach (var data in inv.Rotation)
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

        if ((data.Price > services.Economy.GetGold()))
        {
            ShowError("구매 불가");
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
                    services.Placement.BeginPlantSelection(
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
            slot.OnPurchased(data.IsStackable ? 1 : int.MaxValue);
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

    private void ShowInfo(string msg) { if (footerText) { footerText.color = Color.white; footerText.text = msg; } }
    private void ShowError(string msg) { if (footerText) { footerText.color = Color.red; footerText.text = msg; } }

    public void ShowGuide(string msg) { if (guideText) guideText.text = msg; }
    public void ClearGuide() { if (guideText) guideText.text = ""; }

    private class ShopSession
    {
        private HashSet<ItemData> once = new();
        public bool WasBought(ItemData e) => once.Contains(e);
        public void MarkBought(ItemData e) => once.Add(e);
        public void ClearThisShop() => once.Clear();
    }

    public void Open()
    {
        session?.ClearThisShop();
        panel.SetActive(true);
        BuildShop();
        ClearInfo();
        ClearGuide();
        animationManager.SwitchCameras(CameraManager.CameraType.Shop);
        Debug.Log("상점 오픈!");
    }

    public void Close()
    {
        panel.SetActive(false);
        animationManager.SwitchCameras(CameraManager.CameraType.Normal);
        OnShopClosed?.Invoke();
    }
}
