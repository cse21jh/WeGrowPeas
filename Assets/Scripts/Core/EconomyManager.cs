using TMPro;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [SerializeField] private int gold = 5000;
    [SerializeField] private TextMeshProUGUI CoinUI;

    public bool HasGold(int amount) => gold >= amount;

    public void SpendGold(int amount)
    {
        gold -= amount;
        UpdateCoinUI(gold);
        Debug.Log($"°ñµå {amount} »ç¿ë ¡æ ³²Àº {gold}");
    }

    public void AddGold(int amount)
    {
        gold += amount;
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
}