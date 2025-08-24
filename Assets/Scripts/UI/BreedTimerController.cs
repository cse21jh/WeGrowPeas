using UnityEngine;
using UnityEngine.UI;

public class BreedTimerController : MonoBehaviour
{
    [SerializeField] private Image timerFillImage;




    private void SetFill(float fillAmount)
    {
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = fillAmount;
        }
    }
}
