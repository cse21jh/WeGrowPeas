using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RequestUI : MonoBehaviour
{
    [SerializeField] private Transform questItemContentParent;
    [SerializeField] private GameObject questItem;
    [SerializeField] private RectTransform popupParent;  // 팝업 UI 부모

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
        Debug.Log("퀘스트 리프레시");

        //if (RequestManager.Instance == null || questItemContentParent == null || questItem == null)
        //return;

        ClearRequestContent();

        var reqs = RequestManager.Instance.ActiveReq;
        int spawnCount = reqs.Count;

        Debug.Log(spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            Debug.Log("프리팹 붙이기");
            var item = Instantiate(questItem, questItemContentParent);
            var card = item.GetComponent<RequestCard>();

            card.Set(reqs[i]);
        }
    }

    private void ClearRequestContent()
    {
        for (int i = questItemContentParent.childCount - 1; i >= 0; i--)
            Destroy(questItemContentParent.GetChild(i).gameObject);
    }
}
