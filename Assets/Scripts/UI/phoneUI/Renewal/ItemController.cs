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

    [SerializeField] private GameObject[] itemTags;
    [SerializeField] private TextMeshProUGUI[] itemTagTexts;

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
    public void Bind(ItemData item, ShopCanvasController shop)
    {
        data = item;
        owner = shop;
        if (item == null) return;

        // 등급 태그가 비어 있으면 Rarity로 자동 생성(S/A/B/C)
        string grade = !string.IsNullOrEmpty(item.GradeTagText)
            ? item.GradeTagText
            : GetRarityGradeText(item.Rarity);

        SetItemDetail(
            item.Icon,
            item.DisplayName,
            item.GetDisplayPrice(),
            GetRemainingPurchaseCount(item),
            new[] { grade });

        // 슬롯 클릭 → 상세 패널
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                PhoneManager.Instance?.PhoneTouchEffect();
                owner?.OnClickItem(this);
            });
        }
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

        // 구매 제한: 0 이하면 무제한으로 보고 숨김
        bool hasLimit = countLimit > 0;
        if (detail_ItemLimit != null) detail_ItemLimit.SetActive(hasLimit);
        if (itemPurchaseLimit != null && hasLimit) itemPurchaseLimit.text = $"{countLimit}";

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

        if (detail_ItemGrade != null) detail_ItemGrade.SetActive(tags != null && tags.Length > 0);
    }
    #endregion

    /// <summary>남은 구매 가능 횟수. MaxPurchaseCount가 -1(무제한)이면 0을 반환(표시 안 함).</summary>
    private static int GetRemainingPurchaseCount(ItemData item)
    {
        if (item.MaxPurchaseCount < 0) return 0;
        return Mathf.Max(0, item.MaxPurchaseCount - item.GetTotalPurchaseCount());
    }

    private static string GetRarityGradeText(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Legendary => "S",
            ItemRarity.Special => "A",
            ItemRarity.Rare => "B",
            ItemRarity.Common => "C",
            _ => ""
        };
    }
}
