using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PopupHideController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform maximizedPanel; // 전체 팝업
    [SerializeField] private RectTransform minimizedPanel; // 최소화 핸들
    [SerializeField] private Button minimizeButton;        // 최소화 버튼
    [SerializeField] private Button maximizeButton;         // 전체창 버튼

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    private Vector3 maxPanelOriginalScale;
    private Vector3 minPanelOriginalScale;

    private void Awake()
    {
        maxPanelOriginalScale = maximizedPanel.localScale;
        minPanelOriginalScale = minimizedPanel.localScale;

        // 시작 상태: 전체 패널만 켜져 있고, 핸들은 꺼져 있음
        minimizedPanel.gameObject.SetActive(false);

        // 이벤트 연결
        minimizeButton.onClick.AddListener(MinimizePanel);
        maximizeButton.onClick.AddListener(MaximizePanel);
    }

    public void MinimizePanel()
    {
        SoundManager.Instance.PlayEffect("Button");
        minimizedPanel.localPosition = maximizedPanel.localPosition;
        // 전체 패널 축소
        maximizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                maximizedPanel.gameObject.SetActive(false);
                
                // 핸들 등장
                minimizedPanel.gameObject.SetActive(true);
                minimizedPanel.localScale = Vector3.zero;
                minimizedPanel.DOScale(minPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }

    public void MaximizePanel()
    {
        SoundManager.Instance.PlayEffect("Button");
        maximizedPanel.localPosition = minimizedPanel.localPosition;
        // 핸들 축소
        minimizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                minimizedPanel.gameObject.SetActive(false);
                
                // 전체 패널 복원
                maximizedPanel.gameObject.SetActive(true);
                maximizedPanel.localScale = Vector3.zero;
                maximizedPanel.DOScale(maxPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }
}
