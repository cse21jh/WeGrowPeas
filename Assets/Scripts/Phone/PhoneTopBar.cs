using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneTopBar : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;

    public void SetTitle(string title)
    {
        if (titleText != null) titleText.text = title;
    }
}
