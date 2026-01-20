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
            if (GameManager.Instance != null)
                maxBreedingTime = (int)GameManager.Instance.phoneManager.GetMaxPhoneTimer();
            StopCoroutine(countdownRoutine);
        }


        if (GameManager.Instance != null)
            maxBreedingTime = (int)GameManager.Instance.phoneManager.GetMaxPhoneTimer();
        else
            maxBreedingTime = 30;

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
        if(GameManager.Instance != null)
            textTimer.text = $"{(int)GameManager.Instance.phoneManager.GetMaxPhoneTimer()}s";
        else
            textTimer.text = $"30s";
        breedTimerController.SetFill(0f);
        textTimer.color = Color.black;
    }

    private IEnumerator BreedingCountdown()
    {
        textTimer.color = Color.white;
        breedTimerController.SetFillImmediately(1f);

        while (true)
        {
            if (GameManager.Instance.GetGameIsStopped())
            {
                yield return null;
                continue;
            }

            // Grid의 실제 breedTimer와 동기화
            if (GameManager.Instance?.grid != null)
            {
                float currentTimer = GameManager.Instance.grid.GetBreedTimer();
                float currentMaxTimer = GameManager.Instance.grid.GetMaxBreedTimer();
                
                if (currentTimer <= 0)
                {
                    break;
                }

                int timeLeft = Mathf.CeilToInt(currentTimer);
                maxBreedingTime = Mathf.CeilToInt(currentMaxTimer);

                if (timeLeft <= 10) textTimer.color = Color.red;
                else textTimer.color = Color.white;

                textTimer.text = $"{timeLeft}s";
                
                // 프로그레스바 계산: 현재 시간 / 최대 시간
                float fillAmount = currentTimer / currentMaxTimer;
                breedTimerController.SetFill(fillAmount);
            }
            else
            {
                break;
            }

            yield return new WaitForSeconds(1f); // 더 부드러운 업데이트를 위해 0.1초마다
        }
    }

    private IEnumerator PhoneCountdown()
    {
        int timeLeft = maxBreedingTime;
        textTimer.color = Color.white;
        breedTimerController.SetFillImmediately(0f);

        while (timeLeft >= 0)
        {

            if (GameManager.Instance == null)
            {
                yield return null;
                continue;
            }

            if (GameManager.Instance.GetGameIsStopped())
            {
                yield return null;
                continue;
            }

            if (timeLeft <= 10) textTimer.color = Color.red;

            textTimer.text = $"{timeLeft}s";
            yield return new WaitForSeconds(1f);
            timeLeft--;
            breedTimerController.SetFill(1 - (timeLeft / (float)maxBreedingTime));
        }
    }
}
