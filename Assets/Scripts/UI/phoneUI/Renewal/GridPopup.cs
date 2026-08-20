using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
/// </summary>
public class GridPopup : MonoBehaviour
{
    /// <summary>밭 세로 칸 수. Grid의 인덱스 계산(col = idx / 4)에 묶여 있다.</summary>
    private const int FarmRows = 4;

    /// <summary>칸 하나에 보여줄 아이콘 자리. GridPrefab의 자식 이름과 같아야 한다.</summary>
    private static readonly string[] IconSlotNames = { "Icon_1", "Icon_2", "Icon_2 (1)" };

    [Header("밭 (GridPanel > Scroll View > Viewport > Content)")]
    [SerializeField] private Transform gridContent;
    [Tooltip("세로 한 줄을 담는 그릇 (GridLinePrefab)")]
    [SerializeField] private GameObject gridLinePrefab;
    [Tooltip("칸 하나 (GridPrefab)")]
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

    private readonly List<GameObject> cells = new List<GameObject>();
    private int selectedIndex = -1;

    public void Open()
    {
        if (gameObject.activeSelf) Refresh();
        else gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    /// <summary>구독해 둔 Grid. 껐다 켜는 사이에 바뀔 수 있어 따로 들고 있는다.</summary>
    private Grid subscribedGrid;

    private void OnEnable()
    {
        // 식물이 심기고 뽑히고 옮겨질 때마다 밭이 바뀐다.
        // OnShopBought는 "결제" 시점이라 설치형 아이템(고추·비료 등)은 아직 놓기 전이다.
        // 실제로 놓인 뒤를 잡으려면 Grid 쪽 알림을 들어야 한다.
        subscribedGrid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (subscribedGrid != null) subscribedGrid.OnGridStateChanged += Refresh;

        // 저주 만료·빙결 해제처럼 밭 밖에서 바뀌는 것들.
        GameEvents.OnDayStarted += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (subscribedGrid != null) subscribedGrid.OnGridStateChanged -= Refresh;
        subscribedGrid = null;

        GameEvents.OnDayStarted -= Refresh;
    }

    /// <summary>밭을 다시 깔고, 고른 칸이 있으면 상세도 다시 채운다.</summary>
    public void Refresh()
    {
        BuildGrid();
        ShowDetail(selectedIndex);
    }

    // ── 밭 ────────────────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        Clear(gridContent);
        cells.Clear();

        Grid grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (grid == null || gridContent == null || gridCellPrefab == null) return;

        int maxCol = grid.GetMaxCol();

        for (int col = 0; col < maxCol; col++)
        {
            // 줄 그릇이 없으면 칸을 Content에 바로 넣는다(레이아웃은 Content가 알아서).
            Transform parent = gridContent;
            if (gridLinePrefab != null)
            {
                GameObject line = Instantiate(gridLinePrefab, gridContent);
                line.SetActive(true);
                parent = line.transform;
            }

            for (int row = 0; row < FarmRows; row++)
            {
                int index = col * FarmRows + row; // Grid와 같은 열 우선 인덱스
                BuildCell(grid, index, parent);
            }
        }
    }

    private void BuildCell(Grid grid, int index, Transform parent)
    {
        GameObject cell = Instantiate(gridCellPrefab, parent);
        cell.SetActive(true);

        while (cells.Count <= index) cells.Add(null);
        cells[index] = cell;

        List<TileEffect> effects = GetEffects(grid, index);

        // 아이콘 자리는 세 개뿐이다. 넘치면 개수로 표시한다.
        for (int i = 0; i < IconSlotNames.Length; i++)
        {
            Transform slot = FindDeep(cell.transform, IconSlotNames[i]);
            var image = slot != null ? slot.GetComponent<Image>() : null;
            if (image == null) continue;

            bool show = i < effects.Count;
            image.gameObject.SetActive(show);

            if (!show) continue;

            image.sprite = GetIcon(effects[i]);
            image.color = GetIconColor(grid, index, effects[i]);
        }

        int overflow = effects.Count - IconSlotNames.Length;
        SetText(cell, "AmountText", overflow > 0 ? $"{overflow}+" : "");
        SetText(cell, "AmountText_Underlay", overflow > 0 ? $"{overflow}+" : "");

        SetSelected(cell, index == selectedIndex);

        var button = cell.GetComponent<Button>();
        if (button != null)
        {
            int captured = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectCell(captured));
        }
    }

    private void SelectCell(int index)
    {
        selectedIndex = index;

        for (int i = 0; i < cells.Count; i++)
            if (cells[i] != null) SetSelected(cells[i], i == index);

        ShowDetail(index);
    }

    private static void SetSelected(GameObject cell, bool on)
    {
        Transform frame = FindDeep(cell.transform, "SelectedFrame");
        if (frame != null) frame.gameObject.SetActive(on);
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

        Transform imageT = FindDeep(row.transform, "GridDetailImage");
        var image = imageT != null ? imageT.GetComponent<Image>() : null;
        if (image != null)
        {
            image.sprite = icon;
            image.color = color;
            image.enabled = icon != null;
        }

        SetText(row, "GridDetailText", text);
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

    private static void SetText(GameObject root, string childName, string value)
    {
        Transform t = FindDeep(root.transform, childName);
        var text = t != null ? t.GetComponent<TMP_Text>() : null;
        if (text == null) return;

        text.text = value;
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

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
