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
    [SerializeField] private TMP_Text countText; // �������� ���� ���
    [SerializeField] private Button buyButton;

    private ItemData itemData;
    private int stock;              // ���� ���
    private bool oneTimeOnly;       // �̹� ���� ���ǿ��� 1ȸ�� ���� �����Ѱ�?
    private ShopUI shop;            // �ݹ��

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
        // �������� �ƴϸ� 1ȸ ���� �� ��Ȱ�� ����
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

        // ShopUI�� ���� �õ� ��û
        bool ok = shop.TryBuy(itemData, oneTimeOnly);
        if (!ok)
        {
            // ���� �� ShopUI�� ���� ���� ǥ����
            return;
        }

        // ���� �� ��� ����
        stock--;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shop == null || itemData == null) return;

        string info = $"{itemData.itemName}\n{itemData.description}\n����: {itemData.price} G";
        shop.ShowInfo(info);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shop == null) return;
        shop.ClearInfo();
    }
}