using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RequestUI : MonoBehaviour
{
    [SerializeField] private Transform questItemContentParent;
    [SerializeField] private GameObject questItem;
    [SerializeField] private TextMeshProUGUI nullText;

    [Header("Overall Progress")]
    [SerializeField] private Slider overallProgressBarFill;
    [SerializeField] private TextMeshProUGUI overallProgressText;

    private RequestInstance currentRI; // 팝업을 위해, 지금 클릭된 퀘스트에 대한 정보를 저장.
    public RequestInstance CurrentRI => currentRI;

    private void OnEnable()
    {
        if (RequestManager.Instance != null)
        {
            RequestManager.Instance.OnBoardUpdated += Refresh;
            RequestManager.Instance.OnProgressUpdated += UpdateAllQuestProgress;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (RequestManager.Instance != null)
        {
            RequestManager.Instance.OnBoardUpdated -= Refresh;
            RequestManager.Instance.OnProgressUpdated -= UpdateAllQuestProgress;
        }
    }

    public void Refresh()
    {
        //if (RequestManager.Instance == null || questItemContentParent == null || questItem == null)
        //return;

        ClearRequestContent();

        if (RequestManager.Instance == null) return;

        var reqs = RequestManager.Instance.ActiveReq;
        int spawnCount = reqs.Count;

        if (spawnCount == 0)
        {
            nullText.gameObject.SetActive(true);
            return;
        }
        else
        {
            nullText.gameObject.SetActive(false);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            var item = Instantiate(questItem, questItemContentParent);
            var card = item.GetComponent<ExpandableQuestTab>();

            card.Set(reqs[i], this);
        }

        UpdateOverallQuestProgress();
    }

    public void UpdateAllQuestProgress()
    {
        if (questItemContentParent == null) return;

        foreach (Transform child in questItemContentParent)
        {
            var tab = child.GetComponent<ExpandableQuestTab>();
            if (tab != null)
            {
                tab.UpdateProgress();
            }
        }

        UpdateOverallQuestProgress();
    }

    private void UpdateOverallQuestProgress()
    {
        if (RequestManager.Instance == null) return;

        var reqs = RequestManager.Instance.ActiveReq;
        if (reqs == null || reqs.Count == 0)
        {
            if (overallProgressBarFill != null) overallProgressBarFill.value = 0f;
            if (overallProgressText != null) overallProgressText.text = "0%";
            return;
        }

        int completedCount = 0;
        foreach (var req in reqs)
        {
            if (req.IsCompleted)
            {
                completedCount++;
            }
        }

        float ratio = (float)completedCount / reqs.Count;
        
        if (overallProgressBarFill != null) 
            overallProgressBarFill.value = ratio;
            
        if (overallProgressText != null) 
            overallProgressText.text = $"{Mathf.RoundToInt(ratio * 100)}%";
    }

    public void OnClickReceiveAllRewards()
    {
        if (RequestManager.Instance == null) return;

        var reqs = RequestManager.Instance.ActiveReq;
        bool anyReceived = false;

        foreach (var req in reqs)
        {
            if (req.CanAcceptReward)
            {
                req.GrantRewardOnce();
                anyReceived = true;
            }
        }

        if (!anyReceived)
        {
            SoundManager.Instance.PlayEffect("WrongSelect");
            // Show floating text
            PhoneNotificationBus.OnShow?.Invoke(
                new PhoneNotificationData
                {
                    title = "알림",
                    message = "아직 퀘스트가 완료되지 않았습니다.",
                    duration = 2f
                }
            );
        }
        else
        {
            UpdateAllQuestProgress();
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
