using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textTimer;
    [SerializeField] private int breedingTime = 40;
    private Coroutine countdownRoutine;

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
            StopCoroutine(countdownRoutine);

        countdownRoutine = StartCoroutine(BreedingCountdown());
    }

    public void StopTimer()
    {
        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);
    }

    private IEnumerator BreedingCountdown()
    {
        int timeLeft = breedingTime;
        textTimer.color = Color.black;

        while (timeLeft >= 0)
        {
            if (timeLeft <= 10) textTimer.color = Color.red;

            textTimer.text = $"<sprite=7> {timeLeft}";
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }
    }
}
