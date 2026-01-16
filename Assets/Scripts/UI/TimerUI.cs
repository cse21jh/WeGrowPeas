using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textTimer;
    private int maxBreedingTime;
    private Coroutine countdownRoutine;

    [SerializeField] private BreedTimerController breedTimerController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartBreedingTimer()
    {
        if (countdownRoutine != null)
        {
            maxBreedingTime = (int)GameManager.Instance.grid.GetMaxBreedTimer();
            StopCoroutine(countdownRoutine);
        }

        maxBreedingTime = (int)GameManager.Instance.grid.GetMaxBreedTimer();
        countdownRoutine = StartCoroutine(BreedingCountdown());
    }

    public void StartPhoneTimer()
    {
        if (countdownRoutine != null)
        {
            maxBreedingTime = (int)GameManager.Instance.phoneManager.GetMaxPhoneTimer();
            StopCoroutine(countdownRoutine);
        }

        maxBreedingTime = (int)GameManager.Instance.phoneManager.GetMaxPhoneTimer();
        countdownRoutine = StartCoroutine(PhoneCountdown());
    }

    public void StopTimer()
    {
        if (countdownRoutine != null)
        {
            textTimer.text = $"{maxBreedingTime}s";
            breedTimerController.SetFill(0f);                       
            textTimer.color = Color.black;
            StopCoroutine(countdownRoutine);
        }
    }

    public void StopTimerByPhone()
    {
        if (countdownRoutine != null)
        {
            textTimer.text = $"{maxBreedingTime}s";
            breedTimerController.SetFill(0f);
            textTimer.color = Color.black;
            StopCoroutine(countdownRoutine);
        }
    }

    public void UpdateMaxTimerCount()
    {
        textTimer.text = $"{(int)GameManager.Instance.grid.GetMaxBreedTimer()}s";
        breedTimerController.SetFill(1f);
        textTimer.color = Color.black;
    }

    public void UpdatePhoneMaxTimerCount()
    {
        textTimer.text = $"{(int)GameManager.Instance.phoneManager.GetMaxPhoneTimer()}s";
        breedTimerController.SetFill(0f);
        textTimer.color = Color.black;
    }

    private IEnumerator BreedingCountdown()
    {
        int timeLeft = maxBreedingTime;
        textTimer.color = Color.white;
        breedTimerController.SetFillImmediately(1f);

        while (timeLeft >= 0)
        {
            if (timeLeft <= 10) textTimer.color = Color.red;

            textTimer.text = $"{timeLeft}s";
            yield return new WaitForSeconds(1f);
            timeLeft--;
            breedTimerController.SetFill(timeLeft / (float)maxBreedingTime);
        }
    }

    private IEnumerator PhoneCountdown()
    {
        int timeLeft = maxBreedingTime;
        textTimer.color = Color.white;
        breedTimerController.SetFillImmediately(0f);

        while (timeLeft >= 0)
        {
            if (timeLeft <= 10) textTimer.color = Color.red;

            textTimer.text = $"{timeLeft}s";
            yield return new WaitForSeconds(1f);
            timeLeft--;
            breedTimerController.SetFill(1 - (timeLeft / (float)maxBreedingTime));
        }
    }
}
