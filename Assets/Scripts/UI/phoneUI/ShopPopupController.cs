using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPopupController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI itemDescription;

    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private ItemData itemData;
    [SerializeField] private ShopUI shop;
    [SerializeField] private ItemSlot slot;

    [Header("Grade Tag UI")]
    [SerializeField] private Image gradeTagImage;
    [SerializeField] private TMP_Text gradeTagText;

    [Header("Rarity Colors")]
    [Tooltip("등급별 색상 (순서: Common, Rare, Special, Legendary)")]
    [SerializeField] private Color[] rarityColors = new Color[4];



    public void SetItemInfo(ItemData itemData, ShopUI shop, ItemSlot slot)
    {
        Debug.Log("Setting popup for item: " + itemData.DisplayName + " " + itemData.Description);
        if (itemName != null)
        {
            itemName.text = itemData.DisplayName;
        }
        if (iconImage != null)
        {
            iconImage.sprite = itemData.Icon;
        }
        if (itemDescription != null)
        {
            string description = itemData.Description;
            
            // 구매 횟수 정보 추가
            if (itemData.MaxPurchaseCount >= 0)
            {
                int currentCount = itemData.GetTotalPurchaseCount();
                int maxCount = itemData.MaxPurchaseCount;
                description += $"\n\n구매 횟수: {currentCount}/{maxCount}";
            }
            
            itemDescription.text = description;
        }

        // Grade Tag 설정
        Color rarityColor = GetRarityColor(itemData.Rarity);
        string gradeText = GetRarityGradeText(itemData.Rarity);

        if (gradeTagImage != null)
        {
            if (itemData.GradeTagImage != null)
            {
                gradeTagImage.sprite = itemData.GradeTagImage;
                gradeTagImage.gameObject.SetActive(true);
            }
            else
            {
                gradeTagImage.gameObject.SetActive(false);
            }
            gradeTagImage.color = rarityColor;
        }

        if (gradeTagText != null)
        {
            if (!string.IsNullOrEmpty(itemData.GradeTagText))
            {
                gradeTagText.text = itemData.GradeTagText;
                gradeTagText.gameObject.SetActive(true);
            }
            else if (!string.IsNullOrEmpty(gradeText))
            {
                gradeTagText.text = gradeText;
                gradeTagText.gameObject.SetActive(true);
            }
            else
            {
                gradeTagText.gameObject.SetActive(false);
            }
            gradeTagText.color = rarityColor;
        }
        this.itemData = itemData;
        this.shop = shop;
        this.slot = slot;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => 
            {
                PhoneManager.Instance.PhoneTouchEffect();
                shop.OnClickBuy(itemData, slot);
            });
        }

        if( closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => 
            {
                PhoneManager.Instance.PhoneTouchEffect();
                shop.OnClickHidePopup();
            });
        }
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
