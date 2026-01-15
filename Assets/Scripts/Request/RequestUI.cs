using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RequestUI : MonoBehaviour
{
    [SerializeField] private Transform questItemContentParent;
    [SerializeField] private GameObject questItem;
    [SerializeField] private RectTransform popupParent;  // 팝업 UI 부모

    private RequestInstance currentRI; // 팝업을 위해, 지금 클릭된 퀘스트에 대한 정보를 저장.
    public RequestInstance CurrentRI => currentRI;

    private void OnEnable()
    {
        if (RequestManager.Instance != null)
            RequestManager.Instance.OnBoardUpdated += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (RequestManager.Instance != null)
            RequestManager.Instance.OnBoardUpdated -= Refresh;
    }

    public void OnClickShowPopup()
    {
        popupParent.gameObject.SetActive(true);
        popupParent.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        popupParent.GetComponent<QuestPopupController>().SetItemInfo(this);
    }

    public void OnClickHidePopup()
    {
        popupParent.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
        {
            popupParent.gameObject.SetActive(false);
        });
    }

    public void Refresh()
    {
        //if (RequestManager.Instance == null || questItemContentParent == null || questItem == null)
        //return;

        ClearRequestContent();

        var reqs = RequestManager.Instance.ActiveReq;
        int spawnCount = reqs.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            var item = Instantiate(questItem, questItemContentParent);
            var card = item.GetComponent<RequestCard>();

            card.Set(reqs[i], this);
        }
    }

    private void ClearRequestContent()
    {
        for (int i = questItemContentParent.childCount - 1; i >= 0; i--)
            Destroy(questItemContentParent.GetChild(i).gameObject);
    }

    public void SetPopupRequestInfo(RequestInstance req)
    {
        currentRI = req;
    }
}
