using TMPro;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{


    
    [SerializeField] private TextMeshProUGUI CoinUI;

    // 저장 필요
    [SerializeField] private int gold = 0;

    private int peaSellCount = 0;
    private int peanutSellCount = 0;
    public int PeaSellCount => peaSellCount;
    public int PeanutSellCount => peanutSellCount;

    private int totalGold = 0;
    private int consumeGold = 0;
    private int earnedGoldToday = 0;
    public int TotalGold => totalGold;
    public int ConsumeGold => consumeGold;
    /// <summary>오늘 번 골드. <see cref="PushEarnedGold"/>로 그래프에 밀어넣으면 0으로 초기화된다.</summary>
    public int EarnedGoldToday => earnedGoldToday;

    private void Awake()
    {
        EconomyFeedbackController.EnsureExists(CoinUI);
    }

    private void Start()
    {
        UpdateCoinUI(gold);
    }

    public void InitEconomyManager()
    {
        peaSellCount = 0;
        peanutSellCount = 0;
        totalGold = 0;
        consumeGold = 0;
        earnedGoldToday = 0;
    }

    public bool HasGold(int amount) => gold >= amount;

    public void SpendGold(int amount, GoldFeedbackReason reason = GoldFeedbackReason.Other)
    {
        gold -= amount;
        consumeGold += amount;
        UpdateCoinUI(gold);
        if (amount > 0)
        {
            GameEvents.RaiseGoldFeedback(
                GoldFeedbackData.HudOnly(-amount, gold, reason));
        }
        Debug.Log($"골드 {amount} 사용 → 남은 {gold}");
    }

    public void AddGold(int amount)
    {
        gold += amount;
        totalGold += amount;
        earnedGoldToday += amount;
        UpdateCoinUI(gold);
        Debug.Log($"골드 {amount} 획득 → 합계 {gold}");
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

    public int GetGold()
    {
        return gold;
    }

    /// <summary>골드/판매 집계를 저장 데이터에 담는다. <see cref="LoadEconomyManager"/>와 짝.</summary>
    public void SaveEconomyManager(EconomySave save)
    {
        save.gold = gold;
        save.sellCount[0] = peaSellCount;
        save.sellCount[1] = peanutSellCount;
        save.totalGold = totalGold;
        save.consumeGold = consumeGold;
    }

    public void LoadEconomyManager(EconomySave saveData)
    {
        gold = saveData.gold;
        UpdateCoinUI(gold);
        peaSellCount = saveData.sellCount[0];
        peanutSellCount = saveData.sellCount[1];
        totalGold = saveData.totalGold;
        consumeGold = saveData.consumeGold;
    }

    public void AddSellCount(string plantName)
    {
        if(plantName == "완두콩")
        {
            peaSellCount++;
            CodexProgress.Discover(CodexProgress.Category.Plant, plantName);
            CodexProgress.AddSold(true);
            return;
        }
        else if(plantName == "땅콩")
        {
            peanutSellCount++;
            CodexProgress.Discover(CodexProgress.Category.Plant, plantName);
            CodexProgress.AddSold(false);
            return;
        }
    }

    public void PushEarnedGold()
    {
        PlayerRecordForGraph.SetEG(earnedGoldToday);
        earnedGoldToday = 0;
    }
}
