using DG.Tweening;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class QuestTabController : MonoBehaviour
{
    [SerializeField] private Image baseFrame;
    [SerializeField] private Image rewardFrame;

    [SerializeField] private Color activeColor;
    [SerializeField] private Color finishedColor;

    [SerializeField] private Image getBtnImage;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite finishedSprite;

    [SerializeField] private RectTransform questTabRect;


    [SerializeField] private float effectDuration = 0.25f;
    [SerializeField] private Vector3 effectStrength = new Vector3(0.1f, 0.1f, 0.1f);
    [SerializeField] private Ease effectEase = Ease.InOutSine;


    public void UpdateStatus(bool isQuestActive)
    {
        Sequence seq = DOTween.Sequence();

        if (!isQuestActive)
        {
            seq.Join(baseFrame.DOColor(new Color(finishedColor.r, finishedColor.g, finishedColor.b, baseFrame.color.a), effectDuration).SetEase(effectEase));
            seq.Join(rewardFrame.DOColor(new Color(finishedColor.r, finishedColor.g, finishedColor.b, 0.5f), effectDuration).SetEase(effectEase));

            if (getBtnImage != null && finishedSprite != null)
            {
                getBtnImage.sprite = finishedSprite;
            }

            seq.Join(questTabRect.DOPunchScale(effectStrength, effectDuration).SetEase(effectEase));
        }
        else
        {

            seq.Join(baseFrame.DOColor(new Color(activeColor.r, activeColor.g, activeColor.b, baseFrame.color.a), effectDuration).SetEase(effectEase));
            seq.Join(rewardFrame.DOColor(new Color(activeColor.r, activeColor.g, activeColor.b, 1f), effectDuration).SetEase(effectEase));

            if (getBtnImage != null && activeSprite != null)
            {
                getBtnImage.sprite = activeSprite;
            }

            seq.Join(questTabRect.DOPunchScale(effectStrength, effectDuration).SetEase(effectEase));
        }
    }

}
