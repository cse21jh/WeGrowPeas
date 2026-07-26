using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ThiefEffectController : MonoBehaviour
{
    [SerializeField] private Animator line;

    [SerializeField] private CanvasGroup[] Images;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float totalDuration = 3f;
    [SerializeField] private Ease fadeEase = Ease.Linear;

    public void PlayLineAnimation()
    {
        line.SetTrigger("Play");

        Sequence fadeSequence = DOTween.Sequence();
        foreach (CanvasGroup image in Images)
        {
            fadeSequence.Join(image.DOFade(1f, fadeDuration).SetEase(fadeEase));
        }
        fadeSequence.AppendInterval(totalDuration - fadeDuration);

        fadeSequence.OnComplete(() =>
        {
            foreach (CanvasGroup image in Images)
            {
                image.DOFade(0f, fadeDuration).SetEase(fadeEase);
            }
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayLineAnimation();
        }
    }
}
