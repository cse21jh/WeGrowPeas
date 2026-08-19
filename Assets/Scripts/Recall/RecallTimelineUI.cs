using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 회상 타임라인. 하단 슬라이더로 일차를 옮기면 그날의 농장 상태가 통째로 바뀐다.
///
/// 보여주는 것 — 밭(칸별 식물) / 보유 아이템 / 웨이브와 사망 수 / 그날의 저주 /
/// 보유 골드와 전날 대비 변화량. 추가·판매·구매 수는 전날 스냅샷과의 차이로 계산한다
/// (<see cref="DaySnapshot"/>이 누적값만 담는 이유).
/// </summary>
public class RecallTimelineUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject timelinePanel;

    [Header("Slider")]
    [SerializeField] private Slider daySlider;
    [SerializeField] private TMP_Text dayLabel;

    [Header("밭")]
    [SerializeField] private Transform gridContainer;
    [SerializeField] private GridLayoutGroup gridLayout;

    [Header("아이템 / 저주")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private Transform curseContainer;
    [SerializeField] private TMP_Text curseEmptyText;

    [Header("아이콘 슬롯")]
    [SerializeField] private RecallIconSlot iconSlotPrefab;

    [Header("수치")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text summaryText;

    [Header("설명")]
    [SerializeField] private RecallTooltip tooltip;

    /// <summary>밭이 넓어질 수 있는 최대 열 수. (AddSoilItemData.MAX_COL과 같은 값)</summary>
    private const int MaxFarmCols = 8;

    /// <summary>밭 세로 칸 수. Grid의 인덱스 계산(col = idx / 4)에 묶여 있다.</summary>
    private const int FarmRows = 4;

    private RecallRunFile _run;
    private readonly List<RecallIconSlot> _spawned = new List<RecallIconSlot>();

    public bool IsOpen => timelinePanel != null && timelinePanel.activeSelf;

    private void Awake()
    {
        if (daySlider != null)
        {
            daySlider.wholeNumbers = true;
            daySlider.onValueChanged.AddListener(v => SetDayIndex(Mathf.RoundToInt(v)));
        }
    }

    /// <summary>기록 하나의 타임라인을 연다. 마지막 날부터 보여준다.</summary>
    public void Show(RecallRunFile run)
    {
        if (run == null || run.recall == null || run.recall.days.Count == 0)
        {
            Debug.LogWarning("[Recall] 일자별 기록이 없어 타임라인을 열 수 없습니다.");
            return;
        }

        _run = run;

        if (timelinePanel != null) timelinePanel.SetActive(true);
        ClearDescription();

        // 방금 켠 패널의 크기가 확정돼야 셀 크기를 맞출 수 있다.
        Canvas.ForceUpdateCanvases();

        int last = _run.recall.days.Count - 1;
        if (daySlider != null)
        {
            daySlider.minValue = 0;
            daySlider.maxValue = last;
            // 값이 그대로면 onValueChanged가 안 불리므로 직접 갱신한다.
            if (Mathf.RoundToInt(daySlider.value) == last) SetDayIndex(last);
            else daySlider.value = last;
        }
        else
        {
            SetDayIndex(last);
        }
    }

    public void Close()
    {
        if (timelinePanel != null) timelinePanel.SetActive(false);
        ClearSpawned();
    }

    // ── 하루 그리기 ───────────────────────────────────────────────────────────

    private void SetDayIndex(int index)
    {
        if (_run == null) return;

        var days = _run.recall.days;
        index = Mathf.Clamp(index, 0, days.Count - 1);

        DaySnapshot today = days[index];
        DaySnapshot prev = index > 0 ? days[index - 1] : null;

        ClearSpawned();

        if (dayLabel != null)
            dayLabel.text = today.isFinalPartial ? $"{today.day}일차 (마지막 날)" : $"{today.day}일차";

        BuildGrid(today);
        BuildItems(today, prev);
        BuildCurses(today);
        BuildTexts(today, prev);
    }

    private void BuildGrid(DaySnapshot day)
    {
        if (gridContainer == null || iconSlotPrefab == null) return;

        // 밭 인덱스는 열 우선이다 (Grid: col = idx / 4, row = idx % 4).
        // 세로로 먼저 채워야 실제 밭과 같은 모양으로 보인다.
        if (gridLayout != null)
        {
            gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gridLayout.constraintCount = FarmRows;

            // 칸 수는 항상 최대 크기 기준. 밭이 넓어져도 격자가 흔들리지 않는다.
            FitCellSize(gridLayout, MaxFarmCols, FarmRows);
        }

        // 그날 쓰던 만큼만 실제 칸이고, 나머지는 아직 넓히지 않은 자리로 어둡게 깐다.
        int usable = day.cellSpecies.Length;

        for (int i = 0; i < MaxFarmCols * FarmRows; i++)
        {
            var slot = Instantiate(iconSlotPrefab, gridContainer);

            if (i >= usable)
            {
                slot.SetupLocked($"{i / FarmRows + 1}번째 줄 — 아직 넓히지 않은 땅", ShowDescription, ClearDescription);
            }
            else
            {
                string species = day.cellSpecies[i];

                if (string.IsNullOrEmpty(species))
                    slot.SetupEmpty($"{i + 1}번째 땅 — 비어 있음", ShowDescription, ClearDescription);
                else
                    slot.Setup(RecallLookup.Plant(species), 0, ShowDescription, ClearDescription);
            }

            slot.gameObject.SetActive(true);
            _spawned.Add(slot);
        }
    }

    private void BuildItems(DaySnapshot day, DaySnapshot prev)
    {
        if (itemContainer == null || iconSlotPrefab == null) return;

        for (int i = 0; i < day.itemNames.Length; i++)
        {
            int count = i < day.itemCounts.Length ? day.itemCounts[i] : 0;
            int before = CountOf(prev, day.itemNames[i]);

            var slot = Instantiate(iconSlotPrefab, itemContainer);
            slot.Setup(RecallLookup.Item(day.itemNames[i]), count, ShowDescription, ClearDescription);
            slot.SetHighlighted(count > before); // 그날 산 것
            slot.gameObject.SetActive(true);
            _spawned.Add(slot);
        }

        foreach (var id in day.specialItemIds)
        {
            var slot = Instantiate(iconSlotPrefab, itemContainer);
            slot.Setup(RecallLookup.SpecialItem(id), 0, ShowDescription, ClearDescription);
            slot.SetHighlighted(prev != null && System.Array.IndexOf(prev.specialItemIds, id) < 0);
            slot.gameObject.SetActive(true);
            _spawned.Add(slot);
        }
    }

    private void BuildCurses(DaySnapshot day)
    {
        if (curseEmptyText != null) curseEmptyText.gameObject.SetActive(day.curseIds.Length == 0);
        if (curseContainer == null || iconSlotPrefab == null) return;

        foreach (var id in day.curseIds)
        {
            var slot = Instantiate(iconSlotPrefab, curseContainer);
            slot.Setup(RecallLookup.Curse(id), 0, ShowDescription, ClearDescription);
            slot.gameObject.SetActive(true);
            _spawned.Add(slot);
        }
    }

    private void BuildTexts(DaySnapshot day, DaySnapshot prev)
    {
        if (waveText != null)
            waveText.text = $"{RecallLookup.WaveName(day.waveType)} · 식물 {day.diedCount}개 사망";

        if (goldText != null)
        {
            int delta = day.gold - (prev != null ? prev.gold : day.gold);
            string deltaText = delta == 0 ? "±0" : (delta > 0 ? $"+{delta}" : delta.ToString());
            goldText.text = $"{day.gold} G ({deltaText})";
        }

        if (summaryText != null)
        {
            int added = day.cumBreedCount - (prev != null ? prev.cumBreedCount : 0);
            int sold = day.cumSellCount - (prev != null ? prev.cumSellCount : 0);
            int bought = TotalItemCount(day) - (prev != null ? TotalItemCount(prev) : 0);

            summaryText.text =
                $"추가 {added}개 · 판매 {sold}개 · 사망 {day.diedCount}개\n" +
                $"번 골드 {day.earnedGold} · 구매 {bought}개";
        }
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 지정한 칸 수가 패널에 들어가도록 <see cref="GridLayoutGroup.cellSize"/>를 맞춘다.
    /// 화면 해상도나 캔버스 기준 해상도가 바뀌어도 밭이 패널을 채우게 하려는 것.
    /// </summary>
    private static void FitCellSize(GridLayoutGroup layout, int cols, int rows)
    {
        var rt = layout.transform as RectTransform;
        if (rt == null || cols <= 0 || rows <= 0) return;

        float w = rt.rect.width - layout.padding.left - layout.padding.right - layout.spacing.x * (cols - 1);
        float h = rt.rect.height - layout.padding.top - layout.padding.bottom - layout.spacing.y * (rows - 1);
        if (w <= 0f || h <= 0f) return; // 아직 크기가 안 잡혔으면 건드리지 않는다

        float cell = Mathf.Floor(Mathf.Min(w / cols, h / rows));
        if (cell >= 1f) layout.cellSize = new Vector2(cell, cell);
    }

    private static int CountOf(DaySnapshot snapshot, string itemName)
    {
        if (snapshot == null) return 0;

        for (int i = 0; i < snapshot.itemNames.Length; i++)
            if (snapshot.itemNames[i] == itemName)
                return i < snapshot.itemCounts.Length ? snapshot.itemCounts[i] : 0;

        return 0;
    }

    private static int TotalItemCount(DaySnapshot snapshot)
    {
        int sum = 0;
        foreach (int c in snapshot.itemCounts) sum += c;
        return sum;
    }

    private void ClearSpawned()
    {
        foreach (var slot in _spawned)
            if (slot != null) Destroy(slot.gameObject);
        _spawned.Clear();
    }

    private void ShowDescription(string text)
    {
        if (tooltip != null) tooltip.Show(text);
    }

    private void ClearDescription()
    {
        if (tooltip != null) tooltip.Hide();
    }
}
