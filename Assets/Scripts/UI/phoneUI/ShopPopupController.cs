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
            itemDescription.text = itemData.Description;
        if (iconImage != null)
            iconImage.sprite = itemData.Icon;
        this.itemData = itemData;
        this.shop = shop;
        this.slot = slot;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => shop.OnClickBuy(itemData, slot));
        }

        if( closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => shop.OnClickHidePopup());
        }
    }





}
