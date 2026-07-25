using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCanvasController : MonoBehaviour
{

    [SerializeField] private RectTransform layerOpenRect;
    [SerializeField] private RectTransform layerCloseRect;

    [SerializeField] private RectTransform itemListLayer;
    [SerializeField] private RectTransform descriptionLayer;

    [SerializeField] private float layerMoveDuration = 0.25f;
    [SerializeField] private Ease layerMoveEase = Ease.InOutSine;


    [SerializeField] private Image[] showBtns;
    [SerializeField] private TextMeshProUGUI[] showBtnTexts;

    [SerializeField] private TMP_FontAsset pretendard_Bold;
    [SerializeField] private Color boldColor;
    [SerializeField] private TMP_FontAsset pretendard_Medium;
    [SerializeField] private Color mediumColor;

    [Space(10)]
    [Header("Item List")]
    [SerializeField] private Transform itemListContent;   // 아이템 슬롯이 생성될 부모
    [SerializeField] private ItemController itemPrefab;   // 아이템 슬롯 프리팹

    [Space(10)]
    [Header("Reroll")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollButtonText;

    [Space(10)]
    [Header("Detail Panel")]
    [SerializeField] private Image detailImage;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private TextMeshProUGUI detailPrice;
    [SerializeField] private Button buyButton;

    // 상점 로직(뷰 비의존). 구매/리롤/인벤토리 생성은 전부 여기로 위임.
    private ShopController shop;

    // 현재 목록에 표시 중인 슬롯들
    private readonly List<ItemController> slots = new();

    // 상세 패널에서 보고 있는 아이템
    private ItemController selectedSlot;

    // 마지막으로 선택한 탭 (0=전체, 1=고정, 2=로테이션) — 리롤/구매 후 같은 탭 유지
    private int currentTab = 0;

    private void Awake()
    {
        shop = new ShopController();
    }

    private void OnEnable()
    {
        // 상점을 열 때마다 목록을 최신 상태로 (다른 UI/시스템이 리롤했을 수 있음)
        shop.InvalidateInventory();
        ShowAll();
        UpdateRerollButton();
    }


    /// <summary>
    /// 로테이션 아이템들을 리롤한다. 무료 횟수가 있으면 무료, 없으면 골드를 소모한다.
    /// </summary>
    public void Reroll()
    {
        PhoneManager.Instance?.PhoneTouchEffect();

        if (shop.TryReroll())
        {
            CloseItemDetailPanel();
            RefreshCurrentTab();
            UpdateRerollButton();
        }
    }

    public void ShowAll()
    {
        var inv = shop.GetInventory();

        var all = new List<ItemData>(inv.Fixed);
        all.AddRange(inv.Rotation);
        UpdateItems(all);

        currentTab = 0;
        SetMenuStyle(0);
    }

    public void ShowFixed()
    {
        UpdateItems(shop.GetInventory().Fixed);

        currentTab = 1;
        SetMenuStyle(1);
    }

    public void ShowRotation()
    {
        UpdateItems(shop.GetInventory().Rotation);

        currentTab = 2;
        SetMenuStyle(2);
    }

    private void SetMenuStyle(int mainBtn)
    {
        foreach (var btn in showBtns)
        {
            btn.color = new Color(1f, 1f, 1f, 0.25f);
        }
        foreach (var btn in showBtnTexts)
        {
            btn.font = pretendard_Medium;
            btn.color = mediumColor;
        }

        showBtns[mainBtn].color = new Color(1f, 1f, 1f, 1f);
        showBtnTexts[mainBtn].font = pretendard_Bold;
        showBtnTexts[mainBtn].color = boldColor;
    }



    public void ShowItemDetailPanel()
    {
        // 유일하게 내가 미리 구현해두는 함수

        descriptionLayer.gameObject.SetActive(true);
        descriptionLayer.DOAnchorPosY(layerOpenRect.anchoredPosition.y, layerMoveDuration).SetEase(layerMoveEase).OnComplete(() =>
        {
            itemListLayer.gameObject.SetActive(false);
        });
    }

    public void CloseItemDetailPanel()
    {
        // 유일하게 내가 미리 구현해두는 함수

        itemListLayer.gameObject.SetActive(true);
        descriptionLayer.DOAnchorPosY(layerCloseRect.anchoredPosition.y, layerMoveDuration).SetEase(layerMoveEase).OnComplete(() =>
        {
            descriptionLayer.gameObject.SetActive(false);
        });
    }

    /// <summary>아이템 슬롯 클릭 → 상세 패널에 정보를 채우고 연다. (ItemController에서 호출)</summary>
    public void OnClickItem(ItemController slot)
    {
        if (slot == null || slot.Data == null) return;

        selectedSlot = slot;
        var data = slot.Data;

        if (detailImage != null)
        {
            detailImage.sprite = data.Icon;
            detailImage.enabled = data.Icon != null;
        }
        if (detailName != null) detailName.text = data.DisplayName;
        if (detailDescription != null) detailDescription.text = data.Description;

        int price = data.GetDisplayPrice();
        bool priceHidden = price == int.MaxValue;
        if (detailPrice != null)
        {
            detailPrice.gameObject.SetActive(!priceHidden);
            if (!priceHidden) detailPrice.text = $"{price} G";
        }

        if (buyButton != null)
        {
            buyButton.interactable = !priceHidden && !shop.WasBoughtThisShop(data);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(BuyItem);
        }

        ShowItemDetailPanel();
    }


    /// <summary>
    /// 상세 패널에서 구매. 즉시형은 바로, 배치/식물 선택형은 선택 완료 후 결제된다.
    /// </summary>
    public void BuyItem()
    {
        if (selectedSlot == null || selectedSlot.Data == null) return;

        var data = selectedSlot.Data;

        // 배치/선택형은 농장을 봐야 하므로 상세 패널을 먼저 닫는다.
        if (data.FlowType != ShopFlowType.Instant)
            CloseItemDetailPanel();

        shop.Buy(data, onPurchased: () =>
        {
            CloseItemDetailPanel();
            RefreshCurrentTab();   // 가격/재고/구매 제한 반영
            UpdateRerollButton();  // 상점 연락처 등 무료 리롤 변동 반영
        });
    }


    /// <summary>
    /// 주어진 아이템 목록으로 슬롯을 다시 생성한다.
    /// (ShowAll/ShowFixed/ShowRotation이 필터링한 결과를 넘겨준다)
    /// </summary>
    private void UpdateItems(List<ItemData> items)
    {
        if (itemListContent == null || itemPrefab == null) return;

        // 기존 슬롯 제거
        for (int i = itemListContent.childCount - 1; i >= 0; i--)
            Destroy(itemListContent.GetChild(i).gameObject);
        slots.Clear();
        selectedSlot = null;

        if (items == null) return;

        foreach (var data in items)
        {
            if (data == null) continue;

            var slot = Instantiate(itemPrefab, itemListContent);
            slot.gameObject.SetActive(true);
            slot.Bind(data, this);
            slots.Add(slot);
        }
    }

    /// <summary>현재 선택된 탭 기준으로 목록을 다시 그린다.</summary>
    private void RefreshCurrentTab()
    {
        switch (currentTab)
        {
            case 1: ShowFixed(); break;
            case 2: ShowRotation(); break;
            default: ShowAll(); break;
        }
    }

    private void UpdateRerollButton()
    {
        if (rerollButtonText != null) rerollButtonText.text = shop.GetRerollLabel();

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(Reroll);
        }
    }

    /// <summary>매일 자동 리롤 (스테이지 전환 시 호출).</summary>
    public void DailyReroll()
    {
        shop.DailyReroll();
        CloseItemDetailPanel();
        RefreshCurrentTab();
        UpdateRerollButton();
    }
}
