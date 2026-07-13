using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 도감(전체화면) UI 골격을 씬에 생성하고 <see cref="CodexUIController"/>에 연결.
/// 기능 위주 스켈레톤 — 책/스프링노트 스타일은 이후 인스펙터에서 리스타일.
/// </summary>
public static class CodexUISceneSetup
{
    [MenuItem("Tools/Codex/Add Codex UI To Scene")]
    public static void Build()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // 기존 도감(구/신 구조 모두) 제거 — 씬 오브젝트만, 프리팹 에셋 제외
        foreach (var c in Resources.FindObjectsOfTypeAll<CodexUIController>())
            if (c != null && c.gameObject.scene.IsValid())
                Object.DestroyImmediate(c.gameObject);

        // ── 항상 켜진 루트(컨트롤러 + F8 Update) + 토글되는 시각 패널 ──
        var root = NewUI("CodexRoot", canvas.transform);
        Stretch(root);
        var controller = root.AddComponent<CodexUIController>();

        var panel = NewUI("CodexPanel", root.transform);
        Stretch(panel);
        AddImage(panel, new Color(0.05f, 0.04f, 0.08f, 0.96f));

        // ── 타이틀 ──
        var title = NewText("Title", panel.transform, "도감", 40, TextAlignmentOptions.Center);
        Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), new Vector2(0, -10));

        // ── 카테고리 버튼 행 ──
        var catRow = NewUI("CategoryRow", panel.transform);
        Anchor(catRow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -140), new Vector2(-20, -80));
        var hl = catRow.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 10; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandHeight = true;

        string[] catNames = { "아이템", "식물", "저주", "벌레" };
        var catButtons = new Button[4];
        for (int i = 0; i < 4; i++)
            catButtons[i] = NewButton(catRow.transform, catNames[i]);

        // ── 좌측 목록 (ScrollRect) ──
        var listRoot = NewUI("ListScroll", panel.transform);
        Anchor(listRoot, new Vector2(0, 0), new Vector2(0.42f, 1), new Vector2(20, 50), new Vector2(-10, -150));
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
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.childForceExpandHeight = false; vlg.childControlHeight = true; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content.GetComponent<RectTransform>();

        // ── 우측 상세 ──
        var detailRoot = NewUI("Detail", panel.transform);
        Anchor(detailRoot, new Vector2(0.44f, 0), new Vector2(1, 1), new Vector2(10, 50), new Vector2(-20, -150));
        AddImage(detailRoot, new Color(0f, 0f, 0f, 0.25f));

        var detailIcon = NewUI("DetailIcon", detailRoot.transform);
        Anchor(detailIcon, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -140), new Vector2(140, -20));
        var detailIconImg = AddImage(detailIcon, Color.white);

        var detailName = NewText("DetailName", detailRoot.transform, "", 32, TextAlignmentOptions.TopLeft);
        Anchor(detailName, new Vector2(0, 1), new Vector2(1, 1), new Vector2(160, -90), new Vector2(-20, -20));

        var detailText = NewText("DetailText", detailRoot.transform, "", 22, TextAlignmentOptions.TopLeft);
        Anchor(detailText, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 20), new Vector2(-20, -150));

        // ── 하단 페이지 + 닫기 ──
        var pageText = NewText("PageText", panel.transform, "0 / 0", 24, TextAlignmentOptions.Center);
        Anchor(pageText, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 10), new Vector2(0, 45));

        var closeBtn = NewButton(panel.transform, "닫기");
        Anchor(closeBtn.gameObject, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -60), new Vector2(-20, -15));

        // ── 슬롯 프리팹 ──
        var slotPrefab = BuildSlotPrefab();

        // ── 컨트롤러 필드 연결 ──
        var so = new SerializedObject(controller);
        so.FindProperty("codexPanel").objectReferenceValue = panel;
        var arr = so.FindProperty("categoryButtons");
        arr.arraySize = 4;
        for (int i = 0; i < 4; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = catButtons[i];
        so.FindProperty("listContainer").objectReferenceValue = content.transform;
        so.FindProperty("listSlotPrefab").objectReferenceValue = slotPrefab;
        so.FindProperty("detailName").objectReferenceValue = detailName.GetComponent<TMP_Text>();
        so.FindProperty("detailIcon").objectReferenceValue = detailIconImg;
        so.FindProperty("detailText").objectReferenceValue = detailText.GetComponent<TMP_Text>();
        so.FindProperty("pageText").objectReferenceValue = pageText.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // 닫기 버튼 → CloseCodex
        UnityEventTools.AddPersistentListener(closeBtn.onClick, controller.CloseCodex);

        panel.SetActive(false); // 루트는 켜진 채 시각 패널만 닫힘 → 닫힌 상태에서도 F8 동작
        Debug.Log("[Codex] 도감 UI 골격 생성 완료. F8로 열기/닫기 (또는 CodexUIController.OpenCodex()).");
        Selection.activeGameObject = root;
    }

    // ── 슬롯 프리팹 생성 ──
    private static CodexListSlot BuildSlotPrefab()
    {
        const string dir = "Assets/Resources/Prefabs/Codex";
        EnsureFolder(dir);

        var slot = NewUI("CodexListSlot", null);
        var le = slot.AddComponent<LayoutElement>();
        le.minHeight = 44;
        var btn = slot.AddComponent<Button>();
        var img = slot.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.06f);
        btn.targetGraphic = img;

        var icon = NewUI("Icon", slot.transform);
        Anchor(icon, new Vector2(0, 0), new Vector2(0, 1), new Vector2(6, 6), new Vector2(44, -6));
        var iconImg = AddImage(icon, Color.white);

        var label = NewText("Label", slot.transform, "", 22, TextAlignmentOptions.Left);
        Anchor(label, new Vector2(0, 0), new Vector2(1, 1), new Vector2(52, 0), new Vector2(-8, 0));

        var comp = slot.AddComponent<CodexListSlot>();
        var sso = new SerializedObject(comp);
        sso.FindProperty("button").objectReferenceValue = btn;
        sso.FindProperty("label").objectReferenceValue = label.GetComponent<TMP_Text>();
        sso.FindProperty("icon").objectReferenceValue = iconImg;
        sso.ApplyModifiedPropertiesWithoutUndo();

        string path = $"{dir}/CodexListSlot.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(slot, path);
        Object.DestroyImmediate(slot);
        return prefab.GetComponent<CodexListSlot>();
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
        return go;
    }

    private static Button NewButton(Transform parent, string label)
    {
        var go = NewUI("Btn_" + label, parent);
        var img = AddImage(go, new Color(1, 1, 1, 0.12f));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var t = NewText("Label", go.transform, label, 24, TextAlignmentOptions.Center);
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

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
