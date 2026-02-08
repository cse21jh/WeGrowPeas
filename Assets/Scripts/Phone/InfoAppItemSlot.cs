using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class InfoAppItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText; // 구매 수량 or 레벨
    [SerializeField] private GameObject lockIcon; // 잠금 상태 표시 (일반 특성용)

    private string description;
    private Action<string> onHover;
    private Action onHoverExit;

    public void Setup(Sprite icon, string name, int count, string desc, Action<string> hoverCallback, Action hoverExitCallback = null, bool isLocked = false)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }

        if (nameText != null)
        {
            nameText.text = name;
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(name));
        }

        if (countText != null)
        {
            if (count > 0)
            {
                countText.text = count.ToString();
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }

        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
        }

        description = desc;
        onHover = hoverCallback;
        onHoverExit = hoverExitCallback;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHover?.Invoke(description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }
}
