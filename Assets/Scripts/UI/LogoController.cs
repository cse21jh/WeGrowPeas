using UnityEngine;
using DG.Tweening;
using System.Collections;

public class LogoController : MonoBehaviour
{
    [SerializeField] private float initialScale;
    [SerializeField] private float popupDuration = .5f;
    [SerializeField] private float popupScale = 1.1f;
    [SerializeField] private Ease popupEase = Ease.InOutSine;


    private void Start()
    {
        transform.DOKill();

        this.transform.DOScale(initialScale * popupScale, popupDuration).SetEase(popupEase).OnComplete(() =>
        {
            this.transform.DOScale(initialScale, popupDuration * 0.5f).SetEase(popupEase);
        });
    }

    private void Update()
    {
    }


    private void OnMouseEnter()
    {
        this.transform.DOScale(initialScale * popupScale, popupDuration * 0.5f).SetEase(popupEase);
    }

    private void OnMouseExit()
    {
        this.transform.DOScale(initialScale, popupDuration * 0.5f).SetEase(popupEase);
    }
}
