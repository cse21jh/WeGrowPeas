using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPopupController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI questDescription;

    [SerializeField] private Button getButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private RequestUI request;



    public void SetItemInfo(RequestUI request)
    {
        this.request = request;

        if (getButton != null)
        {
            getButton.onClick.RemoveAllListeners();
            //getButton.onClick.AddListener(() => shop.OnClickBuy(itemData, slot));
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => request.OnClickHidePopup());
        }
    }



}
