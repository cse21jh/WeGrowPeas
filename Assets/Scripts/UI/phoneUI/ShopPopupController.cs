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



    public void SetItemInfo(ItemData itemData, ShopUI shop, ItemSlot slot)
    {
        Debug.Log("Setting popup for item: " + itemData.DisplayName + " " + itemData.Description);
        if (itemName != null)
            itemName.text = itemData.DisplayName;
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
        if (iconImage != null)
            iconImage.sprite = itemData.Icon;
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





}
