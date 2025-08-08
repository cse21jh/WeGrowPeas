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

    private ItemData itemData;
    private int stock;              // 현재 재고
    private bool oneTimeOnly;       // 이번 상점 세션에서 1회만 구매 가능한가?
    private ShopUI shop;            // 콜백용

    public void Init(ShopUI shopUI, ItemData data, bool oneTime, int initialStock)
    {
        shop = shopUI;
        itemData = data;
        oneTimeOnly = oneTime;

        stock = Mathf.Max(1, initialStock);
        if (!itemData.isStackable) stock = 1;

        iconImage.sprite = itemData.icon;
        nameText.text = itemData.itemName;
        priceText.text = $"{itemData.price} G";

        Refresh();
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnClickBuy);
    }

    public void Refresh()
    {
        // 스택형이 아니면 1회 구매 후 비활성 예정
        bool canInteract = stock > 0;
        buyButton.interactable = canInteract;

        if (countText != null)
        {
            if (itemData.isStackable)
            {
                countText.gameObject.SetActive(true);
                countText.text = $"x{stock}";
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
    }

    public void OnClickBuy()
    {
        if (stock <= 0) return;

        // ShopUI에 구매 시도 요청
        bool ok = shop.TryBuy(itemData, oneTimeOnly);
        if (!ok)
        {
            // 실패 시 ShopUI가 에러 문구 표시함
            return;
        }

        // 성공 시 재고 차감
        stock--;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shop == null || itemData == null) return;

        string info = $"{itemData.itemName}\n{itemData.description}\n가격: {itemData.price} G";
        shop.ShowInfo(info);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shop == null) return;
        shop.ClearInfo();
    }
}