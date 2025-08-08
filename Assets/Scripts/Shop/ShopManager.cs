using NUnit.Framework.Interfaces;
using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    private int gold = 10;
    public bool TryBuy(ItemData data)
    {
        if (gold < data.price)
        {
            Debug.Log("°ñµå ºÎÁ·");
            return false;
        }

        gold -= data.price;
        return true;
    }
}
