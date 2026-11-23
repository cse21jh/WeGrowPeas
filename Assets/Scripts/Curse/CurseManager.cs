using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CurseManager : Singleton<CurseManager>
{
    [SerializeField] private List<CurseScriptable> temporalCursePool = new();
    [SerializeField] private List<CurseScriptable> seasonalCursePool = new();

    [Header("Curse Related Objects")]
    [SerializeField] private GameObject fog;
    [Tooltip("버섯 저주용 프리팹 (미할당 시 시각효과 없이 피해만 적용)")]
    [SerializeField] private GameObject mushroom;

    private readonly List<GameObject> _fogObjects = new();
    private readonly List<GameObject> _mushroomObjects = new();

    /// <summary>걸린 저주와 선택 규칙. 이 컴포넌트는 안내 UI와 칸 표시를 더한다.</summary>
    public CurseBoard Board => board ??= new CurseBoard(temporalCursePool, seasonalCursePool, new GridCurseWorld(RefreshTiles));
    private CurseBoard board;

    public bool IsFogged(int idx) => Board.FogTiles.Contains(idx);
    public bool IsMushroom(int idx) => Board.MushroomTiles.Contains(idx);

    [Header("Debug")]
    [Tooltip("좌상단에 현재 적용 중인 저주 오버레이 표시 (F9로 토글)")]
    [SerializeField] private bool showCurseDebug = true;
    [Tooltip("특정 저주 강제 적용 패널 (F10으로 토글)")]
    [SerializeField] private bool showForcePanel = false;
    [Header("Curse UI Positioning")]
    [SerializeField] private Vector2 seasonalTooltipPos = new Vector2(0f, -70f);
    [SerializeField] private Vector2 temporalTooltipPos = new Vector2(0f, -120f);

    [Tooltip("강제 적용 시 사용할 저주 레벨")]
    [Range(1, 3)]
    [SerializeField] private int debugForceLevel = 1;

    public int RemainingCurseDay => Board.RemainingSeasonalDays;
    public int RemainingTempCurseDay => Board.RemainingTemporalDays;
    public CurseInstance currentTempCurse => Board.Temporal;
    public CurseInstance currentSeasonCurse => Board.Seasonal;

    private CurseTooltipUI _activeCurseTooltip;
    private CurseTooltipUI _activeTempCurseTooltip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateCurseUI();
        UpdateTempCurseUI();
    }
    private void OnEnable()
    {
        GameEvents.OnDayPassedForRequest += OnDayPassed;
    }

    private void OnDisable()
    {
        GameEvents.OnDayPassedForRequest -= OnDayPassed;
    }

    /// <summary>하루가 끝날 때 남은 일수를 다시 그린다. 일수는 <see cref="DayFlow"/>가 줄인다.</summary>
    private void OnDayPassed()
    {
        UpdateTempCurseUI();
        UpdateCurseUI();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!DebugPanels.Enabled) return;
        if (Input.GetKeyDown(KeyCode.F9)) showCurseDebug = !showCurseDebug;
        if (Input.GetKeyDown(KeyCode.F10)) showForcePanel = !showForcePanel;
#endif
    }

    /// <summary>[디버그] 특정 저주를 지정 레벨로 즉시 적용(테스트용).</summary>
    public void DebugForceCurse(CurseScriptable data)
    {
        if (data == null) return;

        bool temporal = data.curseType == CurseType.Temporal;
        (temporal ? currentTempCurse : currentSeasonCurse)?.Deactivate();
        Board.Force(data, debugForceLevel)?.Activate();
        UpdateTempCurseUI();
        UpdateCurseUI();

        // 자유시간 시작 때만 도는 초기화를 디버그 강제 적용 시에도 태워준다.
        // (통신장애는 여기서 지속 시간이 정해지므로, 없으면 다음 날 자유시간까지 효과가 안 보임)
        if (temporal && PhoneManager.Instance != null && GameManager.Instance != null && GameManager.Instance.grid != null)
            PhoneManager.Instance.BeginEmpBlockIfActive(GameManager.Instance.grid.Breeder.MaxBreedTimer);
        Debug.Log($"[Curse][강제] {data.name} 적용 (레벨 {debugForceLevel})");
    }

    /// <summary>[디버그] 현재 적용 중인 저주 모두 해제.</summary>
    public void DebugClearCurses()
    {
        Board.Clear();
        UpdateCurseUI();
        UpdateTempCurseUI();
        Debug.Log("[Curse][강제] 모든 저주 해제");
    }

    /// <summary>안개와 버섯 칸의 표시를 지금 상태에 맞춘다.</summary>
    private void RefreshTiles()
    {
        ShowTiles(fog, Board.FogTiles, _fogObjects);
        ShowTiles(mushroom, Board.MushroomTiles, _mushroomObjects);
    }

    private static void ShowTiles(GameObject prefab, List<int> tiles, List<GameObject> objects)
    {
        foreach (var go in objects) if (go != null) Destroy(go);
        objects.Clear();
        if (prefab == null) return;

        foreach (int idx in tiles)
        {
            Transform soil = GameManager.Instance.grid.GetSoilTransform(idx);
            if (soil != null) objects.Add(Instantiate(prefab, soil.position, Quaternion.identity));
        }
    }

    // ── 디버그 오버레이: 현재 적용 중인 저주 + CurseState 수정자 표시 ──
    private GUIStyle _dbgStyle;
    private void OnGUI()
    {
        if (!DebugPanels.Enabled) return; // 릴리즈 빌드에선 항상 false
        if (!showCurseDebug) return;

        if (_dbgStyle == null)
        {
            _dbgStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = false };
            _dbgStyle.normal.textColor = Color.white;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>── 저주 상태 (F9) ──</b>");
        sb.AppendLine($"저주 레벨: {DawnSystem.Current.curseLevel}");
        sb.AppendLine(currentSeasonCurse != null
            ? $"지속형: <color=#FF6E6E>{currentSeasonCurse.Data.LocalizedTitle}</color>  ({RemainingCurseDay}일 남음)"
            : "지속형: 없음");
        sb.AppendLine(currentTempCurse != null
            ? $"단발형: <color=#6EC1FF>{currentTempCurse.Data.LocalizedTitle}</color>"
            : "단발형: 없음");

        sb.AppendLine("<b>[활성 수정자]</b>");
        AppendMod(sb, CurseState.RadiationDecayPercent > 0, $"방사능 -{CurseState.RadiationDecayPercent}%p/일");
        AppendMod(sb, CurseState.MutationAddPercent > 0, $"돌연변이 변종 +{CurseState.MutationAddPercent}%p");
        AppendMod(sb, CurseState.PollenLostRatio > 0, $"꽃가루실종 {CurseState.PollenLostRatio * 100f:0}% 교배불가");
        AppendMod(sb, CurseState.ShopMonopoly, $"독점시장 {CurseState.ShopPriceMinMul * 100f:0}~{CurseState.ShopPriceMaxMul * 100f:0}%");
        AppendMod(sb, CurseState.InsomniaFreeTimeRatio < 1f, $"불면증 자유시간 {CurseState.InsomniaFreeTimeRatio * 100f:0}%");
        AppendMod(sb, CurseState.SeedlessFailPercent > 0, $"씨없는수박 실패 {CurseState.SeedlessFailPercent}%");
        AppendMod(sb, CurseState.BugFestival, $"벌레대발생 딜레이 -{CurseState.BugFestivalDelayReduce}s, 2마리");
        AppendMod(sb, CurseState.HeavyFire, $"집중포화 저항 -{CurseState.HeavyFireExtraDecayPercent}%p");
        AppendMod(sb, CurseState.ReversePercent > 0, $"반란 ±{CurseState.ReversePercent}%p");
        AppendMod(sb, CurseState.WaveBlind, "기상이변 웨이브 확인불가");
        AppendMod(sb, CurseState.DoubleWave, "이중 웨이브");
        AppendMod(sb, CurseState.EmpBlockRatio > 0, $"통신장애 낮 {CurseState.EmpBlockRatio * 100f:0}%");
        AppendMod(sb, CurseState.BreedMadnessPercent > 0, $"광란 랜덤교배 {CurseState.BreedMadnessPercent}%");

        var box = new Rect(5, 5, 470, 30 + 18 * (sb.ToString().Split('\n').Length));
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = prev;

        GUI.Label(new Rect(12, 10, 460, box.height), sb.ToString(), _dbgStyle);

        if (showForcePanel)
            DrawForcePanel(box.height + 15);
    }

    private Vector2 _forceScroll;
    private void DrawForcePanel(float top)
    {
        GUILayout.BeginArea(new Rect(10, top, 260, Screen.height - top - 10), GUI.skin.box);
        GUILayout.Label("── 강제 저주 (F10) ──");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"레벨: {debugForceLevel}", GUILayout.Width(70));
        if (GUILayout.Button("-")) debugForceLevel = Mathf.Max(1, debugForceLevel - 1);
        if (GUILayout.Button("+")) debugForceLevel = Mathf.Min(3, debugForceLevel + 1);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("● 모두 해제")) DebugClearCurses();

        _forceScroll = GUILayout.BeginScrollView(_forceScroll);

        GUILayout.Label("[단발형]");
        foreach (var c in temporalCursePool)
            if (c != null && GUILayout.Button($"{c.curseId}  {c.LocalizedTitle}")) DebugForceCurse(c);

        GUILayout.Label("[지속형]");
        foreach (var c in seasonalCursePool)
            if (c != null && GUILayout.Button($"{c.curseId}  {c.LocalizedTitle}")) DebugForceCurse(c);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static void AppendMod(System.Text.StringBuilder sb, bool active, string text)
    {
        if (active) sb.AppendLine($"· {text}");
    }

    /// <summary>저주가 발동한 뒤의 화면 처리.</summary>
    public void OnCurseApplied()
    {
        // 도감: 저주 발견
        if (currentTempCurse != null) CodexProgress.Discover(CodexProgress.Category.Curse, currentTempCurse.Data.curseId);
        if (currentSeasonCurse != null) CodexProgress.Discover(CodexProgress.Category.Curse, currentSeasonCurse.Data.curseId);
        UpdateTempCurseUI();
        UpdateCurseUI();
    }

    /// <summary>진행 중인 저주를 저장 데이터에 담는다. <see cref="LoadCurseManager"/>와 짝.</summary>
    public void SaveCurseManager(CurseSave save) => Board.Save(save);

    public void LoadCurseManager(CurseSave saveData)
    {
        CurseState.ResetAll(); // 다른 판에서 켜진 저주 수치가 남지 않게. 걸린 저주는 낮이 시작될 때 다시 발동한다
        Board.Load(saveData);
        UpdateCurseUI();
        UpdateTempCurseUI();
    }

    public void UpdateCurseUI()
    {
        if (_activeCurseTooltip != null)
        {
            try
            {
                _activeCurseTooltip.Close();
            }
            catch (System.Exception) { /* ignored */ }
            _activeCurseTooltip = null;
        }

        if (currentSeasonCurse != null && currentSeasonCurse.Data != null)
        {
            Sprite icon = currentSeasonCurse.Data.icon;
            string title = currentSeasonCurse.Data.LocalizedTitle;
            string description = currentSeasonCurse.Data.LocalizedDescription;

            string tooltipDesc = $"<b>{title}</b>\n{description}";

            if (UIManager.Instance != null && UIManager.Instance.Popup != null)
            {
                _activeCurseTooltip = UIManager.Instance.Popup.ShowCurseTooltip(seasonalTooltipPos, icon, tooltipDesc, RemainingCurseDay);
                if (_activeCurseTooltip != null)
                {
                    RectTransform rect = _activeCurseTooltip.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0f, 1f);
                        rect.anchorMax = new Vector2(0f, 1f);
                        rect.pivot = new Vector2(0f, 1f);
                        rect.anchoredPosition = seasonalTooltipPos;
                    }
                }
            }
        }
    }

    public void UpdateTempCurseUI()
    {
        if (_activeTempCurseTooltip != null)
        {
            try
            {
                _activeTempCurseTooltip.Close();
            }
            catch (System.Exception) { /* ignored */ }
            _activeTempCurseTooltip = null;
        }

        if (currentTempCurse != null && currentTempCurse.Data != null)
        {
            Sprite icon = currentTempCurse.Data.icon;
            string title = currentTempCurse.Data.LocalizedTitle;
            string description = currentTempCurse.Data.LocalizedDescription;

            string tooltipDesc = $"<b>{title}</b>\n{description}";

            if (UIManager.Instance != null && UIManager.Instance.Popup != null)
            {
                _activeTempCurseTooltip = UIManager.Instance.Popup.ShowCurseTooltip(temporalTooltipPos, icon, tooltipDesc, RemainingTempCurseDay);
                if (_activeTempCurseTooltip != null)
                {
                    RectTransform rect = _activeTempCurseTooltip.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0f, 1f);
                        rect.anchorMax = new Vector2(0f, 1f);
                        rect.pivot = new Vector2(0f, 1f);
                        rect.anchoredPosition = temporalTooltipPos;
                    }
                }
            }
        }
    }
}
