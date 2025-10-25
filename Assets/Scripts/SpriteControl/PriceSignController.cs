using UnityEngine;
using TMPro;

public class PriceSignController : MonoBehaviour
{
    Animator anim;
    [SerializeField] private TextMeshPro priceText;

    [SerializeField] private bool tasteDisplayMode = true; // true: 숫자, false: 별 아이콘
    [SerializeField] private GameObject tasteDisplay_text;
    [SerializeField] private GameObject tasteDisplay_icon;

    [Header("숫자로 맛 표기")]
    [SerializeField] private TextMeshPro tasteText;

    [Header("별 아이콘으로 맛 표기")]
    [SerializeField] private GameObject[] tasteStars; // 맛을 별 아이콘으로 표시


    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("Animator component not found on PriceSignController GameObject.");
        }

        if (tasteDisplayMode)
        {
            tasteDisplay_text.SetActive(true);
            tasteDisplay_icon.SetActive(false);
        }
        else
        {
            tasteDisplay_text.SetActive(false);
            tasteDisplay_icon.SetActive(true);
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

    public void ShowTaste(int taste)
    {
        if (tasteDisplayMode)
        {
            tasteText.text = taste.ToString() + "/6";
        }
        else
        {
            for(int i = 0; i < tasteStars.Length; i++)
            {
                tasteStars[i].SetActive(i < taste);
            }
        }
    }
}
