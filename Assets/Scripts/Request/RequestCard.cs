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

    public void Set(RequestInstance request)
    {
        //퀘스트 내용에 따라 UI를 set 하는 함수

        title.text = request.Data.requestDefinition;
        reward.text = "보상 - " + request.Data.reward + "골드";
        progress.text = "0/1000";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        FindAnyObjectByType<RequestUI>().OnClickShowPopup();        // 얘도 RequestUI.cs에서 Bind 해주면 거기에 따라서 다시 바꿔야 함
        Debug.Log("퀘스트 아이템을 클릭했습니다. 그러나 아무 일도 일어나지 않음......");
    }
}
