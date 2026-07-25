using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class RequestCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image thumbnail;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI reward;
    [SerializeField] private TextMeshProUGUI progress; //텍스트 말고 다른 걸로 대체될 수도

    [Space(10)]
    [Header("State Effects")]
    [SerializeField] private Image bg_image;
    [SerializeField] private Sprite bg_white;
    [SerializeField] private Sprite bg_yellow;
    [SerializeField] private GameObject complete_panel;
    [SerializeField] private GameObject failed_panel;
    [SerializeField] private TextMeshProUGUI stateText;

    private RequestInstance RI;
    private RequestUI owner;

    public void Set(RequestInstance request, RequestUI ownerUI)
    {
        //퀘스트 내용에 따라 UI를 set 하는 함수

        RI = request;
        owner = ownerUI;

        title.text = request.GetTitleText();
        reward.text = "보상 - " + request.GetRewardText();
        progress.text = request.GetProgressText();

        switch (request.State)
        {
            case RequestState.InProgress:
                bg_image.sprite = bg_white;
                bg_image.color = Color.white;
                complete_panel.SetActive(false);
                failed_panel.SetActive(false);
                stateText.text = "진행 중";
                break;
            case RequestState.Complete:
                bg_image.sprite = bg_yellow;
                bg_image.color = Color.white;
                complete_panel.SetActive(false);
                failed_panel.SetActive(false);
                stateText.text = "수령 대기 중";
                break;
            case RequestState.Granted:
                bg_image.sprite = bg_white;
                bg_image.color = Color.yellow;
                complete_panel.SetActive(true);
                failed_panel.SetActive(false);
                stateText.text = "수령 완료";
                break;
            case RequestState.Fail:
                bg_image.sprite = bg_white;
                bg_image.color = Color.gray;
                complete_panel.SetActive(false);
                failed_panel.SetActive(true);
                stateText.text = "실패";
                break;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner.SetPopupRequestInfo(RI);
        //FindAnyObjectByType<RequestUI>().OnClickShowPopup();
    }
}
