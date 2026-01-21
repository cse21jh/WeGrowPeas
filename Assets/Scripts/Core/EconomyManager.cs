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

    public void SpendGold(int amount)
    {
        gold -= amount;
        consumeGold += amount;
        UpdateCoinUI(gold);
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

    public string ToAbbreviatedString(int number)
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
    }

    public int GetGold()
    {
        return gold;
    }

    public void LoadEconomyManager(SaveData saveData)
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
            return;
        }
        else if(plantName == "땅콩")
        {
            peanutSellCount++;
            return;
        }
    }

    public void PushEarnedGold()
    {
        PlayerRecordForGraph.SetEG(earnedGoldToday);
        earnedGoldToday = 0;
    }
}
