using DG.Tweening;
using UnityEngine;

public class UIFadeController : MonoBehaviour
{
    [SerializeField] private RectTransform[] fadeRects;
    [SerializeField] private Vector2[] fadeDirections;

    [SerializeField] private Ease fadeEase;



    public void FadeOut()
    {
        for (int i = 0; i < fadeRects.Length; i++)
        {
            fadeRects[i].DOAnchorPos(fadeRects[i].anchoredPosition + fadeDirections[i], 1f).SetEase(fadeEase);
        }
    }







}
