using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 회상 메인 화면(전체화면 패널). 남아 있는 기록을 최신순으로 깔아 보여준다.
///
/// 씬 의존이 없어 시작화면에서도 결과 화면에서도 열 수 있다.
/// 컨트롤러가 붙은 루트는 항상 켜져 있고 <see cref="recallPanel"/>만 토글된다
/// (닫힌 상태에서도 단축키가 동작하도록 — <see cref="CodexUIController"/>와 같은 구조).
/// </summary>
public class RecallUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject recallPanel;

    [Header("List")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private RecallListSlot listSlotPrefab;

    [Header("Labels")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text emptyText;

    [Header("Detail")]
    [SerializeField] private RecallDetailUI detailUI;

    /// <summary>목록에 한 줄로 놓을 카드 수. 카드 크기는 여기서 역산한다.</summary>
    private const int CardColumns = 4;

    private readonly List<RecallListSlot> _slots = new List<RecallListSlot>();

    /// <summary>목록에 띄운 사진들. 닫을 때 직접 해제해야 메모리에 쌓이지 않는다.</summary>
    private readonly List<Texture2D> _textures = new List<Texture2D>();

    /// <summary>카드를 눌렀을 때. 상세 화면이 여기에 붙는다.</summary>
    public event Action<string> EntrySelected;

    /// <summary>회상 화면이 열려 있는가.</summary>
    public bool IsOpen => recallPanel != null && recallPanel.activeSelf;

    private void Update()
    {
        // F7은 디버그용 단축키(정식 진입은 버튼). 디버그 패널이 꺼져 있으면 동작하지 않는다.
        if (DebugPanels.Enabled && Input.GetKeyDown(KeyCode.F7)) Toggle();
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            // 타임라인 → 상세 → 목록 → 닫기 순으로 한 단계씩 물러난다.
            if (detailUI != null && detailUI.CloseTopmost()) return;
            CloseRecall();
        }
    }

    private void OnDestroy() => ClearList();

    public void Toggle()
    {
        if (IsOpen) CloseRecall();
        else OpenRecall();
    }

    public void OpenRecall()
    {
        if (recallPanel != null) recallPanel.SetActive(true);
        BuildList();
    }

    public void CloseRecall()
    {
        if (detailUI != null) detailUI.Close();
        if (recallPanel != null) recallPanel.SetActive(false);
        ClearList(); // 사진을 들고 있을 이유가 없다
    }

    private void BuildList()
    {
        ClearList();

        var entries = RecallStore.GetEntries(); // 최신순
        if (countText != null) countText.text = $"{entries.Count} / {RecallStore.MaxEntries}";
        if (emptyText != null) emptyText.gameObject.SetActive(entries.Count == 0);

        if (listContainer == null || listSlotPrefab == null) return;

        Canvas.ForceUpdateCanvases(); // 방금 켠 패널의 크기 확정
        FitCardSize();

        foreach (var entry in entries)
        {
            Texture2D tex = RecallStore.LoadImage(entry.id);
            if (tex != null) _textures.Add(tex);

            var slot = Instantiate(listSlotPrefab, listContainer);
            slot.Setup(entry, tex, OnEntryClicked);
            slot.gameObject.SetActive(true);
            _slots.Add(slot);
        }
    }

    /// <summary>
    /// 카드 크기를 목록 너비에 맞춘다(가로 <see cref="CardColumns"/>장). 고정 크기로 두면
    /// 캔버스가 커질 때 카드만 작게 남으므로 폭에서 역산한다. 사진 비율(16:9)에 맞춰 높이를 정한다.
    /// </summary>
    private void FitCardSize()
    {
        var layout = listContainer != null ? listContainer.GetComponent<GridLayoutGroup>() : null;
        var rt = listContainer as RectTransform;
        if (layout == null || rt == null) return;

        float usable = rt.rect.width - layout.padding.left - layout.padding.right
                       - layout.spacing.x * (CardColumns - 1);
        if (usable <= 0f) return;

        float w = Mathf.Floor(usable / CardColumns);
        if (w < 1f) return;

        // 사진(가로 16:9) + 아래 글자 영역이 카드의 28%를 쓴다 (프리팹 비율과 맞춤).
        float h = Mathf.Floor(w * 9f / 16f / 0.7f);
        layout.cellSize = new Vector2(w, h);
    }

    private void ClearList()
    {
        foreach (var slot in _slots)
            if (slot != null) Destroy(slot.gameObject);
        _slots.Clear();

        foreach (var tex in _textures)
            if (tex != null) Destroy(tex);
        _textures.Clear();
    }

    private void OnEntryClicked(string id)
    {
        if (detailUI != null) detailUI.Show(id);
        EntrySelected?.Invoke(id);
    }
}
