using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [SerializeField] private int gold = 5000;

    public bool HasGold(int amount) => gold >= amount;

    public void SpendGold(int amount)
    {
        gold -= amount;
        Debug.Log($"°ñµå {amount} »ç¿ë ¡æ ³²Àº {gold}");
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"°ñµå {amount} È¹µæ ¡æ ÇÕ°è {gold}");
    }
}