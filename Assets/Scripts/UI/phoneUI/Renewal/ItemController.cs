using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemController : MonoBehaviour
{
    [SerializeField] private Image itemImage;

    [SerializeField] private GameObject detail_ItemType;
    [SerializeField] private GameObject detail_ItemGrade;
    [SerializeField] private GameObject detail_ItemLimit;

    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemPrice;
    [SerializeField] private TextMeshProUGUI itemPurchaseLimit;

    [Header("Badge Texts (고정 상품 / 등급 / 품목 제한)")]
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemGradeText;
    [Tooltip("배지 옆 재고 문구 (예: 재고 제한 없음 / 남은 수량 2)")]
    [SerializeField] private TextMeshProUGUI itemStockText;

    [SerializeField] private GameObject[] itemTags;
    [SerializeField] private TextMeshProUGUI[] itemTagTexts;

    [Header("Buy")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [Tooltip("슬롯 전체를 눌렀을 때 상세를 여는 버튼. 비우면 이 오브젝트의 Button을 사용.")]
    [SerializeField] private Button slotButton;

    private ItemData data;
    private ShopCanvasController owner;

    /// <summary>이 슬롯이 표시 중인 아이템.</summary>
    public ItemData Data => data;

    #region
    /// <summary>
    /// 아이템 하나를 슬롯에 표시한다. 클릭 시 소유 컨트롤러의 상세 패널을 연다.
    /// </summary>
    /// <param name="item"> 표시할 아이템 데이터 </param>
    /// <param name="shop"> 클릭 콜백을 받을 상점 컨트롤러 </param>
    /// <param name="isFixed"> 고정 상품인가(아니면 로테이션 상품) </param>
    public void Bind(ItemData item, ShopCanvasController shop, bool isFixed)
    {
        data = item;
        owner = shop;
        if (item == null) return;

        SetItemDetail(
            item.Icon,
            item.DisplayName,
            item.GetDisplayPrice(),
            GetRemainingPurchaseCount(item),
            ShopBadge.GetTags(item));

        SetBadges(item, isFixed);

        // 슬롯 클릭 → 상세 패널 (구매 버튼과 겹치지 않도록 별도 지정 가능)
        var btn = slotButton != null ? slotButton : GetComponent<Button>();
        if (btn != null && btn != buyButton)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                PhoneManager.Instance?.PhoneTouchEffect();
                owner?.OnClickItem(this);
            });
        }

        SetupBuyButton(item);
    }

    /// <summary>
    /// 슬롯의 구매 버튼. 선택지가 필요한 아이템(드롭다운 보유)은 바로 사지 않고 상세 패널을 연다.
    /// </summary>
    private void SetupBuyButton(ItemData item)
    {
        if (buyButton == null) return;

        var options = item.GetSelectableOptions();
        bool needsSelection = options != null && options.Length > 0;

        if (buyButtonText != null)
            buyButtonText.text = needsSelection ? "선택하기" : "구매하기";

        // 더 이상 살 수 없는 아이템(가격 숨김)은 비활성화
        buyButton.interactable = item.GetDisplayPrice() != int.MaxValue;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() =>
        {
            PhoneManager.Instance?.PhoneTouchEffect();

            if (needsSelection) owner?.OnClickItem(this);  // 상세에서 옵션 고르고 구매
            else owner?.BuyItemDirect(this);               // 옵션 없으면 즉시 구매
        });
    }

    /// <summary>배지 3종(고정 상품 / 등급 / 품목 제한)과 재고 문구 표시.</summary>
    private void SetBadges(ItemData item, bool isFixed)
    {
        ShopBadge.Apply(item, isFixed,
            typeObj: detail_ItemType, typeText: itemTypeText,
            gradeObj: detail_ItemGrade, gradeText: itemGradeText,
            limitObj: detail_ItemLimit, limitText: itemPurchaseLimit);

        if (itemStockText != null) itemStockText.text = ShopBadge.GetStockText(item);
    }

    /// <summary>
    /// 표시 값만 직접 지정하는 저수준 세팅. (데이터가 없는 미리보기 등에서 사용)
    /// </summary>
    /// <param name="image"> 아이템 아이콘 </param>
    /// <param name="name"> 아이템 이름 </param>
    /// <param name="price"> 표시 가격. int.MaxValue면 가격을 숨긴다 </param>
    /// <param name="countLimit"> 남은 구매 가능 횟수. 0 이하(무제한)면 표시하지 않는다 </param>
    /// <param name="tags"> 등급/속성 태그 </param>
    public void SetItemDetail(Sprite image, string name, int price, int countLimit, string[] tags)
    {
        if (itemImage != null)
        {
            itemImage.sprite = image;
            itemImage.enabled = image != null;
        }

        if (itemName != null) itemName.text = name;

        // 가격: int.MaxValue는 "더 이상 구매 불가" → 가격표 숨김 (기존 ItemSlot과 동일 규칙)
        bool priceHidden = price == int.MaxValue;
        if (itemPrice != null)
        {
            itemPrice.gameObject.SetActive(!priceHidden);
            if (!priceHidden) itemPrice.text = $"{price} G";
        }

        // 재고/구매 제한 문구는 Bind에서 배지와 함께 설정한다(SetBadges).
        // 데이터 없이 호출된 경우에만 여기서 직접 표시.
        if (data == null && itemPurchaseLimit != null)
        {
            bool hasLimit = countLimit > 0;
            if (detail_ItemLimit != null) detail_ItemLimit.SetActive(hasLimit);
            if (hasLimit) itemPurchaseLimit.text = $"{countLimit}";
        }

        // 태그: 있는 만큼만 켜기
        if (itemTags != null)
        {
            for (int i = 0; i < itemTags.Length; i++)
            {
                bool show = tags != null && i < tags.Length && !string.IsNullOrEmpty(tags[i]);
                if (itemTags[i] != null) itemTags[i].SetActive(show);
                if (show && itemTagTexts != null && i < itemTagTexts.Length && itemTagTexts[i] != null)
                    itemTagTexts[i].text = tags[i];
            }
        }

    }
    #endregion

    /// <summary>남은 구매 가능 횟수. MaxPurchaseCount가 -1(무제한)이면 0을 반환(표시 안 함).</summary>
    private static int GetRemainingPurchaseCount(ItemData item)
    {
        if (item.MaxPurchaseCount < 0) return 0;
        return Mathf.Max(0, item.MaxPurchaseCount - item.GetTotalPurchaseCount());
    }
}
