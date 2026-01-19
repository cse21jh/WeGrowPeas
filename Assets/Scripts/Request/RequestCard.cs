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
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner.SetPopupRequestInfo(RI);
        FindAnyObjectByType<RequestUI>().OnClickShowPopup();
    }
}
