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

    public bool isLocked = false;

    public void SetFill(float fillAmount)
    {
        targetFillAmount = fillAmount;
        //breedTimerSlider.value = Mathf.Lerp(fillAmount, breedTimerSlider.value, smoothTime);
    }

    public void SetFillImmediately(float fillAmount)
    {
        breedTimerSlider.value = fillAmount;
    }

    private void FixedUpdate()
    {
        if (breedTimerSlider.value != targetFillAmount)
        {
            breedTimerSlider.value = Mathf.Lerp(breedTimerSlider.value, targetFillAmount, smoothTime);
        }
    }

    public void LockText()
    {
        isLocked = true;
        waveTextBox.ShowWaveTextBox();
    }

    public void ReleaseText()
    {
        isLocked = false;
        waveTextBox.HideWaveTextBox();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 버튼 위에 올려졌을 때
        if (!isLocked)
        {
            if (GameManager.Instance != null && GameManager.Instance.enemyController != null)
            {
                GameManager.Instance.enemyController.ShowNextWaveText();
            }
            waveTextBox.gameObject.SetActive(true);
            waveTextBox.ShowWaveTextBox();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 버튼을 떠났을 때
        if (!isLocked)
        {
            waveTextBox.HideWaveTextBox();
        }
    }



}
