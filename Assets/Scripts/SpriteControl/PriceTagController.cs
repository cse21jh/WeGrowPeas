using TMPro;
using UnityEngine;

public class PriceTagController : MonoBehaviour
{
    [SerializeField] private TextMeshPro priceText;


    public void SetPrice(int price)
    {
        Debug.Log(price);

        if (priceText != null)
        {
            priceText.text = price.ToString("D") + "$"; // Format to 2 decimal places
        }
    }
}
