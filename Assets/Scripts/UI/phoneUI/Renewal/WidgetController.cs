using DG.Tweening;
using UnityEngine;

public class WidgetController : MonoBehaviour
{

    [SerializeField] private RectTransform popupRect;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    private Vector2 originalSize;


    private void Start()
    {
        if (popupRect != null)
        {
            originalSize = popupRect.sizeDelta;
            popupRect.sizeDelta = Vector2.zero; // 초기 크기를 0으로 설정
            popupRect.gameObject.SetActive(false); // 초기에는 비활성화
        }
    }

    public void ShowPopup()
    {
        if (popupRect != null)
        {
            popupRect.DOSizeDelta(originalSize, animationDuration).SetEase(animationEase).OnStart(() =>
            {
                popupRect.gameObject.SetActive(true); // 애니메이션 시작 시 활성화
            });
        }
    }


}
