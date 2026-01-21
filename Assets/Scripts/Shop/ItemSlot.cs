using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text countText; // 스택형일 때만 사용
    [SerializeField] private Button buyButton;

    [SerializeField] private Animator leftAnim;
    [SerializeField] private Animator rightAnim;

    [Header("Grade Tag UI")]
    [SerializeField] private Image gradeTagImage;
    [SerializeField] private TMP_Text gradeTagText;

    [Header("Rarity Colors")]
    [Tooltip("등급별 색상 (순서: Common, Rare, Special, Legendary)")]
    [SerializeField] private Color[] rarityColors = new Color[4];

    private ItemData effect;
    private ShopUI shop;            // 콜백용
    private int stock;              // IsStackable이면 초기 n, 아니면 1
    private int maxStock;

    public void Bind(ShopUI shopUI, ItemData eff)
    {
        shop = shopUI;
        effect = eff;

        if (iconImage)
        {
            iconImage.sprite = eff.Icon;
        }
        if (nameText)
        {
            nameText.text = eff.DisplayName;
        }

        stock = eff.IsStackable ? Mathf.Max(1, eff.InitialStock) : 1;
        maxStock = stock;

        // 버튼 리스너 갱신
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => 
            {
                PhoneManager.Instance.PhoneTouchEffect();
                shop.OnClickShowPopup(effect, this);
            });
        }

        // Grade Tag 설정 - iconImage, nameText와 동일하게 설정만 하고 Refresh()에서 활성화/비활성화
        Color rarityColor = GetRarityColor(eff.Rarity);
        string gradeText = GetRarityGradeText(eff.Rarity);

        if (gradeTagImage != null)
        {
            if (eff.GradeTagImage != null)
            {
                gradeTagImage.sprite = eff.GradeTagImage;
            }
            else
            {
                gradeTagImage.sprite = null;
            }
            gradeTagImage.color = rarityColor;
        }

        if (gradeTagText != null)
        {
            // ItemData에 GradeTagText가 있으면 사용, 없으면 자동 생성된 등급 텍스트 사용
            if (!string.IsNullOrEmpty(eff.GradeTagText))
            {
                gradeTagText.text = eff.GradeTagText;
            }
            else
            {
                // 자동 생성된 등급 텍스트 사용 (S, A, B, C)
                gradeTagText.text = gradeText;
            }
            // 텍스트 색상은 변경하지 않음 (원래 색상 유지)
            // 프리팹에서 비활성화되어 있을 수 있으므로 명시적으로 활성화
            if (!string.IsNullOrEmpty(gradeTagText.text))
            {
                gradeTagText.gameObject.SetActive(true);
            }
        }

        Refresh();
    }

    public void OnPurchased()
    {
        // 비스택형도 구매 직후 슬롯 닫히도록 0 처리
        if (effect.IsStackable) stock = Mathf.Max(0, stock - 1);
        else if (effect.OnePerShopIfNotStackable) stock = 0;
        else if (effect.GetDisplayPrice() == int.MaxValue) stock = 0; // 가격표 떼기 조건이면 0
        
        Refresh();
    }

    /// <summary>
    /// 가격을 업데이트합니다 (구매 후 가격이 변경되는 아이템용).
    /// </summary>
    public void RefreshPrice()
    {
        Refresh();
    }

    private void Refresh()
    {
        int displayPrice = effect.GetDisplayPrice();
        bool priceHidden = (displayPrice == int.MaxValue);    // 가격표 떼기 조건
        bool soldOutByStock = (stock <= 0);
        bool shouldOpen = !(priceHidden || soldOutByStock);

        // 수량 UI
        if (countText)
        {
            if (effect.IsStackable)
            {
                countText.gameObject.SetActive(true);
                countText.text = $"{stock}/{maxStock}";
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }

        // 가격 UI (int.MaxValue면 가격표를 아예 숨김)
        if (priceText)
        {
            priceText.gameObject.SetActive(!priceHidden);
            if (!priceHidden) priceText.text = $"{displayPrice} G";
        }

        // 구매 버튼
        if (buyButton) buyButton.interactable = shouldOpen;

        // 상자 애니메이션(닫힘 처리)
        if (leftAnim) leftAnim.SetBool("isOpen", shouldOpen);
        if (rightAnim) rightAnim.SetBool("isOpen", shouldOpen);

        iconImage.gameObject.SetActive(shouldOpen);
        nameText.gameObject.SetActive(shouldOpen);

        // Grade Tag 활성화/비활성화 - iconImage, nameText와 동일하게 shouldOpen에 따라 처리
        // 단, 시작할 때는 항상 활성화되도록 보장
        if (gradeTagImage != null)
        {
            bool hasGradeTagImage = gradeTagImage.sprite != null;
            if (hasGradeTagImage)
            {
                gradeTagImage.gameObject.SetActive(shouldOpen);
            }
        }
        if (gradeTagText != null)
        {
            // text가 설정되어 있으면 표시 (ItemData의 GradeTagText 또는 자동 생성된 등급 텍스트)
            bool hasGradeTagText = !string.IsNullOrEmpty(gradeTagText.text);
            if (hasGradeTagText)
            {
                gradeTagText.gameObject.SetActive(shouldOpen);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shop == null || effect == null) return;

        int displayPrice = effect.GetDisplayPrice();
        bool priceHidden = (displayPrice == int.MaxValue);
        string priceLine = priceHidden ? "" : $"\n가격: {displayPrice} G";

        //shop.SendMessage("ShowInfo", $"{effect.DisplayName}\n{effect.Description}{priceLine}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shop == null) return;
        //shop.SendMessage("ShowInfo", ""); // 하단 정보 지우기
    }

    /// <summary>
    /// Rarity에 따른 등급 텍스트 반환 (S, A, B, C)
    /// </summary>
    private string GetRarityGradeText(ItemRarity rarity)
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

    /// <summary>
    /// Rarity에 따른 색상 반환
    /// </summary>
    private Color GetRarityColor(ItemRarity rarity)
    {
        int index = rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Rare => 1,
            ItemRarity.Special => 2,
            ItemRarity.Legendary => 3,
            _ => 0
        };

        if (rarityColors != null && index < rarityColors.Length)
            return rarityColors[index];
        
        return Color.white;
    }
}
