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

    public void StopTimer()
    {
        if (countdownRoutine != null)
        {
            textTimer.text = $"{maxBreedingTime}s";
            breedTimerController.SetFill(1f);
            textTimer.color = Color.black;
            StopCoroutine(countdownRoutine);
        }
    }

    private IEnumerator BreedingCountdown()
    {
        int timeLeft = maxBreedingTime;
        textTimer.color = Color.white;

        while (timeLeft >= 0)
        {
            if (timeLeft <= 10) textTimer.color = Color.red;

            textTimer.text = $"{timeLeft}s";
            yield return new WaitForSeconds(1f);
            timeLeft--;
            breedTimerController.SetFill(timeLeft / (float)maxBreedingTime);
        }
    }

}
