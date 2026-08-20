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
        if (popupRect == null) return;

        // 닫기/열기 트윈이 남아 있으면 방금 연 팝업을 도로 닫거나 크기가 어긋난다.
        popupRect.DOKill();

        popupRect.gameObject.SetActive(true);
        popupRect.sizeDelta = Vector2.zero; // 항상 0에서 펼쳐지도록

        popupRect.DOSizeDelta(originalSize, animationDuration).SetEase(animationEase);
    }


}
