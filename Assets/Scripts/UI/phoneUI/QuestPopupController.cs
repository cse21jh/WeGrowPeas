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
    [SerializeField] private TextMeshProUGUI reward;

    private RequestInstance currentRequest;


    public void SetItemInfo(RequestUI request)
    {
        this.request = request;
        currentRequest = request.CurrentRI;
        ShowPopupDetail();

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

    private void ShowPopupDetail()
    {
        questName.text = currentRequest.Data.requestTitle;
        questDescription.text = currentRequest.Data.requestDescription;
        reward.text = "" + currentRequest.Data.reward + 'G';
    }



}
