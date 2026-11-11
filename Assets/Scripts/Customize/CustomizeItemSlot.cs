using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>치장 목록의 아이템 한 칸. 아이콘·이름·"사용중" 표시·선택 강조.</summary>
public class CustomizeItemSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject equippedBadge;
    [SerializeField] private GameObject selectedFrame;

    public CustomizeItemData Item { get; private set; }

    public void Setup(CustomizeItemData item, Action<CustomizeItemSlot> onClick)
    {
        Item = item;
        icon.sprite = item.icon;
        icon.enabled = item.icon != null;
        nameText.text = item.displayName;
        SetEquipped(false);
        SetSelected(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(this));
    }

    public void SetEquipped(bool on) => equippedBadge.SetActive(on);

    public void SetSelected(bool on) => selectedFrame.SetActive(on);
}
