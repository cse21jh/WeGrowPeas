using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보상 수령 팝업(Popup_GetMoney). 완료했지만 아직 받지 않은 퀘스트 보상을 한 번에 받는다.
///
/// 하나씩 받는 것은 퀘스트 탭(<see cref="ExpandableQuestTab"/>)이 맡고,
/// 여기서는 "지금 받을 수 있는 것 전부"만 처리한다. 실제 지급은 같은
/// <see cref="RequestInstance.GrantRewardOnce"/>를 부르므로 규칙이 갈라지지 않는다.
/// </summary>
public class RewardClaimPopup : MonoBehaviour
{
    [Header("표시")]
    [Tooltip("\"수령 가능한 보상 7개 / 5000G\"")]
    [SerializeField] private TMP_Text summaryText;

    [Tooltip("받을 게 없을 때 버튼을 흐리게 할지")]
    [SerializeField] private bool dimButtonWhenEmpty = true;

    [Header("버튼")]
    [SerializeField] private Button getButton;

    private void OnEnable()
    {
        var manager = RequestManager.Instance;
        if (manager != null)
        {
            manager.OnProgressUpdated += Refresh;
            manager.OnBoardUpdated += Refresh;
        }

        if (getButton != null)
        {
            getButton.onClick.RemoveListener(ClaimAll);
            getButton.onClick.AddListener(ClaimAll);
        }

        Refresh();
    }

    private void OnDisable()
    {
        var manager = RequestManager.Instance;
        if (manager != null)
        {
            manager.OnProgressUpdated -= Refresh;
            manager.OnBoardUpdated -= Refresh;
        }

        if (getButton != null) getButton.onClick.RemoveListener(ClaimAll);
    }

    public void Open()
    {
        if (gameObject.activeSelf) Refresh();
        else gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    /// <summary>수령 가능한 보상 개수와 골드 합계를 다시 센다.</summary>
    public void Refresh()
    {
        List<RequestInstance> claimable = GetClaimable();
        int gold = SumGold(claimable);

        if (summaryText != null)
            summaryText.text = claimable.Count > 0
                ? $"수령 가능한 보상 {claimable.Count}개 / {gold}G"
                : "수령 가능한 보상이 없습니다";

        if (getButton != null)
        {
            getButton.interactable = claimable.Count > 0;

            if (dimButtonWhenEmpty)
            {
                var group = getButton.GetComponent<CanvasGroup>();
                if (group == null) group = getButton.gameObject.AddComponent<CanvasGroup>();
                group.alpha = claimable.Count > 0 ? 1f : 0.5f;
            }
        }
    }

    /// <summary>받을 수 있는 보상을 전부 받는다. (전체 수령 버튼)</summary>
    public void ClaimAll()
    {
        List<RequestInstance> claimable = GetClaimable();

        if (claimable.Count == 0)
        {
            SoundManager.Instance?.PlayEffect("WrongSelect");
            PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
            {
                title = "알림",
                message = "지금 받을 수 있는 보상이 없습니다.",
                duration = 2f
            });
            return;
        }

        int gold = SumGold(claimable);

        foreach (RequestInstance request in claimable)
            request.GrantRewardOnce(); // 지급 규칙은 하나씩 받을 때와 같다

        PhoneManager.Instance?.PhoneTouchEffect();
        PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
        {
            title = "보상 수령",
            message = $"보상 {claimable.Count}개를 받았습니다. (+{gold}G)",
            duration = 2f
        });

        Refresh();
    }

    // ── 집계 ──────────────────────────────────────────────────────────────────

    private static List<RequestInstance> GetClaimable()
    {
        var list = new List<RequestInstance>();

        var manager = RequestManager.Instance;
        if (manager == null || manager.ActiveReq == null) return list;

        foreach (RequestInstance request in manager.ActiveReq)
            if (request != null && request.CanAcceptReward)
                list.Add(request);

        return list;
    }

    /// <summary>골드 보상만 합산한다. 유전자 등 다른 보상은 개수에만 들어간다.</summary>
    private static int SumGold(List<RequestInstance> requests)
    {
        int total = 0;

        foreach (RequestInstance request in requests)
        {
            if (request?.Data?.rewards == null) continue;

            foreach (var reward in request.Data.rewards)
                if (reward.type == RewardType.Gold)
                    total += reward.amount;
        }

        return total;
    }
}
