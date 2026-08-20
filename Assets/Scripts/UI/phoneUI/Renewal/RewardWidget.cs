using UnityEngine;

/// <summary>
/// 홈 화면 보상 위젯의 알람 점. 받아갈 퀘스트 보상이 있으면 켜고, 다 받으면 끈다.
///
/// 개수는 <see cref="RequestManager"/>에서 직접 센다. 퀘스트 앱 아이콘의 알람을 따라가지 않는 이유는,
/// 그쪽이 꺼지는 시점과 어긋날 수 있어서다.
/// </summary>
public class RewardWidget : MonoBehaviour
{
    [Tooltip("받을 보상이 있으면 켜지는 점. 보통 위젯 껍데기(Widget)의 Alarm.")]
    [SerializeField] private GameObject alarmDot;

    private void OnEnable()
    {
        var manager = RequestManager.Instance;
        if (manager != null)
        {
            manager.OnProgressUpdated += Refresh;
            manager.OnBoardUpdated += Refresh;
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
    }

    /// <summary>받을 보상이 남아 있는지 다시 확인한다.</summary>
    public void Refresh()
    {
        if (alarmDot != null) alarmDot.SetActive(HasClaimableReward());
    }

    private static bool HasClaimableReward()
    {
        var manager = RequestManager.Instance;
        if (manager == null || manager.ActiveReq == null) return false;

        foreach (RequestInstance request in manager.ActiveReq)
            if (request != null && request.CanAcceptReward)
                return true;

        return false;
    }
}
