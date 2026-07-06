using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PopupHideController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform maximizedPanel; // ��ü �˾�
    [SerializeField] private RectTransform minimizedPanel; // �ּ�ȭ �ڵ�
    [SerializeField] private Button minimizeButton;        // �ּ�ȭ ��ư
    [SerializeField] private Button maximizeButton;         // ��üâ ��ư

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    private Vector3 maxPanelOriginalScale;
    private Vector3 minPanelOriginalScale;

    private void Awake()
    {
        maxPanelOriginalScale = maximizedPanel.localScale;
        minPanelOriginalScale = minimizedPanel.localScale;

        // ���� ����: ��ü �гθ� ���� �ְ�, �ڵ��� ���� ����
        minimizedPanel.gameObject.SetActive(false);

        // �̺�Ʈ ����
        minimizeButton.onClick.AddListener(MinimizePanel);
        maximizeButton.onClick.AddListener(MaximizePanel);
    }

    public void MinimizePanel()
    {
        // 두 패널의 상단(Top) 위치를 일치시키기 위한 보정값 계산
        float maxOffset = maximizedPanel.rect.height * (1f - maximizedPanel.pivot.y);
        float minOffset = minimizedPanel.rect.height * (1f - minimizedPanel.pivot.y);

        Vector3 targetPos = maximizedPanel.localPosition;
        targetPos.y = targetPos.y + maxOffset - minOffset;
        minimizedPanel.localPosition = targetPos;
        // ��ü �г� ���
        maximizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                maximizedPanel.gameObject.SetActive(false);

                // �ڵ� ����
                minimizedPanel.gameObject.SetActive(true);
                minimizedPanel.localScale = Vector3.zero;
                minimizedPanel.DOScale(minPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }

    public void MaximizePanel()
    {
        // 두 패널의 상단(Top) 위치를 일치시키기 위한 보정값 계산
        float maxOffset = maximizedPanel.rect.height * (1f - maximizedPanel.pivot.y);
        float minOffset = minimizedPanel.rect.height * (1f - minimizedPanel.pivot.y);

        Vector3 targetPos = minimizedPanel.localPosition;
        targetPos.y = targetPos.y + minOffset - maxOffset;
        maximizedPanel.localPosition = targetPos;
        // �ڵ� ���
        minimizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                minimizedPanel.gameObject.SetActive(false);

                // ��ü �г� ����
                maximizedPanel.gameObject.SetActive(true);
                maximizedPanel.localScale = Vector3.zero;
                maximizedPanel.DOScale(maxPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }
}
