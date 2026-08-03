using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감(전체화면 책) 컨트롤러. 좌측 카테고리(4) → 하위 목록 → 우측 상세, 하단 페이지.
/// 데이터는 <see cref="CodexCatalog"/>에서 로드하며 씬 의존이 없어 시작화면에서도 열 수 있다.
/// </summary>
public class CodexUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject codexPanel;

    [Header("Category Buttons (Item, Plant, Curse, Bug 순서)")]
    [SerializeField] private Button[] categoryButtons;

    [Header("List")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private CodexListSlot listSlotPrefab;

    [Header("Detail")]
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailText;

    [Header("Footer")]
    [SerializeField] private TMP_Text pageText; // (현재) / (전체)

    private CodexProgress.Category _cat = CodexProgress.Category.Item;
    private List<CodexEntry> _entries = new List<CodexEntry>();
    private readonly List<CodexListSlot> _slots = new List<CodexListSlot>();
    private int _selected = -1;

    /// <summary>도감이 열려 있는가.</summary>
    public bool IsOpen => codexPanel != null && codexPanel.activeSelf;

    // 컨트롤러는 항상 켜진 루트에 있어야 함(패널만 토글). 그래야 닫힌 상태에서도 F8이 동작.
    private void Update()
    {
        // F8은 디버그용 단축키(정식 진입은 버튼). 디버그 패널이 꺼져 있으면 동작하지 않는다.
        if (DebugPanels.Enabled && Input.GetKeyDown(KeyCode.F8)) Toggle();
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) CloseCodex();
    }

    public void Toggle()
    {
        if (IsOpen) CloseCodex();
        else OpenCodex();
    }

    public void OpenCodex()
    {
        if (codexPanel != null) codexPanel.SetActive(true);
        WireCategoryButtons();
        SelectCategory((int)_cat);
    }

    public void CloseCodex()
    {
        if (codexPanel != null) codexPanel.SetActive(false);
    }

    private void WireCategoryButtons()
    {
        if (categoryButtons == null) return;
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            int idx = i;
            categoryButtons[i].onClick.RemoveAllListeners();
            categoryButtons[i].onClick.AddListener(() => SelectCategory(idx));
        }
    }

    public void SelectCategory(int catIndex)
    {
        _cat = (CodexProgress.Category)catIndex;
        _entries = CodexCatalog.Get(_cat);
        BuildList();
        SelectEntry(_entries.Count > 0 ? 0 : -1);
    }

    private void BuildList()
    {
        foreach (var s in _slots) if (s != null) Destroy(s.gameObject);
        _slots.Clear();

        if (listContainer == null || listSlotPrefab == null) return;

        for (int i = 0; i < _entries.Count; i++)
        {
            var slot = Instantiate(listSlotPrefab, listContainer);
            slot.Setup(_entries[i], i, SelectEntry);
            slot.gameObject.SetActive(true);
            _slots.Add(slot);
        }
    }

    public void SelectEntry(int index)
    {
        _selected = index;

        if (index < 0 || index >= _entries.Count)
        {
            if (detailName) detailName.text = "";
            if (detailText) detailText.text = "";
            if (detailIcon) detailIcon.enabled = false;
            UpdatePage();
            return;
        }

        var e = _entries[index];

        if (e.locked)
        {
            // 잠김: 이름 대신 "잠김", 상세엔 무엇을 해야 해금되는지 안내
            if (detailName) detailName.text = "잠김";
            if (detailText)
                detailText.text = string.IsNullOrEmpty(e.unlockHint)
                    ? "특정 조건을 만족하면 해금됩니다."
                    : e.unlockHint;
            if (detailIcon) detailIcon.enabled = false;
        }
        else
        {
            if (detailName) detailName.text = e.discovered ? e.displayName : "???";
            if (detailText) detailText.text = e.discovered ? e.detail : "아직 발견하지 못했습니다.";
            if (detailIcon)
            {
                bool show = e.discovered && e.icon != null;
                detailIcon.enabled = show;
                if (show) detailIcon.sprite = e.icon;
            }
        }
        UpdatePage();
    }

    private void UpdatePage()
    {
        if (pageText == null) return;
        int total = _entries.Count;
        int cur = (_selected >= 0 && total > 0) ? _selected + 1 : 0;
        pageText.text = $"{cur} / {total}";
    }
}
