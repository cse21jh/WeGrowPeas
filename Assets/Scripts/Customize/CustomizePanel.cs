using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치장 화면 오른쪽 패널. 왼쪽 탭으로 종류를 고르면 그 종류의 아이템을 목록에 채운다.
/// 장착·저장은 아직 없다. 슬롯을 누르면 선택 강조만 하고 <see cref="ItemSelected"/>를 알린다.
/// </summary>
public class CustomizePanel : MonoBehaviour
{
    [Tooltip("CustomizeCategory 순서(토양, 목재, 풀밭, 케이스, 장식품)대로.")]
    [SerializeField] private Toggle[] tabs;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private CustomizeItemSlot slotPrefab;

    /// <summary>목록에서 아이템을 골랐다.</summary>
    public event Action<CustomizeItemData> ItemSelected;

    public CustomizeCategory CurrentCategory { get; private set; }

    private readonly List<CustomizeItemSlot> _slots = new List<CustomizeItemSlot>();

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            var category = (CustomizeCategory)i;
            tabs[i].onValueChanged.AddListener(on => { if (on) Show(category); });
        }
    }

    private void Start()
    {
        int on = Array.FindIndex(tabs, t => t.isOn);
        Show(on >= 0 ? (CustomizeCategory)on : CustomizeCategory.Soil);
    }

    /// <summary>그 종류의 아이템으로 목록을 다시 채운다.</summary>
    public void Show(CustomizeCategory category)
    {
        CurrentCategory = category;

        foreach (Transform child in content) Destroy(child.gameObject);
        _slots.Clear();

        foreach (var item in CustomizeCatalog.Get(category))
        {
            if (!item.IsUnlocked) continue;
            var slot = Instantiate(slotPrefab, content);
            slot.Setup(item, OnSlotClicked);
            _slots.Add(slot);
        }

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f; // 맨 위부터
    }

    private void OnSlotClicked(CustomizeItemSlot clicked)
    {
        foreach (var slot in _slots) slot.SetSelected(slot == clicked);
        ItemSelected?.Invoke(clicked.Item);
    }
}
