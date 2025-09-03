using UnityEngine;
using TMPro;

public class PriceSignController : MonoBehaviour
{
    Animator anim;
    [SerializeField] private TextMeshPro priceText;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("Animator component not found on PriceSignController GameObject.");
        }
    }

    public void SetPrice(int price)
    {
        anim.SetBool("isShow", true);
        //Debug.Log(price);

        if (priceText != null)
        {
            priceText.text = price.ToString("D") + "$"; // Format to 2 decimal places
        }
    }

    public void HidePrice()
    {
        anim.SetBool("isShow", false);
    }
}
