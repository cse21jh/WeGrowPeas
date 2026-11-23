using TMPro;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{


    
    [SerializeField] private TextMeshProUGUI CoinUI;

    [Tooltip("새 게임의 시작 골드.")]
    [SerializeField] private int gold = 0;

    /// <summary>골드와 판매 집계. 이 컴포넌트는 값이 바뀔 때 화면을 맞춘다.</summary>
    public EconomyState State => state ??= new EconomyState(gold);
    private EconomyState state;

    public int PeaSellCount => State.PeaSellCount;
    public int PeanutSellCount => State.PeanutSellCount;
    public int TotalGold => State.TotalGold;
    public int ConsumeGold => State.ConsumeGold;
    /// <summary>오늘 번 골드. <see cref="PushEarnedGold"/>로 그래프에 밀어넣으면 0으로 초기화된다.</summary>
    public int EarnedGoldToday => State.EarnedToday;

    private void Awake()
    {
        EconomyFeedbackController.EnsureExists(CoinUI);
    }

    private void Start()
    {
        UpdateCoinUI(State.Gold);
    }

    public void InitEconomyManager() => State.ResetCounters();

    public bool HasGold(int amount) => State.HasGold(amount);

    public void SpendGold(int amount, GoldFeedbackReason reason = GoldFeedbackReason.Other)
    {
        State.Spend(amount);
        UpdateCoinUI(State.Gold);
        if (amount > 0)
        {
            GameEvents.RaiseGoldFeedback(
                GoldFeedbackData.HudOnly(-amount, State.Gold, reason));
        }
        Debug.Log($"골드 {amount} 사용 → 남은 {State.Gold}");
    }

    public void AddGold(int amount)
    {
        State.Add(amount);
        UpdateCoinUI(State.Gold);
        Debug.Log($"골드 {amount} 획득 → 합계 {State.Gold}");
    }

    public static string ToAbbreviatedString(int number)
    {
        float value = number;

        if (number >= 1000000000) // 10억 이상 (B)
        {
            return (value / 1000000000f).ToString("0.#") + "B";
        }
        if (number >= 1000000) // 100만 이상 (M)
        {
            return (value / 1000000f).ToString("0.#") + "M";
        }
        if (number >= 10000) // 10000 이상 (K)
        {
            return (value / 1000f).ToString("0.#") + "K";
        }

        return number.ToString(); // 1000 미만은 그대로 출력
    } 

    private void UpdateCoinUI(int val)
    {
        CoinUI.text = $"{ToAbbreviatedString(val)}";
        // 골드가 바뀌는 모든 경로(획득/사용/로드)가 여기를 지나므로 여기서 한 번만 알린다.
        GameEvents.RaiseGoldChanged(val);
    }

    public int GetGold() => State.Gold;

    /// <summary>골드/판매 집계를 저장 데이터에 담는다. <see cref="LoadEconomyManager"/>와 짝.</summary>
    public void SaveEconomyManager(EconomySave save) => State.Save(save);

    public void LoadEconomyManager(EconomySave saveData)
    {
        State.Load(saveData);
        UpdateCoinUI(State.Gold);
    }

    public void AddSellCount(PlantState plant)
    {
        if (!plant.IsMovableKind) return; // 완두와 땅콩만 센다
        State.CountSale(plant.Kind);
        if (GameManager.Instance?.Tutorial != null) return;
        CodexProgress.Discover(CodexProgress.Category.Plant, plant.Species);
        CodexProgress.AddSold(plant.Kind == PlantKind.Pea);
    }

    public void PushEarnedGold() => PlayerRecordForGraph.SetEG(State.TakeEarnedToday());
}
