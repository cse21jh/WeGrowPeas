using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

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

    private ItemData effect;
    private ShopUI shop;            // 콜백용
    private int stock; // IsStackable이면 초기 n, 아니면 1
    private int maxStock;

    public void Bind(ShopUI shopUI, ItemData eff)
    {
        shop = shopUI;
        effect = eff;

        iconImage.sprite = eff.Icon;
        nameText.text = eff.DisplayName;
        priceText.text = $"{eff.GetDisplayPrice()} G";

        stock = eff.IsStackable ? Mathf.Max(1, eff.InitialStock) : 1;
        maxStock = stock;
        Refresh();

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => shop.OnClickBuy(effect, this));
    }

    public void OnPurchased()
    {
        if (effect.IsStackable)
            stock = Mathf.Max(0, stock - 1);

        Refresh();
    }

    private void Refresh()
    {
        if (countText != null)
        {
            if (effect.IsStackable) { countText.gameObject.SetActive(true); countText.text = $"{stock}/{maxStock}"; }
            else { countText.gameObject.SetActive(false); }
        }
        buyButton.interactable = stock > 0;

        priceText.text = $"{effect.GetDisplayPrice()} G";

        leftAnim.SetBool("isOpen", stock > 0);
        rightAnim.SetBool("isOpen", stock > 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shop == null || effect == null) return;
        shop.SendMessage("ShowInfo", $"{effect.DisplayName}\n{effect.Description}\n가격: {effect.GetDisplayPrice()} G");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shop == null) return;
        shop.SendMessage("ShowInfo", ""); // 하단 정보 지우기
    }
}