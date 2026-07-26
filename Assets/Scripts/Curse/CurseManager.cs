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
    private readonly List<int> _foggedTiles = new();    // 안개 낀 타일(저항 확인 불가)
    private readonly List<int> _mushroomTiles = new();   // 버섯 타일(웨이브 시 피해)
    public bool IsFogged(int idx) => _foggedTiles.Contains(idx);
    public bool IsMushroom(int idx) => _mushroomTiles.Contains(idx);

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

    private int seasonInterval = 5; //지속형 저주 설정 단위 (계절 = 5일)
    private int remainingCurseDay = 0; // 지속형 저주 남은 기간
    public int RemainingCurseDay => remainingCurseDay;
    private int remainingTempCurseDay = 0; // 단발형 저주 남은 기간(다일 지속)
    public int RemainingTempCurseDay => remainingTempCurseDay;

    public CurseInstance currentTempCurse = null;
    public CurseInstance currentSeasonCurse = null;

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

    /// <summary>하루가 끝날 때(OnDayPassedForRequest) 단발형 만료 + 지속형 일수 차감.</summary>
    public void OnDayPassed()
    {
        RemoveCurse();
        UpdateSeasonalCurse();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9)) showCurseDebug = !showCurseDebug;
        if (Input.GetKeyDown(KeyCode.F10)) showForcePanel = !showForcePanel;
    }

    /// <summary>[디버그] 특정 저주를 지정 레벨로 즉시 적용(테스트용).</summary>
    public void DebugForceCurse(CurseScriptable data)
    {
        if (data == null) return;
        var inst = CreateInstanceById(data, debugForceLevel);
        if (inst == null) return;

        if (data.curseType == CurseType.Temporal)
        {
            currentTempCurse?.Deactivate();
            currentTempCurse = inst;
            var lv = data.GetLevel(debugForceLevel);
            remainingTempCurseDay = Mathf.Max(1, lv != null ? lv.days : 0);
            currentTempCurse.Activate();
            UpdateTempCurseUI();

            // 자유시간 시작 때만 도는 초기화를 디버그 강제 적용 시에도 태워준다.
            // (통신장애는 여기서 지속 시간이 정해지므로, 없으면 다음 날 자유시간까지 효과가 안 보임)
            if (PhoneManager.Instance != null && GameManager.Instance != null && GameManager.Instance.grid != null)
                PhoneManager.Instance.BeginEmpBlockIfActive(GameManager.Instance.grid.MaxBreedTimer);
        }
        else
        {
            currentSeasonCurse?.Deactivate();
            currentSeasonCurse = inst;
            remainingCurseDay = seasonInterval;
            currentSeasonCurse.Activate();
            UpdateCurseUI();
        }
        Debug.Log($"[Curse][강제] {data.title} 적용 (레벨 {debugForceLevel})");
    }

    /// <summary>[디버그] 현재 적용 중인 저주 모두 해제.</summary>
    public void DebugClearCurses()
    {
        currentTempCurse?.Deactivate();
        currentTempCurse = null;
        currentSeasonCurse?.Deactivate();
        currentSeasonCurse = null;
        remainingCurseDay = 0;
        remainingTempCurseDay = 0;
        UpdateCurseUI();
        UpdateTempCurseUI();
        Debug.Log("[Curse][강제] 모든 저주 해제");
    }

    // ───── 저주 시각/타일 액션 (CurseInstance.Activate에서 호출) ─────

    /// <summary>저주(안개): 무작위 count개 타일에 안개 생성(해당 타일 식물 저항 확인 불가).</summary>
    public void SpawnFog(int count)
    {
        ClearFog();
        SpawnOnRandomTiles(fog, count, _fogObjects, _foggedTiles);
        if (fog == null) Debug.LogWarning("[Curse] fog 프리팹 미할당 — 안개 시각효과 없음(저항 숨김은 적용됨)");
    }

    public void ClearFog()
    {
        foreach (var go in _fogObjects) if (go != null) Destroy(go);
        _fogObjects.Clear();
        _foggedTiles.Clear();
    }

    /// <summary>저주(버섯): 무작위 count개 타일에 버섯 생성. 피해는 웨이브 통과 시 <see cref="ResolveMushroomWave"/>에서.</summary>
    public void SpawnMushroom(int count)
    {
        ClearMushroom();
        SpawnOnRandomTiles(mushroom, count, _mushroomObjects, _mushroomTiles);
    }

    public void ClearMushroom()
    {
        foreach (var go in _mushroomObjects) if (go != null) Destroy(go);
        _mushroomObjects.Clear();
        _mushroomTiles.Clear();
    }

    /// <summary>저주(버섯): 웨이브가 지나갈 때 호출 — 버섯 타일 위 식물에 피해(페트병은 방어).</summary>
    public void ResolveMushroomWave()
    {
        if (_mushroomTiles.Count == 0) return;
        var grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (grid == null) return;

        foreach (int idx in _mushroomTiles)
            if (grid.plantGrid.TryGetValue(idx, out Plant p) && p != null)
                p.Die(DeathCause.Generic); // Die가 페트병을 자동 방어
    }

    private void SpawnOnRandomTiles(GameObject prefab, int count, List<GameObject> store, List<int> tileStore)
    {
        var grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (grid == null) return;

        int tiles = grid.GetMaxCol() * 4;
        var idxs = new List<int>();
        for (int i = 0; i < tiles; i++) idxs.Add(i);
        for (int i = idxs.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (idxs[i], idxs[j]) = (idxs[j], idxs[i]);
        }

        for (int i = 0; i < count && i < idxs.Count; i++)
        {
            int idx = idxs[i];
            tileStore.Add(idx);

            var soil = grid.GetSoilTransform(idx);
            if (soil != null && prefab != null)
                store.Add(Instantiate(prefab, soil.position, Quaternion.identity));
        }
    }

    // ── 디버그 오버레이: 현재 적용 중인 저주 + CurseState 수정자 표시 ──
    private GUIStyle _dbgStyle;
    private void OnGUI()
    {
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
            ? $"지속형: <color=#FF6E6E>{currentSeasonCurse.Data.title}</color>  ({remainingCurseDay}일 남음)"
            : "지속형: 없음");
        sb.AppendLine(currentTempCurse != null
            ? $"단발형: <color=#6EC1FF>{currentTempCurse.Data.title}</color>"
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
            if (c != null && GUILayout.Button($"{c.curseId}  {c.title}")) DebugForceCurse(c);

        GUILayout.Label("[지속형]");
        foreach (var c in seasonalCursePool)
            if (c != null && GUILayout.Button($"{c.curseId}  {c.title}")) DebugForceCurse(c);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static void AppendMod(System.Text.StringBuilder sb, bool active, string text)
    {
        if (active) sb.AppendLine($"· {text}");
    }

    /// <summary>
    /// stage = 오늘(밤 시점). 내일(stage+1)에 적용될 저주를 이 시점(전날 자유시간)에 선택·예고한다.
    /// - 단발형: 매 계절 3일차(3, 8, 13, …)에 확정 발동
    /// - 지속형: 매 계절 1일차(6, 11, 16, …)부터 적용, 첫 계절(1~5)은 미발생
    /// </summary>
    public void SelectCurse(int stage)
    {
        int tomorrow = stage + 1;

        if (tomorrow % seasonInterval == 3) SelectTemporalCurse();

        if (tomorrow > seasonInterval && tomorrow % seasonInterval == 1) SelectSeasonalCurse();
    }

    public void ApplyCurse()
    {
        ApplyTemporalCurse();
        ApplySeasonalCurse();
    }

    private void SelectTemporalCurse()
    {
        if (temporalCursePool == null || temporalCursePool.Count == 0) return;

        int currentStage = GameManager.Instance != null ? GameManager.Instance.stage : 1;
        var validCurses = temporalCursePool.Where(c => c != null && currentStage >= c.unlockStage).ToList();
        if (validCurses.Count == 0) return;

        var data = validCurses[UnityEngine.Random.Range(0, validCurses.Count)];
        int level = GetCurseLevel();
        currentTempCurse = CreateInstanceById(data, level);
        var lv = data.GetLevel(level);
        remainingTempCurseDay = Mathf.Max(1, lv != null ? lv.days : 0); // 다일 지속(기상이변·이중웨이브), 그 외는 1일
    }

    private void ApplyTemporalCurse()
    {
        if (currentTempCurse != null)
        {
            currentTempCurse.Activate();
            CodexProgress.Discover(CodexProgress.Category.Curse, currentTempCurse.Data.curseId); // 도감: 저주 발견
        }
        UpdateTempCurseUI();
    }

    private void SelectSeasonalCurse()
    {
        if (seasonalCursePool == null || seasonalCursePool.Count == 0) return;

        var data = seasonalCursePool[UnityEngine.Random.Range(0, seasonalCursePool.Count)];
        currentSeasonCurse = CreateInstanceById(data, GetCurseLevel());
        remainingCurseDay = seasonInterval;

        Debug.Log($"[Curse] 다음 계절 지속형 저주 예고: {currentSeasonCurse?.Data.title}");
        //TODO: 다음 지속형 저주에 대한 경고 UI(안전재난경보) 표시
    }

    private void ApplySeasonalCurse()
    {
        if (currentSeasonCurse != null)
        {
            currentSeasonCurse.Activate();
            CodexProgress.Discover(CodexProgress.Category.Curse, currentSeasonCurse.Data.curseId); // 도감: 저주 발견
        }
        UpdateCurseUI();
    }

    private void RemoveCurse()
    {
        if (currentTempCurse == null) return;

        remainingTempCurseDay--;
        if (remainingTempCurseDay <= 0)
        {
            currentTempCurse.Deactivate();
            currentTempCurse = null;
        }
        UpdateTempCurseUI();
    }

    private void UpdateSeasonalCurse()
    {
        if (currentSeasonCurse == null) return;

        remainingCurseDay--;
        Debug.Log($"[Curse] {currentSeasonCurse.Data.title} 저주 {remainingCurseDay}일 남음");

        if (remainingCurseDay <= 0)
        {
            currentSeasonCurse.Deactivate();
            currentSeasonCurse = null;
        }
        UpdateCurseUI();
    }

    private CurseInstance CreateInstanceById(CurseScriptable data, int level)
    {
        string typeCode = data.curseId;

        return typeCode switch
        {
            "101" => new ReverseCurse(data, level),
            "102" => new FogCurse(data, level),
            "103" => new ThiefCurse(data, level),
            "104" => new WaveBlindCurse(data, level),
            "105" => new MushroomCurse(data, level),
            "106" => new BreedMadnessCurse(data, level),
            "107" => new RearrangeCurse(data, level),
            "108" => new DoubleWaveCurse(data, level),
            "109" => new EMPCurse(data, level),

            "201" => new BugFestivalCurse(data, level),
            "202" => new MutationCurse(data, level),
            "203" => new RadiationCurse(data, level),
            "204" => new PollenLostCurse(data, level),
            "205" => new ShopMonopolyCurse(data, level),
            "206" => new InsomniaCurse(data, level),
            "207" => new SeedlessCurse(data, level),
            "208" => new HeavyFireCurse(data, level),

            _ => null
        };
    }

    /// <summary>이번 런의 저주 레벨(1~3). 새벽 저주 레벨을 따르며, 최소 1로 보정.</summary>
    private int GetCurseLevel()
    {
        int lv = DawnSystem.Current.curseLevel;
        return lv < 1 ? 1 : lv;
    }

    public string[] SaveCurseManager()
    {
        string[] cId = new string[2];

        cId[0] = currentTempCurse != null ? currentTempCurse.Data.curseId : null;
        cId[1] = currentSeasonCurse != null ? currentSeasonCurse.Data.curseId : null;

        return cId;
    }

    public void LoadCurseManager(SaveData saveData)
    {
        int level = GetCurseLevel();

        var scriptable = FindTempScriptableById(saveData.curseId[0]); //temp

        if (scriptable != null) currentTempCurse = CreateInstanceById(scriptable, level);
        else currentTempCurse = null;

        scriptable = FindSeasonScriptableById(saveData.curseId[1]); //season

        if (scriptable != null) currentSeasonCurse = CreateInstanceById(scriptable, level);
        else currentSeasonCurse = null;

        remainingCurseDay = saveData.remainSeasonCurseDay;
        remainingTempCurseDay = saveData.remainTempCurseDay;

        UpdateCurseUI();
        UpdateTempCurseUI();
    }

    private CurseScriptable FindTempScriptableById(string id)
    {
        return temporalCursePool.FirstOrDefault(p => p != null && p.curseId == id);
    }

    private CurseScriptable FindSeasonScriptableById(string id)
    {
        return seasonalCursePool.FirstOrDefault(p => p != null && p.curseId == id);
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
            string title = currentSeasonCurse.Data.title;
            string description = currentSeasonCurse.Data.description;

            string tooltipDesc = $"<b>{title}</b>\n{description}";

            if (UIManager.Instance != null && UIManager.Instance.Popup != null)
            {
                _activeCurseTooltip = UIManager.Instance.Popup.ShowCurseTooltip(seasonalTooltipPos, icon, tooltipDesc, remainingCurseDay);
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
            string title = currentTempCurse.Data.title;
            string description = currentTempCurse.Data.description;

            string tooltipDesc = $"<b>{title}</b>\n{description}";

            if (UIManager.Instance != null && UIManager.Instance.Popup != null)
            {
                _activeTempCurseTooltip = UIManager.Instance.Popup.ShowCurseTooltip(temporalTooltipPos, icon, tooltipDesc, remainingTempCurseDay);
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
