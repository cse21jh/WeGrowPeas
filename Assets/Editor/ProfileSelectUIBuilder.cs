using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 시작 화면에 프로필 선택 창(ProfileSelectRoot 프리팹)과 여는 버튼을 만든다.
///
/// 모양은 시작 화면의 기존 요소에서 가져온다.
///   카드·팝업 배경, 폰트   ← SaveSlotPanel/Slot_1
///   뒤로 버튼              ← SaveSlotPanel/BeforeBtn (복제)
///   여는 버튼              ← SettingsBtn (복제, 왼쪽 옆에 배치)
///
/// 수치는 1920x1080 기준으로 적고, 시작 화면 Canvas의 기준 해상도(CanvasScaler)에 맞춰 줄여 쓴다.
///
/// Build: 이미 만든 프리팹·버튼이 있으면 다시 만들지 않고 연결만 맞춘다. 여러 번 실행해도 안전하다.
/// Rebuild: 프리팹과 씬의 인스턴스를 지우고 처음부터 다시 만든다(프리팹에서 고친 디자인은 사라진다).
/// </summary>
public static class ProfileSelectUIBuilder
{
    private const string StartScenePath = "Assets/Scenes/StartScene.unity";
    private const string FolderPath = "Assets/Resources/Prefabs/Profile";
    private const string PrefabPath = FolderPath + "/ProfileSelectRoot.prefab";
    private const string MarkerSpritePath = FolderPath + "/ProfileCurrentMarker.png";

    private const string RootName = "ProfileSelectRoot";
    private const string ButtonName = "Btn_프로필";
    private const string ButtonLabel = "프로필";

    /// <summary>여는 버튼 위치(SettingsBtn과 같은 오른쪽 위 기준). 어색하면 이 값만 고치거나 씬에서 옮기면 된다.</summary>
    private static readonly Vector2 ButtonPosition = new Vector2(-120f, -40f);

    /// <summary>아래 수치를 적은 기준 해상도(세로).</summary>
    private const float DesignHeight = 1080f;

    /// <summary>Canvas 기준 해상도 / DesignHeight. 예) 800x600 캔버스면 0.556.</summary>
    private static float k = 1f;

    private static float S(float v) => v * k;
    private static Vector2 S(Vector2 v) => v * k;

    private static readonly Color TitleColor = new Color(1f, 0.84f, 0.35f);
    private static readonly Color LabelColor = new Color(0.55f, 0.8f, 1f);
    private static readonly Color MarkerColor = new Color(0.9f, 0.15f, 0.15f);
    private static readonly Color DeleteColor = new Color(0.85f, 0.3f, 0.3f);

    [MenuItem("Tools/Profile/Build Profile Select UI")]
    public static void Build() => Run(rebuild: false);

    [MenuItem("Tools/Profile/Rebuild Profile Select UI (overwrite)")]
    public static void Rebuild()
    {
        if (!EditorUtility.DisplayDialog("프로필 선택 UI 다시 만들기",
                "프리팹과 씬의 인스턴스를 지우고 처음부터 다시 만듭니다.\n프리팹에서 고친 디자인은 사라집니다.",
                "다시 만들기", "취소"))
            return;
        Run(rebuild: true);
    }

    private static void Run(bool rebuild)
    {
        // 씬을 열면 저장 안 한 변경은 사라진다. 먼저 물어본다.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Profile] 씬을 열지 못했습니다: {StartScenePath}");
            return;
        }

        Transform canvas = null;
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Canvas") { canvas = go.transform; break; }
        if (canvas == null) { Debug.LogError("[Profile] StartScene에서 'Canvas'를 찾지 못했습니다."); return; }

        var t = Templates.Find(canvas);
        if (t == null) return;

        var scaler = canvas.GetComponent<CanvasScaler>();
        k = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            ? scaler.referenceResolution.y / DesignHeight
            : 1f;

        // 1) 프리팹 + 씬 인스턴스
        Transform existing = canvas.Find(RootName);
        if (rebuild)
        {
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            existing = null;
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        ProfileSelectUI ui;
        if (existing != null)
        {
            ui = existing.GetComponent<ProfileSelectUI>();
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = CreatePrefab(canvas, t);
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.transform.SetAsLastSibling(); // 다른 시작 화면 UI보다 위에 그린다
            ui = instance.GetComponent<ProfileSelectUI>();
        }

        if (ui == null) { Debug.LogError($"[Profile] {RootName}에 ProfileSelectUI가 없습니다."); return; }

        // 2) 여는 버튼
        SetupOpenButton(canvas, t.settingsButton, ui);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = ui.gameObject;

        Debug.Log($"[Profile] 프로필 선택 UI 설치 완료. 프리팹: {PrefabPath}, 버튼: Canvas/{ButtonName}");
    }

    // ── 템플릿 ────────────────────────────────────────────────────────────────

    private class Templates
    {
        public Image slotImage;
        public TMP_FontAsset font;
        public GameObject backButton;
        public GameObject settingsButton;

        public static Templates Find(Transform canvas)
        {
            var slot = canvas.Find("SaveSlotPanel/Slot_1");
            var back = canvas.Find("SaveSlotPanel/BeforeBtn");
            var settings = canvas.Find("SettingsBtn");

            if (slot == null || back == null || settings == null)
            {
                Debug.LogError("[Profile] 템플릿을 찾지 못했습니다. 필요: Canvas/SaveSlotPanel/Slot_1, " +
                               "Canvas/SaveSlotPanel/BeforeBtn, Canvas/SettingsBtn");
                return null;
            }

            var label = slot.GetComponentInChildren<TMP_Text>(true);
            return new Templates
            {
                slotImage = slot.GetComponent<Image>(),
                font = label != null ? label.font : TMP_Settings.defaultFontAsset,
                backButton = back.gameObject,
                settingsButton = settings.gameObject,
            };
        }
    }

    // ── 프리팹 생성 ───────────────────────────────────────────────────────────

    private static GameObject CreatePrefab(Transform canvas, Templates t)
    {
        EnsureFolder();
        var marker = CreateMarkerSprite();

        var root = NewUI(RootName, canvas);
        Stretch(root);
        var ui = root.gameObject.AddComponent<ProfileSelectUI>();

        // 전체 창(어두운 배경이 뒤쪽 클릭을 막는다)
        var panel = NewUI("Panel", root);
        Stretch(panel);
        AddImage(panel, null, new Color(0f, 0f, 0f, 0.8f));

        var title = AddText(NewUI("Title", panel), "플레이할 세이브 프로필을 선택하세요!", 48, TitleColor, t.font);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1400f, 80f));

        // 카드 3장
        var row = NewUI("Cards", panel);
        Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1500f, 560f));
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = S(60f);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        var cards = new ProfileCardUI[ProfileStore.ProfileCount];
        for (int i = 0; i < cards.Length; i++)
            cards[i] = CreateCard(row, i, t, marker);

        // 뒤로 버튼(BeforeBtn 복제). 시작 화면에서 쓰던 연결은 지우고 ProfileSelectUI가 런타임에 붙인다.
        var back = Object.Instantiate(t.backButton, panel);
        back.name = "BackButton";
        back.SetActive(true);
        var backRt = back.GetComponent<RectTransform>();
        PlaceRaw(backRt, new Vector2(0f, 0.5f), S(new Vector2(120f, -150f)), backRt.sizeDelta);
        var backButton = back.GetComponent<Button>();
        ClearPersistentListeners(backButton);
        var backLabel = back.GetComponentInChildren<TMP_Text>(true);
        if (backLabel != null) backLabel.text = "뒤로";

        // 삭제 확인 팝업
        var confirm = NewUI("ConfirmPopup", panel);
        Stretch(confirm);
        AddImage(confirm, null, new Color(0f, 0f, 0f, 0.6f));

        var box = NewUI("Box", confirm);
        Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 340f));
        AddImage(box, t.slotImage);

        var confirmText = AddText(NewUI("Message", box), "", 32, Color.white, t.font);
        Place(confirmText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(680f, 180f));

        var yes = CreateTextButton(box, "YesButton", "삭제", t, DeleteColor);
        Place(yes.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(-150f, -100f), new Vector2(220f, 80f));
        var no = CreateTextButton(box, "NoButton", "취소", t, Color.white);
        Place(no.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(150f, -100f), new Vector2(220f, 80f));

        // 참조 연결
        var so = new SerializedObject(ui);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        var cardsProp = so.FindProperty("cards");
        cardsProp.arraySize = cards.Length;
        for (int i = 0; i < cards.Length; i++) cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        so.FindProperty("backButton").objectReferenceValue = backButton;
        so.FindProperty("confirmPopup").objectReferenceValue = confirm.gameObject;
        so.FindProperty("confirmText").objectReferenceValue = confirmText;
        so.FindProperty("confirmYesButton").objectReferenceValue = yes;
        so.FindProperty("confirmNoButton").objectReferenceValue = no;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 프리팹에는 꺼진 상태로 저장(Awake에서도 끄지만 에디터에서 씬을 가리지 않도록)
        panel.gameObject.SetActive(false);
        confirm.gameObject.SetActive(false);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath, out bool ok);
        Object.DestroyImmediate(root.gameObject);
        if (!ok) { Debug.LogError($"[Profile] 프리팹 저장 실패: {PrefabPath}"); return null; }
        return prefab;
    }

    private static ProfileCardUI CreateCard(Transform parent, int index, Templates t, Sprite marker)
    {
        var card = NewUI($"Card_{index + 1}", parent);
        card.sizeDelta = S(new Vector2(420f, 560f));
        var bg = AddImage(card, t.slotImage);
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        var cardUI = card.gameObject.AddComponent<ProfileCardUI>();

        var title = AddText(NewUI("Title", card), $"세이브 프로필 {index + 1}", 40, TitleColor, t.font);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(380f, 60f));

        // 데이터 있음
        var filled = NewUI("Filled", card);
        Place(filled, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(380f, 260f));
        var v = filled.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = S(4f);
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        LayoutText(filled, "PlayTimeLabel", "플레이 시간", 28, LabelColor, t.font, 40f);
        var playTime = LayoutText(filled, "PlayTime", "0:00:00", 32, Color.white, t.font, 44f);
        var spacer = NewUI("Spacer", filled);
        spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = S(30f);
        LayoutText(filled, "UpdatedLabel", "업데이트됨", 28, LabelColor, t.font, 40f);
        var updated = LayoutText(filled, "Updated", "-", 28, Color.white, t.font, 40f);

        // 데이터 없음
        var empty = AddText(NewUI("Empty", card), "비어있음", 32, Color.white, t.font);
        Place(empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(380f, 60f));

        // 현재 프로필 표시(카드 위 빨간 화살표)
        var markerRt = NewUI("CurrentMarker", card);
        Place(markerRt, new Vector2(0.5f, 1f), new Vector2(0f, 30f), new Vector2(56f, 48f));
        var markerImg = AddImage(markerRt, null, MarkerColor);
        markerImg.sprite = marker;
        markerImg.raycastTarget = false;

        // 삭제 버튼(카드 아래)
        var delete = CreateTextButton(card, "DeleteButton", "삭제", t, DeleteColor);
        Place(delete.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, -70f), new Vector2(120f, 70f));

        var so = new SerializedObject(cardUI);
        so.FindProperty("cardButton").objectReferenceValue = button;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("filledGroup").objectReferenceValue = filled.gameObject;
        so.FindProperty("playTimeText").objectReferenceValue = playTime;
        so.FindProperty("updatedText").objectReferenceValue = updated;
        so.FindProperty("emptyGroup").objectReferenceValue = empty.gameObject;
        so.FindProperty("deleteButton").objectReferenceValue = delete;
        so.FindProperty("currentMarker").objectReferenceValue = markerRt.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        return cardUI;
    }

    // ── 여는 버튼 ─────────────────────────────────────────────────────────────

    private static void SetupOpenButton(Transform canvas, GameObject template, ProfileSelectUI ui)
    {
        Transform existing = canvas.Find(ButtonName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = Object.Instantiate(template, canvas);
            go.name = ButtonName;
            go.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            go.GetComponent<RectTransform>().anchoredPosition = ButtonPosition;

            var label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = ButtonLabel;
        }
        go.SetActive(true);

        var button = go.GetComponent<Button>();
        if (button == null) { Debug.LogError($"[Profile] {ButtonName}에 Button이 없습니다."); return; }

        ClearPersistentListeners(button);
        UnityEventTools.AddPersistentListener(button.onClick, new UnityAction(ui.Open));
        EditorUtility.SetDirty(button);
    }

    // ── 도우미 ────────────────────────────────────────────────────────────────

    private static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>위치·크기는 1920x1080 기준 수치로 받아 캔버스에 맞게 줄인다.</summary>
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        => PlaceRaw(rt, anchor, S(pos), S(size));

    /// <summary>이미 캔버스 단위인 값을 그대로 쓴다.</summary>
    private static void PlaceRaw(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>template이 있으면 그 스프라이트·타입·색을 그대로 쓴다.</summary>
    private static Image AddImage(RectTransform rt, Image template, Color? color = null)
    {
        var img = rt.gameObject.AddComponent<Image>();
        if (template != null)
        {
            img.sprite = template.sprite;
            img.type = template.type;
            img.pixelsPerUnitMultiplier = template.pixelsPerUnitMultiplier;
            img.color = template.color;
        }
        if (color.HasValue) img.color = color.Value;
        return img;
    }

    private static TextMeshProUGUI AddText(RectTransform rt, string text, float size, Color color, TMP_FontAsset font)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = S(size);
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TextMeshProUGUI LayoutText(RectTransform parent, string name, string text, float size, Color color,
                                              TMP_FontAsset font, float height)
    {
        var tmp = AddText(NewUI(name, parent), text, size, color, font);
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = S(height);
        return tmp;
    }

    private static Button CreateTextButton(RectTransform parent, string name, string label, Templates t, Color tint)
    {
        var rt = NewUI(name, parent);
        var img = AddImage(rt, t.slotImage);
        img.color = tint;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        var text = AddText(NewUI("Label", rt), label, 28, Color.white, t.font);
        Stretch(text.rectTransform);
        return button;
    }

    private static void ClearPersistentListeners(Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(FolderPath)) return;
        Directory.CreateDirectory(FolderPath);
        AssetDatabase.Refresh();
    }

    /// <summary>현재 프로필 표시용 아래 방향 삼각형 스프라이트를 만든다(없을 때만).</summary>
    private static Sprite CreateMarkerSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
        if (sprite != null) return sprite;
        AssetDatabase.DeleteAsset(MarkerSpritePath); // 잘못 가져온(스프라이트 없는) 이전 파일

        const int w = 64, h = 56;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < h; y++)
        {
            // 아래(y=0)가 꼭짓점, 위로 갈수록 넓어진다.
            float half = (y + 0.5f) / h * (w / 2f);
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs(x + 0.5f - w / 2f);
                float a = Mathf.Clamp01(half - d + 0.5f); // 가장자리 1px 안티앨리어싱
                tex.SetPixel(x, y, a > 0f ? new Color(1f, 1f, 1f, a) : clear);
            }
        }
        tex.Apply();
        File.WriteAllBytes(MarkerSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(MarkerSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(MarkerSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single; // 기본 설정(Multiple)이면 Sprite로 불러와지지 않는다
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
        if (sprite == null) Debug.LogWarning($"[Profile] 화살표 스프라이트를 불러오지 못했습니다: {MarkerSpritePath}");
        return sprite;
    }
}
