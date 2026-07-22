using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SignPostController : MonoBehaviour
{
    Animator anim;
    [SerializeField] private Image waveImage;
    [SerializeField] private List<Sprite> waveSprite;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("Animator component not found on PriceSignController GameObject.");
        }
    }

    public void SetSignPost(int waveType)
    {
        anim.SetBool("isShow", true);
        //Debug.Log(price);

        if (waveImage != null)
        {
            waveImage.sprite = waveSprite[waveType]; // Format to 2 decimal places
        }
    }

    public void HideSignPost()
    {
        anim.SetBool("isShow", false);
    }
}
