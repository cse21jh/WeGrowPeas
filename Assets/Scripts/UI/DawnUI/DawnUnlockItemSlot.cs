using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 새벽 UI에서 단계별 해금 아이템 아이콘을 표시하고 마우스 호버 이벤트를 감지하여 툴팁을 띄우는 슬롯.
/// </summary>
public class DawnUnlockItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    private DawnUIController.UnlockItemInfo info;
    private DawnUIController controller;

    public void Setup(DawnUIController.UnlockItemInfo itemInfo, DawnUIController uiController)
    {
        info = itemInfo;
        controller = uiController;

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        // 아이콘 스프라이트가 존재하는 경우에만 변경 (null이면 프리팹 기본 상태 유지)
        if (iconImage != null && itemInfo.icon != null)
        {
            iconImage.sprite = itemInfo.icon;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null)
        {
            string suffix = info.isSpecial ? " (특수 아이템)" : " (상점 아이템)";
            string nameLine = $"<size=120%>{info.displayName}</size>{suffix}";
            controller.ShowTooltip(nameLine, info.description, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (controller != null)
        {
            controller.HideTooltip();
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.HideTooltip();
        }
    }
}
