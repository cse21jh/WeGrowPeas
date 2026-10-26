using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WeGrowPeas.UI.Book;

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
    [SerializeField] private BookCloseMotion closeMotion;

    [Header("List")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private RecallListSlot listSlotPrefab;

    [Header("Book list")]
    [SerializeField] private BookLayoutController bookLayouts;
    [SerializeField, Min(0)] private int listLayoutIndex = 1;
    [SerializeField] private RecallListSlot[] leftPageSlots = Array.Empty<RecallListSlot>();
    [SerializeField] private RecallListSlot[] rightPageSlots = Array.Empty<RecallListSlot>();
    [SerializeField] private Button previousPage;
    [SerializeField] private Button nextPage;

    [Header("Labels")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private TMP_Text rightEmptyText;
    [SerializeField] private TMP_Text leftPageNumber;
    [SerializeField] private TMP_Text rightPageNumber;

    [Header("Detail")]
    [SerializeField] private RecallDetailUI detailUI;
    [SerializeField] private RecallTimelineUI bookTimeline;
    [SerializeField, Min(0)] private int detailLayoutIndex = 2;
    [SerializeField] private Button backToList;
    [SerializeField] private Button resultButton;
    private RecallRunFile _selectedRun;
    private string _selectedRecordId;

    /// <summary>목록에 한 줄로 놓을 카드 수. 카드 크기는 여기서 역산한다.</summary>
    private const int CardColumns = 4;

    private readonly List<RecallListSlot> _slots = new List<RecallListSlot>();

    /// <summary>목록에 띄운 사진들. 닫을 때 직접 해제해야 메모리에 쌓이지 않는다.</summary>
    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private readonly List<Texture2D> _retiredTextures = new List<Texture2D>();
    private List<RecallIndexEntry> _entries = new List<RecallIndexEntry>();
    private int _spread;

    private int SlotsPerSpread => leftPageSlots.Length + rightPageSlots.Length;
    public int SpreadIndex => _spread;
    public int SpreadCount => Mathf.Max(1, Mathf.CeilToInt(_entries.Count / (float)Mathf.Max(1, SlotsPerSpread)));
    private bool IsTurning => bookLayouts != null && bookLayouts.IsTurning;
    public bool IsClosing => closeMotion != null && closeMotion.IsClosing;
    public bool IsOpening => closeMotion != null && closeMotion.IsOpening;
    private bool IsBusy => IsTurning || IsClosing || IsOpening;
    private bool IsBookListVisible => IsOpen && bookLayouts != null && bookLayouts.CurrentIndex == listLayoutIndex;
    public bool IsBookDetailVisible => IsOpen && bookLayouts != null && bookTimeline != null &&
                                       bookLayouts.CurrentIndex == detailLayoutIndex;

    /// <summary>카드를 눌렀을 때. 상세 화면이 여기에 붙는다.</summary>
    public event Action<string> EntrySelected;

    /// <summary>회상 화면이 열려 있는가.</summary>
    public bool IsOpen => recallPanel != null && recallPanel.activeSelf;

    private void OnEnable()
    {
        if (previousPage != null) previousPage.onClick.AddListener(PreviousPage);
        if (nextPage != null) nextPage.onClick.AddListener(NextPage);
        if (backToList != null) backToList.onClick.AddListener(BackToList);
        if (resultButton != null) resultButton.onClick.AddListener(ShowResult);
        if (bookTimeline != null) bookTimeline.DayChanged += RefreshBookPageNumbers;
    }

    private void OnDisable()
    {
        if (closeMotion != null) closeMotion.CancelAndRestore();
        if (previousPage != null) previousPage.onClick.RemoveListener(PreviousPage);
        if (nextPage != null) nextPage.onClick.RemoveListener(NextPage);
        if (backToList != null) backToList.onClick.RemoveListener(BackToList);
        if (resultButton != null) resultButton.onClick.RemoveListener(ShowResult);
        if (bookTimeline != null) { bookTimeline.DayChanged -= RefreshBookPageNumbers; bookTimeline.Close(); }
        ClearList();
    }

    private void Start()
    {
        if (bookLayouts != null && IsOpen) OpenRecall();
    }

    private void LateUpdate()
    {
        RefreshBookNavigation();
        // The source pictures remain alive until the page-turn snapshots are finished.
        if (!IsTurning) ReleaseTextures(_retiredTextures);
    }

    private void Update()
    {
        if (IsClosing || IsOpening) return;
        // F7은 디버그용 단축키(정식 진입은 버튼). 디버그 패널이 꺼져 있으면 동작하지 않는다.
        if (DebugPanels.Enabled && Input.GetKeyDown(KeyCode.F7)) Toggle();
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            // 타임라인 → 상세 → 목록 → 닫기 순으로 한 단계씩 물러난다.
            if (detailUI != null && detailUI.CloseTopmost()) return;
            if (IsBookDetailVisible) { BackToList(); return; }
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
        if (IsBusy) return;
        if (recallPanel != null) recallPanel.SetActive(true);
        if (bookLayouts != null) BuildBookList(RecallStore.GetEntries());
        else BuildList(); // Older scenes still use the original scroll list.
        if (closeMotion != null) closeMotion.PlayOpen(RefreshBookNavigation);
        RefreshBookNavigation();
    }

    public void CloseRecall()
    {
        if (IsClosing || IsOpening) return;
        if (detailUI != null) detailUI.Close();
        var tooltip = GetComponentInChildren<RecallTooltip>();
        if (tooltip != null) tooltip.Hide();
        if (!IsOpen || closeMotion == null || !closeMotion.Play(CloseRecallImmediately))
            CloseRecallImmediately();
    }

    /// <summary>Immediate teardown for scene changes; normal close actions play the book motion first.</summary>
    public void CloseRecallImmediately()
    {
        if (detailUI != null) detailUI.Close();
        if (recallPanel != null) recallPanel.SetActive(false);
        if (closeMotion != null) closeMotion.CancelAndRestore();
        if (bookTimeline != null) bookTimeline.Close();
        _selectedRun = null;
        _selectedRecordId = null;
        ClearList(); // 사진을 들고 있을 이유가 없다
        RefreshBookNavigation();
    }

    private void BuildBookList(List<RecallIndexEntry> entries)
    {
        if (detailUI != null) detailUI.Close();
        if (bookTimeline != null) bookTimeline.Close();
        _selectedRun = null;
        _selectedRecordId = null;
        ClearList();
        _entries = entries;
        _spread = 0;
        if (countText != null) countText.text = $"{entries.Count} / {RecallStore.MaxEntries}";
        bookLayouts.ShowImmediate(listLayoutIndex);
        RenderSpread();
    }

    public void PreviousPage() => MovePage(-1);
    public void NextPage() => MovePage(1);

    private void MovePage(int direction)
    {
        if (IsBookDetailVisible)
        {
            if (IsBusy || (detailUI != null && detailUI.IsOpen)) return;
            int day = bookTimeline.DayIndex + direction;
            if (day < 0 || day >= bookTimeline.DayCount) return;
            bookLayouts.Turner.TryTurn(direction > 0, () => bookTimeline.SetDayIndex(day));
            RefreshBookNavigation();
            return;
        }
        if (!IsBookListVisible || IsBusy || (detailUI != null && detailUI.IsOpen)) return;
        int target = _spread + direction;
        if (target < 0 || target >= SpreadCount) return;
        bookLayouts.Turner.TryTurn(direction > 0, () =>
        {
            _spread = target;
            RenderSpread();
        });
        RefreshBookNavigation();
    }

    private void RenderSpread()
    {
        _retiredTextures.AddRange(_textures);
        _textures.Clear();
        int first = _spread * SlotsPerSpread;
        FillPage(leftPageSlots, first);
        FillPage(rightPageSlots, first + leftPageSlots.Length);
        if (emptyText != null) emptyText.gameObject.SetActive(_entries.Count == 0);
        if (rightEmptyText != null)
        {
            rightEmptyText.text = _entries.Count == 0 ? "아직 남은 기록이 없습니다." : "마지막 기록입니다.";
            rightEmptyText.gameObject.SetActive(first + leftPageSlots.Length >= _entries.Count);
        }
        RefreshBookPageNumbers();
        Canvas.ForceUpdateCanvases();
        RefreshBookNavigation();
    }

    private void FillPage(RecallListSlot[] slots, int first)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            int index = first + i;
            if (index >= _entries.Count)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
                continue;
            }
            var entry = _entries[index];
            Texture2D texture = RecallStore.LoadImage(entry.id);
            if (texture != null) _textures.Add(texture);
            slot.Setup(entry, texture, OnEntryClicked, index + 1);
            slot.gameObject.SetActive(true);
        }
    }

    private void RefreshBookNavigation()
    {
        if (bookLayouts == null) return;
        bool ready = !IsBusy && (detailUI == null || !detailUI.IsOpen);
        bool detail = IsBookDetailVisible;
        int index = detail ? bookTimeline.DayIndex : _spread;
        int count = detail ? bookTimeline.DayCount : SpreadCount;
        bool canNavigate = ready && (detail || IsBookListVisible);
        if (previousPage != null) previousPage.interactable = canNavigate && index > 0;
        if (nextPage != null) nextPage.interactable = canNavigate && index >= 0 && index + 1 < count;
        if (backToList != null) backToList.interactable = ready && detail;
        if (resultButton != null) resultButton.interactable = ready && detail && _selectedRun != null;
    }

    private void RefreshBookPageNumbers()
    {
        string number = IsBookDetailVisible
            ? (bookTimeline.DayCount == 0 ? "0/0" : $"{bookTimeline.DayIndex + 1}/{bookTimeline.DayCount}")
            : (_entries.Count == 0 ? "0/0" : $"{_spread + 1}/{SpreadCount}");
        if (leftPageNumber != null) leftPageNumber.text = number;
        if (rightPageNumber != null) rightPageNumber.text = number;
        RefreshBookNavigation();
    }

    // Called by Layout 2 On Show, after the old pages are captured and before the new pages are captured.
    public void RenderBookDetail()
    {
        if (bookTimeline != null && _selectedRun != null) bookTimeline.Show(_selectedRun);
        RefreshBookPageNumbers();
    }

    // Called by Layout 1 On Show. The list keeps the spread that opened the record.
    public void RestoreBookList()
    {
        if (bookTimeline != null) bookTimeline.Close();
        RefreshBookPageNumbers();
    }

    public void BackToList()
    {
        if (!IsBookDetailVisible || IsBusy || (detailUI != null && detailUI.IsOpen)) return;
        if (bookLayouts.TryShow(listLayoutIndex))
        {
            _selectedRun = null;
            _selectedRecordId = null;
        }
    }

    public void ShowResult()
    {
        if (!IsBookDetailVisible || IsBusy || detailUI == null || string.IsNullOrEmpty(_selectedRecordId)) return;
        detailUI.Show(_selectedRecordId);
        RefreshBookNavigation();
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

        ClearPage(leftPageSlots);
        ClearPage(rightPageSlots);
        ReleaseTextures(_textures);
        ReleaseTextures(_retiredTextures);
        _entries = new List<RecallIndexEntry>();
        _spread = 0;
    }

    private static void ClearPage(RecallListSlot[] slots)
    {
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            slot.Clear();
            slot.gameObject.SetActive(false);
        }
    }

    private static void ReleaseTextures(List<Texture2D> textures)
    {
        foreach (var texture in textures)
        {
            if (texture == null) continue;
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }
        textures.Clear();
    }

    private void OnEntryClicked(string id)
    {
        if (bookLayouts != null && (!IsBookListVisible || IsBusy)) return;
        if (bookLayouts != null && bookTimeline != null)
        {
            var run = RecallStore.LoadRun(id);
            if (run == null)
            {
                Debug.LogWarning($"[Recall] 기록을 읽지 못했습니다: {id}");
                return;
            }
            _selectedRun = run;
            _selectedRecordId = id;
            if (!bookLayouts.TryShow(detailLayoutIndex)) return;
            RefreshBookNavigation();
        }
        else if (detailUI != null) detailUI.Show(id);
        EntrySelected?.Invoke(id);
    }
}
