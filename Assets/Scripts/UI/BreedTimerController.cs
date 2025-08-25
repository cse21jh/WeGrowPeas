using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BreedTimerController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Slider breedTimerSlider;

    [SerializeField] private WaveTextBoxController waveTextBox;

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





    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 버튼 위에 올려졌을 때
        waveTextBox.ShowWaveTextBox();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 버튼을 떠났을 때
        waveTextBox.HideWaveTextBox();
    }



}
