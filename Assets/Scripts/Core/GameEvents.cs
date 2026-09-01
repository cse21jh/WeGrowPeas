using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnSaveGameRequested;

    public static void RequestSaveGame()
    {
        OnSaveGameRequested?.Invoke();
    }

    public static event Action OnBugKilled;
    public static void RaiseBugKilled () => OnBugKilled?.Invoke();

    public static event Action OnPeaBreeded;
    public static void RaisePeaBreeded() => OnPeaBreeded?.Invoke();

    public static event Action OnPeaDied;
    public static void RaisePeaDied() => OnPeaDied?.Invoke();

    public static event Action<ItemData> OnShopBought;
    public static void RaiseShopBought(ItemData item) => OnShopBought?.Invoke(item);

    public static event Action OnDayPassedForRequest; //NoSellPea & remaining day check
    public static void RaiseDayPassedForRequest() => OnDayPassedForRequest?.Invoke();

    public static event Action OnQuestDayPassed;
    public static void RaiseQuestDayPassed() => OnQuestDayPassed?.Invoke();

    /// <summary>
    /// 웨이브 예정표가 새로 정해졌을 때 (EnemyController.SetNextWave).
    /// 하루가 지났다는 알림(OnQuestDayPassed)은 폰이 닫힌 뒤에 오므로,
    /// 예보를 보여주는 UI는 이쪽을 들어야 폰이 열려 있는 동안 최신값이 뜬다.
    /// </summary>
    public static event Action OnWaveScheduleChanged;
    public static void RaiseWaveScheduleChanged() => OnWaveScheduleChanged?.Invoke();

    /// <summary>
    /// 새 일차가 시작됐을 때 (GameManager.StageUpdate). 부를 때 stage는 이미 새 값이다.
    /// OnQuestDayPassed는 stage가 오르기 <b>전</b>에 오므로,
    /// "며칠째" · "N일 전"처럼 일차를 기준으로 계산하는 UI는 이쪽을 들어야 한다.
    /// </summary>
    public static event Action OnDayStarted;
    public static void RaiseDayStarted() => OnDayStarted?.Invoke();

    public static event Action<Plant> OnPeaSold;
    public static void RaisePeaSold(Plant p) => OnPeaSold?.Invoke(p);

    /// <summary>보유 골드가 변할 때(획득/사용/로드). 인자 = 변경 후 보유량.</summary>
    public static event Action<int> OnGoldChanged;
    public static void RaiseGoldChanged(int gold) => OnGoldChanged?.Invoke(gold);

    /// <summary>골드 증감에 대한 시각 피드백을 요청한다.</summary>
    public static event Action<GoldFeedbackData> OnGoldFeedbackRequested;
    public static void RaiseGoldFeedback(GoldFeedbackData data)
    {
        try
        {
            OnGoldFeedbackRequested?.Invoke(data);
        }
        catch (Exception exception)
        {
            // 장식용 피드백 실패가 결제·보상 같은 게임 로직을 중단시키지 않게 격리한다.
            Debug.LogException(exception);
        }
    }

    /// <summary>식물 가치 상승에 대한 시각 피드백을 요청한다.</summary>
    public static event Action<PlantValueFeedbackData> OnPlantValueFeedbackRequested;
    public static void RaisePlantValueFeedback(PlantValueFeedbackData data)
    {
        try
        {
            OnPlantValueFeedbackRequested?.Invoke(data);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    /// <summary>화면에 표시되는 남은 교배 횟수가 바뀔 때 호출한다.</summary>
    public static event Action<int> OnBreedCountChanged;
    public static void RaiseBreedCountChanged(int count)
    {
        try
        {
            OnBreedCountChanged?.Invoke(count);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    /// <summary>교배 횟수 증가에 대한 HUD 피드백을 요청한다.</summary>
    public static event Action<BreedCountFeedbackData> OnBreedCountFeedbackRequested;
    public static void RaiseBreedCountFeedback(BreedCountFeedbackData data)
    {
        try
        {
            OnBreedCountFeedbackRequested?.Invoke(data);
        }
        catch (Exception exception)
        {
            // 장식용 피드백 실패가 아이템 구매 결과를 되돌리지 않게 격리한다.
            Debug.LogException(exception);
        }
    }

    public static event Action OnPlantMoved;
    public static void RaisePlantMoved() => OnPlantMoved?.Invoke();

    public static event Action<int> OnDayEndedWithRemainingBreeds;
    public static void RaiseDayEndedWithRemainingBreeds(int remainingBreeds) => OnDayEndedWithRemainingBreeds?.Invoke(remainingBreeds);

    public static event Action OnPeaDiedByBug;
    public static void RaisePeaDiedByBug() => OnPeaDiedByBug?.Invoke();

    /// <summary>
    /// 모든 구독을 강제로 끊는다. ⚠ 평상시 호출 금지.
    ///
    /// 구독자들은 OnEnable에서 구독하고 OnDisable에서 해제하므로(현재 모든 구독/해제가 1:1 대응)
    /// 별도 청소가 필요 없다. Unity는 (모든 Awake) → (모든 OnEnable) → (모든 Start) 순으로 돌기 때문에,
    /// Start나 그 이후에 이걸 호출하면 이미 걸린 정상 구독까지 전부 지워진다.
    /// (실제로 GameManager.Start에서 호출해 CurseManager의 저주 해제 구독이 날아가,
    ///  단발형 저주가 하루가 지나도 해제되지 않는 버그가 있었다.)
    /// </summary>
    public static void Reset()
    {
        OnSaveGameRequested = null;
        OnBugKilled = null;
        OnPeaBreeded = null;
        OnPeaDied = null;
        OnShopBought = null;
        OnDayPassedForRequest = null;
        OnQuestDayPassed = null;
        OnPeaSold = null;
        OnPlantMoved = null;
        OnDayEndedWithRemainingBreeds = null;
        OnPeaDiedByBug = null;
        OnGoldChanged = null;
        OnGoldFeedbackRequested = null;
        OnPlantValueFeedbackRequested = null;
        OnBreedCountChanged = null;
        OnBreedCountFeedbackRequested = null;
    }
}
