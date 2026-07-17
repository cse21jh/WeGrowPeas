using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class UnlockPopup : BasePopup
{
    [Header("Panel References")]
    [SerializeField] private RectTransform maximizedPanel;
    [SerializeField] private RectTransform minimizedPanel;

    [Header("Buttons")]
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button maximizeButton;
    [SerializeField] private Button maxCloseButton;
    [SerializeField] private Button minCloseButton;

    [Header("Item Grid Settings")]
    [SerializeField] private Transform itemContainer;        // Parent transform containing GridLayoutGroup
    [SerializeField] private GameObject itemSlotPrefab;     // Prefab or template child slot

    [Header("Tooltip Panel")]
    [SerializeField] private RectTransform tooltipPanel;     // Local speech bubble tooltip panel
    [SerializeField] private TextMeshProUGUI tooltipText;    // Text to print description

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    private Vector3 maxPanelOriginalScale = Vector3.one;
    private Vector3 minPanelOriginalScale = Vector3.one;
    private List<GameObject> activeSlots = new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();

        if (maximizedPanel != null) maxPanelOriginalScale = maximizedPanel.localScale;
        if (minimizedPanel != null) minPanelOriginalScale = minimizedPanel.localScale;

        if (minimizedPanel != null) minimizedPanel.gameObject.SetActive(false);

        if (minimizeButton != null) minimizeButton.onClick.AddListener(MinimizePanel);
        if (maximizeButton != null) maximizeButton.onClick.AddListener(MaximizePanel);

        if (maxCloseButton != null) maxCloseButton.onClick.AddListener(Close);
        if (minCloseButton != null) minCloseButton.onClick.AddListener(Close);

        if (tooltipPanel != null) tooltipPanel.gameObject.SetActive(false);
        
        // If the itemSlotPrefab is a child of the container in the scene, deactivate it to use as template
        if (itemSlotPrefab != null && itemSlotPrefab.transform.parent == itemContainer)
        {
            itemSlotPrefab.SetActive(false);
        }
    }

    public override void Open()
    {
        base.Open();
        
        // Reset to maximized state on opening
        if (maximizedPanel != null)
        {
            maximizedPanel.gameObject.SetActive(true);
            maximizedPanel.localScale = maxPanelOriginalScale;
        }
        if (minimizedPanel != null)
        {
            minimizedPanel.gameObject.SetActive(false);
        }

        HideTooltip();
    }

    /// 가변 개수의 아이템 데이터들을 입력받아 슬롯 그리드를 동적으로 구성합니다.
    public void Setup(List<ItemData> items, System.Action onClose = null)
    {
        onCloseCallback = onClose;

        // 기존에 인스턴스화된 슬롯 클리어
        foreach (var slot in activeSlots)
        {
            if (slot != null && slot != itemSlotPrefab)
            {
                Destroy(slot);
            }
        }
        activeSlots.Clear();

        if (items == null || items.Count == 0) return;

        // 슬롯 동적 생성
        foreach (var item in items)
        {
            if (item == null) continue;

            if (itemSlotPrefab != null)
            {
                GameObject slotObj = Instantiate(itemSlotPrefab, itemContainer);
                slotObj.SetActive(true);

                // 슬롯 컴포넌트 추가 또는 획득
                UnlockItemSlot slotScript = slotObj.GetComponent<UnlockItemSlot>();
                if (slotScript == null)
                {
                    slotScript = slotObj.AddComponent<UnlockItemSlot>();
                }

                slotScript.Initialize(item, this);
                activeSlots.Add(slotObj);
            }
        }

        HideTooltip();
    }

    public void MinimizePanel()
    {
        if (maximizedPanel == null || minimizedPanel == null) return;
        HideTooltip();

        // PopupHideController와 동일한 방식으로 계산
        float maxOffset = maximizedPanel.rect.height * (1f - maximizedPanel.pivot.y);
        float minOffset = minimizedPanel.rect.height * (1f - minimizedPanel.pivot.y);

        Vector3 targetPos = maximizedPanel.localPosition;
        targetPos.y = targetPos.y + maxOffset - minOffset;
        minimizedPanel.localPosition = targetPos;

        maximizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                maximizedPanel.gameObject.SetActive(false);

                minimizedPanel.gameObject.SetActive(true);
                minimizedPanel.localScale = Vector3.zero;
                minimizedPanel.DOScale(minPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }

    public void MaximizePanel()
    {
        if (maximizedPanel == null || minimizedPanel == null) return;

        // PopupHideController와 동일한 방식으로 계산
        float maxOffset = maximizedPanel.rect.height * (1f - maximizedPanel.pivot.y);
        float minOffset = minimizedPanel.rect.height * (1f - minimizedPanel.pivot.y);

        Vector3 targetPos = minimizedPanel.localPosition;
        targetPos.y = targetPos.y + minOffset - maxOffset;
        maximizedPanel.localPosition = targetPos;

        minimizedPanel.DOScale(Vector3.zero, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                minimizedPanel.gameObject.SetActive(false);

                maximizedPanel.gameObject.SetActive(true);
                maximizedPanel.localScale = Vector3.zero;
                maximizedPanel.DOScale(maxPanelOriginalScale, duration).SetEase(ease).SetUpdate(true);
            }).SetUpdate(true);
    }

    public void ShowTooltip(string description, Vector3 slotWorldPosition)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = description;
        tooltipPanel.gameObject.SetActive(true);

        // 부모 캔버스 모드에 따라 적합한 UI 카메라를 찾아 매칭 (Overlay 대응)
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // 월드 좌표를 팝업 RectTransform 로컬 공간 좌표로 변환 (BasePopup의 캐싱된 rectTransform 활용)
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPosition),
            uiCamera,
            out localPoint
        );

        // 말풍선 위치 조정 (슬롯의 윗단 부분에 위치하도록 오프셋 부여)
        tooltipPanel.anchoredPosition = localPoint + new Vector2(0f, 60f);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }
    }
}

public class UnlockItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ItemData itemData;
    private UnlockPopup ownerPopup;
    private Image iconImage;

    public void Initialize(ItemData data, UnlockPopup popup)
    {
        itemData = data;
        ownerPopup = popup;

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>(true);
            }
        }

        if (iconImage != null && data != null)
        {
            iconImage.sprite = data.Icon;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ownerPopup != null && itemData != null && !string.IsNullOrEmpty(itemData.Description))
        {
            ownerPopup.ShowTooltip(itemData.Description, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ownerPopup != null)
        {
            ownerPopup.HideTooltip();
        }
    }

    private void OnDisable()
    {
        if (ownerPopup != null)
        {
            ownerPopup.HideTooltip();
        }
    }
}
