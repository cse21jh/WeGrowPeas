using System;
using UnityEngine;
using UnityEngine.UI;

public class BreedTimerController : MonoBehaviour
{
    [SerializeField] private Slider breedTimerSlider;

    [SerializeField] private float smoothTime = 0.1f;

    [SerializeField] private float targetFillAmount = 1f;

    public void SetFill(float fillAmount)
    {
        targetFillAmount = fillAmount;
        //breedTimerSlider.value = Mathf.Lerp(fillAmount, breedTimerSlider.value, smoothTime);
    }

    private void FixedUpdate()
    {
        if (breedTimerSlider.value != targetFillAmount)
        {
            breedTimerSlider.value = Mathf.Lerp(breedTimerSlider.value, targetFillAmount, smoothTime);
        }
    }

}
