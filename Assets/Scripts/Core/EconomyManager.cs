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
    public int TotalGold => totalGold;
    public int ConsumeGold => consumeGold;

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
}