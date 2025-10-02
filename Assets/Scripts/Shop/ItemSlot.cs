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

    private ItemData effect;
    private ShopUI shop;            // 콜백용
    private int stock;              // IsStackable이면 초기 n, 아니면 1
    private int maxStock;

    public void Bind(ShopUI shopUI, ItemData eff)
    {
        shop = shopUI;
        effect = eff;

        if (iconImage) iconImage.sprite = eff.Icon;
        if (nameText) nameText.text = eff.DisplayName;

        stock = eff.IsStackable ? Mathf.Max(1, eff.InitialStock) : 1;
        maxStock = stock;

        // 버튼 리스너 갱신
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => shop.OnClickBuy(effect, this));
        }

        Refresh();
    }

    public void OnPurchased()
    {
        // 비스택형도 구매 직후 슬롯 닫히도록 0 처리
        if (effect.IsStackable) stock = Mathf.Max(0, stock - 1);
        else if (effect.OnePerShopIfNotStackable) stock = 0;
        else if (effect.Price == int.MaxValue) stock = 0; // 가격표 떼기 조건이면 0
        
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shop == null || effect == null) return;

        int displayPrice = effect.GetDisplayPrice();
        bool priceHidden = (displayPrice == int.MaxValue);
        string priceLine = priceHidden ? "" : $"\n가격: {displayPrice} G";

        shop.SendMessage("ShowInfo", $"{effect.DisplayName}\n{effect.Description}{priceLine}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shop == null) return;
        shop.SendMessage("ShowInfo", ""); // 하단 정보 지우기
    }
}
