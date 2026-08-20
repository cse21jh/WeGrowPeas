using System.Collections.Generic;
using UnityEngine;

/// <summary>밭 한 칸에 걸려 있을 수 있는 효과. <see cref="GridPopup.effectIcons"/>의 순서와 같다.</summary>
public enum TileEffect
{
    GoldSoil,          // 황금 흙
    Fertilizer,        // 전용 비료
    PetBottle,         // 페트병
    ChiliPepper,       // 고추(매운 맛)
    Freezer,           // 급속 냉각기
    Sprinkler,         // 스프링클러
    AbsorbFertilizer,  // 저항력 흡수 비료
    Fog,               // 저주: 안개
    Mushroom           // 저주: 버섯
}

/// <summary>
/// 밭 정보 팝업(Popup_Grid). 정보 앱의 "밭 정보" 탭을 대체한다.
///
/// 왼쪽에 밭을 세로 4칸 × 가로 maxCol로 깔고, 칸을 누르면 오른쪽에 그 칸의 효과를 나열한다.
/// 효과 판정과 설명 문구는 정보 앱(<see cref="InfoAppGridSlot"/>)과 같다.
///
/// 칸과 상세 줄은 각각 <see cref="GridCellSlot"/> / <see cref="GridDetailRow"/>가 채운다.
/// (Tools/Grid/Setup Grid Prefabs 로 붙이고 연결할 수 있다)
/// </summary>
public class GridPopup : MonoBehaviour
{
    /// <summary>밭 세로 칸 수. Grid의 인덱스 계산(col = idx / 4)에 묶여 있다.</summary>
    private const int FarmRows = 4;

    [Header("밭 (GridPanel > Scroll View > Viewport > Content)")]
    [SerializeField] private Transform gridContent;

    [Tooltip("세로 한 줄 (GridLinePrefab). 안에 칸이 들어 있으면 그것을 그대로 쓴다.\n" +
             "비우면 아래 Cell Prefab으로 칸을 직접 찍어 Content에 넣는다.")]
    [SerializeField] private GameObject gridLinePrefab;

    [Tooltip("칸 하나 (GridPrefab). 줄 프리팹이 칸을 갖고 있으면 쓰이지 않는다.")]
    [SerializeField] private GameObject gridCellPrefab;

    [Header("상세 (DetailPanel > Scroll View > Viewport > Content)")]
    [SerializeField] private Transform detailContent;

    [Tooltip("효과 한 줄 (GridDetailPrefab)")]
    [SerializeField] private GameObject detailRowPrefab;

    [Tooltip("칸을 고르기 전에 띄울 안내. 없으면 생략")]
    [SerializeField] private GameObject detailEmptyText;

    [Header("효과 아이콘 (TileEffect 순서)")]
    [SerializeField] private Sprite[] effectIcons;

    // 비료 아이콘 색은 웨이브 색(WavePalette)을 그대로 쓴다. 여기서 따로 지정하지 않는다.

    private readonly List<GridCellSlot> cells = new List<GridCellSlot>();
    private int selectedIndex = -1;

    /// <summary>구독해 둔 Grid. 껐다 켜는 사이에 바뀔 수 있어 따로 들고 있는다.</summary>
    private Grid subscribedGrid;

    public void Open()
    {
        if (gameObject.activeSelf) Refresh();
        else gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    private void OnEnable()
    {
        // 저주 만료·빙결 해제처럼 밭 밖에서 바뀌는 것들.
        GameEvents.OnDayStarted += Refresh;

        Refresh(); // 여기서 Grid 구독도 함께 시도한다
    }

    private void OnDisable()
    {
        Unsubscribe();
        GameEvents.OnDayStarted -= Refresh;
    }

    /// <summary>
    /// Grid 알림에 붙는다. 식물이 심기고 뽑히고 옮겨질 때, 비료·페트병 등을 놓을 때 발생한다.
    /// (OnShopBought는 "결제" 시점이라 설치형 아이템은 아직 놓기 전이다)
    ///
    /// OnEnable 한 번만으로는 놓치는 경우가 있다. 팝업이 처음부터 켜져 있으면
    /// 그 시점엔 GameManager/Grid가 아직 없고, 계속 켜져 있으니 OnEnable이 다시 돌지도 않는다.
    /// 그래서 갱신할 때마다 확인해서 아직 안 붙었으면 붙는다.
    /// </summary>
    private void EnsureSubscribed()
    {
        Grid grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (grid == subscribedGrid) return; // 이미 이 Grid에 붙어 있음

        Unsubscribe();

        subscribedGrid = grid;
        if (subscribedGrid != null) subscribedGrid.OnGridStateChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (subscribedGrid != null) subscribedGrid.OnGridStateChanged -= Refresh;
        subscribedGrid = null;
    }

    /// <summary>밭을 다시 깔고, 고른 칸이 있으면 상세도 다시 채운다.</summary>
    public void Refresh()
    {
        EnsureSubscribed();

        BuildGrid();
        ShowDetail(selectedIndex);
    }

    // ── 밭 ────────────────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        Clear(gridContent);
        cells.Clear();

        Grid grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (grid == null)
        {
            // 농장 씬 밖이거나 아직 초기화 전. 다음 Refresh에서 다시 시도한다.
            Debug.LogWarning("[GridPopup] Grid를 찾지 못해 밭을 그리지 못했습니다.");
            return;
        }

        if (gridContent == null || gridCellPrefab == null)
        {
            Debug.LogWarning("[GridPopup] Grid Content 또는 Cell Prefab이 연결되지 않았습니다.");
            return;
        }

        int maxCol = grid.GetMaxCol();

        for (int col = 0; col < maxCol; col++)
        {
            Transform parent = gridContent;
            List<GridCellSlot> lineSlots = null;

            if (gridLinePrefab != null)
            {
                GameObject line = Instantiate(gridLinePrefab, gridContent);
                line.SetActive(true);
                parent = line.transform;

                // 줄 프리팹이 칸을 이미 갖고 있으면 그대로 쓴다.
                // 새로 찍어 넣으면 프리팹에 잡아 둔 간격·정렬이 무너지고 칸이 두 배로 생긴다.
                lineSlots = new List<GridCellSlot>(line.GetComponentsInChildren<GridCellSlot>(true));
            }

            for (int row = 0; row < FarmRows; row++)
            {
                int index = col * FarmRows + row; // Grid와 같은 열 우선 인덱스

                GridCellSlot slot = (lineSlots != null && row < lineSlots.Count)
                    ? lineSlots[row]
                    : SpawnCell(parent);

                if (slot == null) continue;

                slot.gameObject.SetActive(true);
                FillCell(grid, index, slot);
            }

            // 줄에 칸이 더 있으면 남는 것은 끈다.
            if (lineSlots != null)
                for (int i = FarmRows; i < lineSlots.Count; i++)
                    if (lineSlots[i] != null) lineSlots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>줄 프리팹이 칸을 안 갖고 있을 때만 새로 찍는다.</summary>
    private GridCellSlot SpawnCell(Transform parent)
    {
        if (gridCellPrefab == null)
        {
            Debug.LogWarning("[GridPopup] 줄 프리팹에 칸이 없고 Cell Prefab도 비어 있습니다.");
            return null;
        }

        GameObject cell = Instantiate(gridCellPrefab, parent);
        cell.SetActive(true);

        var slot = cell.GetComponent<GridCellSlot>();
        if (slot == null)
            Debug.LogWarning("[GridPopup] Cell Prefab에 GridCellSlot이 없습니다. " +
                             "Tools/Grid/Setup Grid Prefabs 로 붙일 수 있습니다.");

        return slot;
    }

    private void FillCell(Grid grid, int index, GridCellSlot slot)
    {
        while (cells.Count <= index) cells.Add(null);
        cells[index] = slot;

        List<TileEffect> effects = GetEffects(grid, index);

        // 아이콘 칸만큼만 보여주고, 넘치는 만큼은 개수로 표시한다.
        int shown = Mathf.Min(effects.Count, slot.IconCapacity);

        for (int i = 0; i < slot.IconCapacity; i++)
        {
            if (i < shown) slot.SetIcon(i, GetIcon(effects[i]), GetIconColor(grid, index, effects[i]));
            else slot.HideIcon(i);
        }

        slot.SetOverflow(effects.Count - shown);
        slot.SetSelected(index == selectedIndex);

        int captured = index;
        slot.SetClick(() => SelectCell(captured));
    }

    private void SelectCell(int index)
    {
        selectedIndex = index;

        for (int i = 0; i < cells.Count; i++)
            if (cells[i] != null) cells[i].SetSelected(i == index);

        ShowDetail(index);
    }

    // ── 상세 ──────────────────────────────────────────────────────────────────

    private void ShowDetail(int index)
    {
        Clear(detailContent);

        Grid grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        bool hasSelection = grid != null && index >= 0 && index < grid.GetMaxCol() * FarmRows;

        if (detailEmptyText != null) detailEmptyText.SetActive(!hasSelection);
        if (!hasSelection || detailContent == null || detailRowPrefab == null) return;

        List<TileEffect> effects = GetEffects(grid, index);

        if (effects.Count == 0)
        {
            AddDetailRow(null, $"{index + 1}번째 땅 — 효과 없음 (일반 토양)", Color.white);
            return;
        }

        foreach (TileEffect effect in effects)
            AddDetailRow(GetIcon(effect), GetDescription(grid, index, effect), GetIconColor(grid, index, effect));
    }

    private void AddDetailRow(Sprite icon, string text, Color color)
    {
        GameObject row = Instantiate(detailRowPrefab, detailContent);
        row.SetActive(true);

        var detail = row.GetComponent<GridDetailRow>();
        if (detail == null)
        {
            Debug.LogWarning("[GridPopup] Detail Row Prefab에 GridDetailRow가 없습니다. " +
                             "Tools/Grid/Setup Grid Prefabs 로 붙일 수 있습니다.");
            return;
        }

        detail.Setup(icon, text, color);
    }

    // ── 효과 판정 (정보 앱과 같은 규칙) ───────────────────────────────────────

    private static List<TileEffect> GetEffects(Grid grid, int index)
    {
        var list = new List<TileEffect>();
        if (grid == null) return list;

        if (grid.HasGoldSoil(index)) list.Add(TileEffect.GoldSoil);
        if (grid.TryGetFertilizerType(index, out _)) list.Add(TileEffect.Fertilizer);
        if (grid.HasPetBottle(index)) list.Add(TileEffect.PetBottle);
        if (grid.IsAffectedByChiliPepper(index)) list.Add(TileEffect.ChiliPepper);
        if (grid.IsPlantFrozen(index)) list.Add(TileEffect.Freezer);
        if (grid.IsAffectedBySprinkler(index)) list.Add(TileEffect.Sprinkler);
        if (grid.HasAbsorbFertilizer(index)) list.Add(TileEffect.AbsorbFertilizer);

        var curse = CurseManager.Instance;
        if (curse != null)
        {
            if (curse.IsFogged(index)) list.Add(TileEffect.Fog);
            if (curse.IsMushroom(index)) list.Add(TileEffect.Mushroom);
        }

        return list;
    }

    private static string GetDescription(Grid grid, int index, TileEffect effect)
    {
        switch (effect)
        {
            case TileEffect.GoldSoil: return "황금 흙: 모든 저항력 90% 고정, 이동 불가";
            case TileEffect.Fertilizer:
                return grid.TryGetFertilizerType(index, out var type)
                    ? $"비료: {type} 저항력 +5%"
                    : "비료";
            case TileEffect.PetBottle: return "페트병: 사망 1회 방지, 이동 불가";
            case TileEffect.ChiliPepper: return "매운 맛: 우성 형질 저항력 +20%";
            case TileEffect.Freezer: return "급속 냉각기: 식물 빙결 상태 (피해 면역)";
            case TileEffect.Sprinkler: return "스프링클러: 수분 공급 (비료 시너지 효과 포함)";
            case TileEffect.AbsorbFertilizer: return "저항력 흡수 비료: 주변 식물의 저항력을 지속 흡수";
            case TileEffect.Fog: return "안개: 이 땅 식물의 저항력을 확인할 수 없음";
            case TileEffect.Mushroom: return "버섯: 이번 웨이브에 이 땅의 식물이 피해를 입음";
            default: return effect.ToString();
        }
    }

    private Sprite GetIcon(TileEffect effect)
    {
        int i = (int)effect;
        return (effectIcons != null && i >= 0 && i < effectIcons.Length) ? effectIcons[i] : null;
    }

    /// <summary>비료만 대응 웨이브 색으로 물들인다. 나머지는 원래 색 그대로.</summary>
    private static Color GetIconColor(Grid grid, int index, TileEffect effect)
    {
        if (effect != TileEffect.Fertilizer) return Color.white;
        if (!grid.TryGetFertilizerType(index, out var type)) return Color.white;

        return WavePalette.GetColor(type);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    /// <summary>에디터에서 넣어 둔 예시 행도 함께 비운다.</summary>
    private static void Clear(Transform content)
    {
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);

            // Destroy는 프레임 끝에 처리되므로 먼저 떼어낸다.
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }
}
