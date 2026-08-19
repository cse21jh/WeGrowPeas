using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 회상 메인 화면(전체화면) UI 골격을 씬에 생성하고 <see cref="RecallUIController"/>에 연결.
/// 기능 위주 스켈레톤 — 사진첩/앨범 스타일은 이후 인스펙터에서 리스타일.
/// (<see cref="CodexUISceneSetup"/>과 같은 구조)
/// </summary>
public static class RecallUISceneSetup
{
    /// <summary>
    /// 회상 캔버스의 정렬 순서. 일반 UI(0~99)보다 위, 로딩/전환 오버레이(1000 이상)보다 아래.
    /// </summary>
    private const int SortingOrder = 200;

    [MenuItem("Tools/Recall/Add Recall UI To Scene")]
    public static void Build()
    {
        // 기존 회상 UI 제거 — 씬 오브젝트만, 프리팹 에셋 제외
        foreach (var c in Resources.FindObjectsOfTypeAll<RecallUIController>())
            if (c != null && c.gameObject.scene.IsValid())
                Object.DestroyImmediate(c.gameObject);

        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ── 항상 켜진 루트(컨트롤러 + F7 Update) + 토글되는 시각 패널 ──
        // 캔버스를 새로 만들어 씬 최상위에 둔다. 기존 캔버스를 빌려 쓰면 어느 캔버스가
        // 잡힐지 보장이 없어(FindObjectOfType은 순서 미정의) 로딩 화면 같은 데 붙어버린다.
        var root = NewUI("RecallRoot", null);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CopyScalerSettings(root.AddComponent<CanvasScaler>());
        root.AddComponent<GraphicRaycaster>();

        var controller = root.AddComponent<RecallUIController>();

        var panel = NewUI("RecallPanel", root.transform);
        Stretch(panel);
        AddImage(panel, new Color(0.05f, 0.04f, 0.08f, 0.96f));

        // 자리는 전부 화면 비율로 잡는다 (고정 픽셀이면 캔버스가 커질 때 구석에 몰린다).

        // ── 타이틀 / 개수 ──
        var title = NewText("Title", panel.transform, "회상", 40, TextAlignmentOptions.Center);
        AnchorRel(title, 0.25f, 0.90f, 0.75f, 0.99f);

        var countText = NewText("CountText", panel.transform, "0 / 50", 20, TextAlignmentOptions.Left);
        AnchorRel(countText, 0.03f, 0.90f, 0.25f, 0.98f);

        // ── 기록 없음 안내 ──
        var emptyText = NewText("EmptyText", panel.transform, "아직 남은 기록이 없습니다.", 26, TextAlignmentOptions.Center);
        AnchorRel(emptyText, 0.03f, 0.03f, 0.97f, 0.88f);

        // ── 목록 (스크롤 + 그리드) ──
        var listRoot = NewUI("ListScroll", panel.transform);
        AnchorRel(listRoot, 0.03f, 0.03f, 0.97f, 0.88f);
        AddImage(listRoot, new Color(0f, 0f, 0f, 0.25f));
        var scroll = listRoot.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = NewUI("Viewport", listRoot.transform);
        Stretch(viewport);
        AddImage(viewport, new Color(0, 0, 0, 0.01f));
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = NewUI("Content", viewport.transform);
        Anchor(content, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        content.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);

        // 좌상단부터 최신순으로 깔린다 (RecallStore.GetEntries가 최신순으로 넘겨줌)
        // 카드 크기는 런타임에 목록 너비를 4열로 나눠 정한다 (RecallUIController.FitCardSize).
        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200, 150);
        grid.spacing = new Vector2(12, 12);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content.GetComponent<RectTransform>();

        // ── 닫기 ──
        var closeBtn = NewButton(panel.transform, "닫기");
        AnchorRel(closeBtn.gameObject, 0.88f, 0.90f, 0.97f, 0.98f);

        // ── 슬롯 프리팹 ──
        var slotPrefab = BuildSlotPrefab();

        // ── 상세 화면 (목록 위에 덮인다. 목록보다 뒤에 만들어야 위로 그려짐) ──
        var detailUI = BuildDetailPanel(root.transform);

        // ── 툴팁은 맨 마지막 = 모든 패널 위에 뜬다 ──
        var tooltip = BuildTooltip(root.transform);

        var timeline = detailUI.GetComponentInChildren<RecallTimelineUI>(true);
        if (timeline != null)
        {
            var tso = new SerializedObject(timeline);
            tso.FindProperty("tooltip").objectReferenceValue = tooltip;
            tso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 컨트롤러 필드 연결 ──
        var so = new SerializedObject(controller);
        so.FindProperty("recallPanel").objectReferenceValue = panel;
        so.FindProperty("listContainer").objectReferenceValue = content.transform;
        so.FindProperty("listSlotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("countText").objectReferenceValue = countText.GetComponent<TMP_Text>();
        so.FindProperty("emptyText").objectReferenceValue = emptyText.GetComponent<TMP_Text>();
        so.FindProperty("detailUI").objectReferenceValue = detailUI;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 닫기 버튼 → CloseRecall
        UnityEventTools.AddPersistentListener(closeBtn.onClick, controller.CloseRecall);

        panel.SetActive(false); // 루트는 켜진 채 시각 패널만 닫힘 → 닫힌 상태에서도 F7 동작

        AddEntryButtonIfPossible();

        Debug.Log("[Recall] 회상 UI 골격 생성 완료. F7로 열기/닫기.");
        Selection.activeGameObject = root;
    }

    /// <summary>
    /// 시작화면이면 버튼 목록에 "회상" 버튼을 만들어 연결한다.
    /// 위치·스타일은 다른 버튼에 맞춰 인스펙터에서 조정하면 된다.
    /// </summary>
    private static void AddEntryButtonIfPossible()
    {
        var clickEvent = Object.FindObjectOfType<UIClickEvent>();
        if (clickEvent == null) return; // 시작화면이 아니면 그냥 건너뛴다

        var so = new SerializedObject(clickEvent);
        var panelProp = so.FindProperty("buttonPanel");
        var buttonPanel = panelProp != null ? panelProp.objectReferenceValue as GameObject : null;
        if (buttonPanel == null)
        {
            Debug.Log("[Recall] 시작화면 버튼 패널을 찾지 못했습니다. 회상 버튼은 직접 만들어 " +
                      "UIClickEvent.OnClick_OpenRecall()에 연결하세요.");
            return;
        }

        if (buttonPanel.transform.Find("Btn_회상") != null)
        {
            Debug.Log("[Recall] 회상 버튼이 이미 있어 새로 만들지 않았습니다.");
            return;
        }

        var entryBtn = NewButton(buttonPanel.transform, "회상");

        // 버튼 패널에 레이아웃 그룹이 있을 수도, 없을 수도 있다. 어느 쪽이든 크기가 잡히도록
        // 실제 크기와 LayoutElement를 모두 준다 (안 그러면 0x0으로 안 보인다).
        entryBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 40);
        var le = entryBtn.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 140;
        le.preferredHeight = 40;

        UnityEventTools.AddPersistentListener(entryBtn.onClick, clickEvent.OnClick_OpenRecall);
        Debug.Log("[Recall] 시작화면에 회상 버튼을 추가했습니다. 위치/스타일은 인스펙터에서 맞춰 주세요.");
    }

    // ── 상세 화면 ──
    private static RecallDetailUI BuildDetailPanel(Transform parent)
    {
        var detailRoot = NewUI("RecallDetail", parent);
        Stretch(detailRoot);
        var detailUI = detailRoot.AddComponent<RecallDetailUI>();

        var panel = NewUI("DetailPanel", detailRoot.transform);
        Stretch(panel);
        AddImage(panel, new Color(0.04f, 0.03f, 0.06f, 0.98f));

        // 결과 화면 프리팹(Envelope)은 600x400 · 스케일 1.33으로 화면을 거의 채운다.
        // 그래서 자리를 깎지 않고 전체를 내주고, 헤더는 그 위에 겹쳐 띄운다.
        var contentRoot = NewUI("ContentRoot", panel.transform);
        Stretch(contentRoot);

        var headerBar = NewUI("HeaderBar", panel.transform);
        AnchorRel(headerBar, 0f, 0.88f, 1f, 1f);
        AddImage(headerBar, new Color(0, 0, 0, 0.55f)).raycastTarget = false;

        var headerText = NewText("HeaderText", headerBar.transform, "", 20, TextAlignmentOptions.TopLeft);
        AnchorRel(headerText, 0.02f, 0.05f, 0.72f, 0.95f);

        var backBtn = NewButton(panel.transform, "목록으로");
        AnchorRel(backBtn.gameObject, 0.87f, 0.90f, 0.98f, 0.98f);

        var timelineBtn = NewButton(panel.transform, "타임라인");
        AnchorRel(timelineBtn.gameObject, 0.75f, 0.90f, 0.86f, 0.98f);

        // 타임라인은 상세 위에 덮인다 (상세보다 뒤에 만들어야 위로 그려짐)
        var timelineUI = BuildTimelinePanel(detailRoot.transform);

        var so = new SerializedObject(detailUI);
        so.FindProperty("detailPanel").objectReferenceValue = panel;
        so.FindProperty("contentRoot").objectReferenceValue = contentRoot.transform;
        so.FindProperty("headerText").objectReferenceValue = headerText.GetComponent<TMP_Text>();
        so.FindProperty("timelineUI").objectReferenceValue = timelineUI;

        // 결과 화면 프리팹이 이미 뽑혀 있으면 물려준다 (없으면 런타임에 Resources에서 찾는다)
        var contentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EndingPrefabPath);
        if (contentPrefab != null)
            so.FindProperty("endingContentPrefab").objectReferenceValue = contentPrefab;

        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(backBtn.onClick, detailUI.Close);
        UnityEventTools.AddPersistentListener(timelineBtn.onClick, detailUI.OpenTimeline);

        panel.SetActive(false);
        return detailUI;
    }

    // ── 타임라인 화면 ──
    private static RecallTimelineUI BuildTimelinePanel(Transform parent)
    {
        var root = NewUI("RecallTimeline", parent);
        Stretch(root);
        var timelineUI = root.AddComponent<RecallTimelineUI>();

        var panel = NewUI("TimelinePanel", root.transform);
        Stretch(panel);
        AddImage(panel, new Color(0.03f, 0.03f, 0.05f, 1f));

        // 자리는 전부 화면 비율로 잡는다. 셀 크기는 런타임에 패널 크기에 맞춰 계산된다
        // (RecallTimelineUI.FitCellSize) — 여기 숫자는 시작값일 뿐이다.

        // ── 상단: 일차 + 닫기 ──
        var dayLabel = NewText("DayLabel", panel.transform, "", 30, TextAlignmentOptions.Left);
        AnchorRel(dayLabel, 0.03f, 0.91f, 0.45f, 0.99f);

        var closeBtn = NewButton(panel.transform, "닫기");
        AnchorRel(closeBtn.gameObject, 0.88f, 0.91f, 0.97f, 0.99f);

        // ── 좌: 밭 (세로 4칸 고정, 가로로 늘어남) ──
        var gridRoot = NewUI("GridPanel", panel.transform);
        AnchorRel(gridRoot, 0.03f, 0.22f, 0.48f, 0.88f);
        AddImage(gridRoot, new Color(0, 0, 0, 0.25f));

        var gridLayout = gridRoot.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(48, 48);
        gridLayout.spacing = new Vector2(6, 6);
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        // 밭 인덱스가 열 우선(col = idx / 4)이라 세로로 먼저 채운다.
        gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        gridLayout.constraintCount = 4;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // ── 우: 수치 ──
        var waveText = NewText("WaveText", panel.transform, "", 24, TextAlignmentOptions.TopLeft);
        AnchorRel(waveText, 0.52f, 0.82f, 0.97f, 0.89f);

        var goldText = NewText("GoldText", panel.transform, "", 30, TextAlignmentOptions.TopLeft);
        AnchorRel(goldText, 0.52f, 0.73f, 0.97f, 0.82f);

        var summaryText = NewText("SummaryText", panel.transform, "", 20, TextAlignmentOptions.TopLeft);
        AnchorRel(summaryText, 0.52f, 0.60f, 0.97f, 0.73f);

        // ── 우: 보유 아이템 ──
        var itemTitle = NewText("ItemTitle", panel.transform, "보유 아이템", 20, TextAlignmentOptions.Left);
        AnchorRel(itemTitle, 0.52f, 0.54f, 0.97f, 0.60f);

        var itemRoot = NewUI("ItemPanel", panel.transform);
        AnchorRel(itemRoot, 0.52f, 0.34f, 0.97f, 0.54f);
        AddImage(itemRoot, new Color(0, 0, 0, 0.25f));
        var itemLayout = itemRoot.AddComponent<GridLayoutGroup>();
        itemLayout.cellSize = new Vector2(44, 44);
        itemLayout.spacing = new Vector2(6, 6);
        itemLayout.padding = new RectOffset(8, 8, 8, 8);
        itemLayout.childAlignment = TextAnchor.UpperLeft;

        // ── 우: 저주 ──
        var curseTitle = NewText("CurseTitle", panel.transform, "저주", 20, TextAlignmentOptions.Left);
        AnchorRel(curseTitle, 0.52f, 0.28f, 0.97f, 0.34f);

        var curseRoot = NewUI("CursePanel", panel.transform);
        AnchorRel(curseRoot, 0.52f, 0.15f, 0.97f, 0.28f);
        AddImage(curseRoot, new Color(0, 0, 0, 0.25f));
        var curseLayout = curseRoot.AddComponent<GridLayoutGroup>();
        curseLayout.cellSize = new Vector2(44, 44);
        curseLayout.spacing = new Vector2(6, 6);
        curseLayout.padding = new RectOffset(8, 8, 8, 8);
        curseLayout.childAlignment = TextAnchor.UpperLeft;

        var curseEmpty = NewText("CurseEmptyText", curseRoot.transform, "없음", 18, TextAlignmentOptions.Left);
        curseEmpty.GetComponent<TMP_Text>().color = new Color(1, 1, 1, 0.4f);

        // ── 하단: 슬라이더 ──
        // 설명은 커서를 따라다니는 툴팁이 맡는다(RecallTooltip). 하단에 고정 칸을 두지 않는다.
        var slider = BuildSlider(panel.transform);
        AnchorRel(slider.gameObject, 0.03f, 0.04f, 0.97f, 0.10f);

        // ── 아이콘 슬롯 프리팹 ──
        var iconPrefab = BuildIconSlotPrefab();

        var so = new SerializedObject(timelineUI);
        so.FindProperty("timelinePanel").objectReferenceValue = panel;
        so.FindProperty("daySlider").objectReferenceValue = slider;
        so.FindProperty("dayLabel").objectReferenceValue = dayLabel.GetComponent<TMP_Text>();
        so.FindProperty("gridContainer").objectReferenceValue = gridRoot.transform;
        so.FindProperty("gridLayout").objectReferenceValue = gridLayout;
        so.FindProperty("itemContainer").objectReferenceValue = itemRoot.transform;
        so.FindProperty("curseContainer").objectReferenceValue = curseRoot.transform;
        so.FindProperty("curseEmptyText").objectReferenceValue = curseEmpty.GetComponent<TMP_Text>();
        so.FindProperty("iconSlotPrefab").objectReferenceValue = iconPrefab;
        so.FindProperty("waveText").objectReferenceValue = waveText.GetComponent<TMP_Text>();
        so.FindProperty("goldText").objectReferenceValue = goldText.GetComponent<TMP_Text>();
        so.FindProperty("summaryText").objectReferenceValue = summaryText.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(closeBtn.onClick, timelineUI.Close);

        panel.SetActive(false);
        return timelineUI;
    }

    // ── 마우스를 따라다니는 설명 툴팁 ──
    private static RecallTooltip BuildTooltip(Transform parent)
    {
        var root = NewUI("RecallTooltip", parent);
        Stretch(root);
        var tooltip = root.AddComponent<RecallTooltip>();

        var panel = NewUI("TooltipPanel", root.transform);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // 캔버스 중심 기준으로 좌표 계산
        rt.pivot = new Vector2(0f, 1f);                        // 커서의 오른쪽 아래로 펼쳐진다
        AddImage(panel, new Color(0.02f, 0.02f, 0.04f, 0.92f)).raycastTarget = false;

        // 글자 길이에 맞춰 세로로 늘어나고, 가로는 아래 preferredWidth에서 멈춘다.
        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = NewText("TooltipText", panel.transform, "", 18, TextAlignmentOptions.TopLeft);
        var le = textGo.AddComponent<LayoutElement>();
        le.preferredWidth = 360; // 이 폭을 넘으면 줄바꿈

        var so = new SerializedObject(tooltip);
        so.FindProperty("panel").objectReferenceValue = rt;
        so.FindProperty("text").objectReferenceValue = textGo.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return tooltip;
    }

    /// <summary>일차 이동용 슬라이더. 기본 Slider 프리팹이 없으므로 직접 조립한다.</summary>
    private static Slider BuildSlider(Transform parent)
    {
        var go = NewUI("DaySlider", parent);
        var slider = go.AddComponent<Slider>();

        var background = NewUI("Background", go.transform);
        Anchor(background, new Vector2(0, 0.35f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
        AddImage(background, new Color(1, 1, 1, 0.15f));

        // 손잡이는 넉넉히 잡는다. 너무 얇으면 큰 화면에서 집기 어렵다.
        const float handleWidth = 28f;

        var fillArea = NewUI("Fill Area", go.transform);
        Anchor(fillArea, new Vector2(0, 0.35f), new Vector2(1, 0.65f), new Vector2(handleWidth / 2f, 0), new Vector2(-handleWidth / 2f, 0));
        var fill = NewUI("Fill", fillArea.transform);
        Anchor(fill, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(handleWidth, 0));
        var fillImg = AddImage(fill, new Color(0.9f, 0.8f, 0.35f, 0.8f));

        var handleArea = NewUI("Handle Slide Area", go.transform);
        Anchor(handleArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(handleWidth / 2f, 0), new Vector2(-handleWidth / 2f, 0));
        var handle = NewUI("Handle", handleArea.transform);
        Anchor(handle, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(handleWidth, 0));
        var handleImg = AddImage(handle, Color.white);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = true;
        slider.minValue = 0;
        slider.maxValue = 0;

        return slider;
    }

    private static RecallIconSlot BuildIconSlotPrefab()
    {
        const string dir = "Assets/Resources/Prefabs/Recall";
        EnsureFolder(dir);

        var slot = NewUI("RecallIconSlot", null);
        var bg = AddImage(slot, new Color(1, 1, 1, 0.06f));

        var icon = NewUI("Icon", slot.transform);
        AnchorRel(icon, 0.08f, 0.08f, 0.92f, 0.92f);
        var iconImg = AddImage(icon, Color.white);
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        var countText = NewText("CountText", slot.transform, "", 16, TextAlignmentOptions.BottomRight);
        AnchorRel(countText, 0f, 0f, 0.96f, 1f);

        var comp = slot.AddComponent<RecallIconSlot>();
        var sso = new SerializedObject(comp);
        sso.FindProperty("background").objectReferenceValue = bg;
        sso.FindProperty("icon").objectReferenceValue = iconImg;
        sso.FindProperty("countText").objectReferenceValue = countText.GetComponent<TMP_Text>();
        sso.ApplyModifiedPropertiesWithoutUndo();

        string path = $"{dir}/RecallIconSlot.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(slot, path);
        Object.DestroyImmediate(slot);
        return prefab.GetComponent<RecallIconSlot>();
    }

    // ── 결과 화면 프리팹 추출 ──

    private const string EndingPrefabPath = "Assets/Resources/Prefabs/Recall/RecallEndingContent.prefab";

    /// <summary>
    /// 열려 있는 씬(GameOverScene)의 결과 화면(Envelope)을 프리팹으로 뽑는다.
    /// 회상 상세가 이 프리팹을 그대로 띄우므로, 엔딩 화면을 손보면 다시 뽑기만 하면 된다.
    /// </summary>
    [MenuItem("Tools/Recall/Extract Ending UI Prefab")]
    public static void ExtractEndingPrefab()
    {
        var record = Object.FindObjectOfType<UIGameRecord>(true);
        if (record == null)
        {
            EditorUtility.DisplayDialog("회상",
                "이 씬에서 UIGameRecord를 찾지 못했습니다.\nGameOverScene을 연 뒤 다시 실행하세요.", "확인");
            return;
        }

        EnsureFolder(System.IO.Path.GetDirectoryName(EndingPrefabPath).Replace("\\", "/"));

        // 뽑아낸 뒤 씬의 원본이 이 프리팹의 인스턴스가 될 수 있다(에디터 버전/조작에 따라 다름).
        // 그 편이 오히려 낫다 — 프리팹을 고치면 엔딩 화면과 회상이 함께 바뀌므로 한 벌로 유지된다.
        // 다만 씬이 바뀌었을 수 있으니 실행 후 GameOverScene 저장 여부를 확인할 것.
        var prefab = PrefabUtility.SaveAsPrefabAsset(record.gameObject, EndingPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[Recall] 결과 화면 프리팹 저장에 실패했습니다.");
            return;
        }

        // 씬에 회상 UI가 이미 있으면 새 프리팹을 물려준다.
        foreach (var detail in Resources.FindObjectsOfTypeAll<RecallDetailUI>())
        {
            if (detail == null || !detail.gameObject.scene.IsValid()) continue;

            var so = new SerializedObject(detail);
            so.FindProperty("endingContentPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log($"[Recall] 결과 화면 프리팹을 뽑았습니다: {EndingPrefabPath} (원본 '{record.gameObject.name}')");
        Selection.activeObject = prefab;
    }

    // ── 슬롯 프리팹 생성 ──
    private static RecallListSlot BuildSlotPrefab()
    {
        const string dir = "Assets/Resources/Prefabs/Recall";
        EnsureFolder(dir);

        var slot = NewUI("RecallListSlot", null);
        var btn = slot.AddComponent<Button>();
        var bg = slot.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.06f);
        btn.targetGraphic = bg;

        // 카드 크기는 런타임에 정해지므로(RecallUIController.FitCardSize) 안쪽도 비율로 잡는다.

        // 사진 (위쪽 대부분)
        var photoGo = NewUI("Photo", slot.transform);
        AnchorRel(photoGo, 0.02f, 0.28f, 0.98f, 0.98f);
        var photo = photoGo.AddComponent<RawImage>();
        photo.raycastTarget = false;

        var noPhoto = NewText("NoPhotoMark", slot.transform, "사진 없음", 16, TextAlignmentOptions.Center);
        AnchorRel(noPhoto, 0.02f, 0.28f, 0.98f, 0.98f);
        noPhoto.GetComponent<TMP_Text>().color = new Color(1, 1, 1, 0.4f);

        // 날짜 / 정보 (아래쪽)
        var dateLabel = NewText("DateLabel", slot.transform, "", 15, TextAlignmentOptions.Left);
        AnchorRel(dateLabel, 0.04f, 0.15f, 0.96f, 0.27f);

        var infoLabel = NewText("InfoLabel", slot.transform, "", 17, TextAlignmentOptions.Left);
        AnchorRel(infoLabel, 0.04f, 0.02f, 0.96f, 0.15f);

        var comp = slot.AddComponent<RecallListSlot>();
        var sso = new SerializedObject(comp);
        sso.FindProperty("button").objectReferenceValue = btn;
        sso.FindProperty("photo").objectReferenceValue = photo;
        sso.FindProperty("noPhotoMark").objectReferenceValue = noPhoto;
        sso.FindProperty("dateLabel").objectReferenceValue = dateLabel.GetComponent<TMP_Text>();
        sso.FindProperty("infoLabel").objectReferenceValue = infoLabel.GetComponent<TMP_Text>();
        sso.ApplyModifiedPropertiesWithoutUndo();

        string path = $"{dir}/RecallListSlot.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(slot, path);
        Object.DestroyImmediate(slot);
        return prefab.GetComponent<RecallListSlot>();
    }

    /// <summary>
    /// 씬에 이미 있는 캔버스의 스케일 설정을 그대로 따라간다.
    /// 기준 해상도가 다르면 회상 화면만 크기가 따로 놀기 때문이다.
    /// </summary>
    private static void CopyScalerSettings(CanvasScaler target)
    {
        target.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        target.referenceResolution = new Vector2(800, 600);
        target.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        target.matchWidthOrHeight = 0f;

        foreach (var scaler in Object.FindObjectsOfType<CanvasScaler>())
        {
            if (scaler == null || scaler == target) continue;
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;

            target.referenceResolution = scaler.referenceResolution;
            target.screenMatchMode = scaler.screenMatchMode;
            target.matchWidthOrHeight = scaler.matchWidthOrHeight;
            return;
        }
    }

    // ── 헬퍼 ──
    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white; t.enableWordWrapping = true;
        t.raycastTarget = false;
        return go;
    }

    private static Button NewButton(Transform parent, string label)
    {
        var go = NewUI("Btn_" + label, parent);
        var img = AddImage(go, new Color(1, 1, 1, 0.12f));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var t = NewText("Label", go.transform, label, 22, TextAlignmentOptions.Center);
        Stretch(t);
        return btn;
    }

    private static Image AddImage(GameObject go, Color c)
    {
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Anchor(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
    }

    /// <summary>
    /// 화면 비율로 자리를 잡는다(0~1). 고정 픽셀 여백과 달리 캔버스 크기가 달라져도
    /// 같은 비율을 유지한다 — 해상도나 기준 해상도가 바뀌어도 배치가 무너지지 않는다.
    /// </summary>
    private static void AnchorRel(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
