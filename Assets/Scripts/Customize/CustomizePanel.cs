using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치장 화면 오른쪽 패널. 왼쪽 탭으로 종류를 고르면 그 종류의 아이템을 목록에 채운다.
/// 슬롯을 누르면 그 아이템을 장착하고(<see cref="CustomizeState"/>), "사용중" 표시를 옮긴다.
/// </summary>
public class CustomizePanel : MonoBehaviour
{
    [Tooltip("CustomizeCategory 순서(토양, 목재, 풀밭, 케이스, 장식품)대로.")]
    [SerializeField] private Toggle[] tabs;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private CustomizeItemSlot slotPrefab;

    public CustomizeCategory CurrentCategory { get; private set; }

    private readonly List<CustomizeItemSlot> _slots = new List<CustomizeItemSlot>();

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            var category = (CustomizeCategory)i;
            // 이미 보고 있는 탭이면 다시 그리지 않는다.
            // (에디터에서는 UI를 다시 계산할 때 Toggle이 "켜짐"을 한 번 더 보내는데,
            //  그때 목록을 지우면 "rebuild loop" 에러가 난다.)
            tabs[i].onValueChanged.AddListener(on => { if (on && category != CurrentCategory) Show(category); });
        }
    }

    private void OnEnable()
    {
        CustomizeState.EquippedChanged += OnItemStateChanged;
        CustomizeInventory.OwnedChanged += OnItemStateChanged;
    }

    private void OnDisable()
    {
        CustomizeState.EquippedChanged -= OnItemStateChanged;
        CustomizeInventory.OwnedChanged -= OnItemStateChanged;
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
        RefreshSlotStates();

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f; // 맨 위부터
    }

    // 보유하지 않은 아이템은 CustomizeState.Equip이 무시한다. (구매 창은 나중에 여기서 띄운다.)
    private void OnSlotClicked(CustomizeItemSlot clicked) => CustomizeState.Equip(clicked.Item);

    private void OnItemStateChanged(CustomizeItemData item)
    {
        if (item.category == CurrentCategory) RefreshSlotStates();
    }

    /// <summary>슬롯마다 보유 여부(자물쇠)와 장착 여부("사용중")를 다시 표시한다.</summary>
    private void RefreshSlotStates()
    {
        foreach (var slot in _slots)
        {
            slot.SetOwned(CustomizeInventory.IsOwned(slot.Item));
            slot.SetEquipped(CustomizeState.IsEquipped(slot.Item));
        }
    }
}
