using TMPro;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    
    [SerializeField] private TextMeshProUGUI CoinUI;

    // ÀúÀå ÇÊ¿ä
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
        Debug.Log($"°ñµå {amount} »ç¿ë ¡æ ³²Àº {gold}");
    }

    public void AddGold(int amount)
    {
        gold += amount;
        totalGold += amount;
        earnedGoldToday += amount;
        UpdateCoinUI(gold);
        Debug.Log($"°ñµå {amount} È¹µæ ¡æ ÇÕ°è {gold}");
    }

    private void UpdateCoinUI(int val)
    {
        CoinUI.text = $"{val}$";
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
        if(plantName == "¿ÏµÎÄá")
        {
            peaSellCount++;
            return;
        }
        else if(plantName == "¶¥Äá")
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