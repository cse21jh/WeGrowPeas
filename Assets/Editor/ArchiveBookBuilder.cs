using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using WeGrowPeas.UI.Book;
using Object = UnityEngine.Object;

/// <summary>Additive archive installation. Source books and every existing layout are preserved.</summary>
public static class ArchiveBookBuilder
{
    public const string PrefabPath = "Assets/Resource/UI/Archive/ArchiveRoot.prefab";
    private const string Art = "Assets/Resource/UI/Archive/";
    private const string ScenePath = "Assets/Scenes/StartScene 3.unity";
    private static TMP_FontAsset titleFont, bodyFont, menuFont;
    private static Material titleMaterial, bodyMaterial;

    [MenuItem("Tools/Archive/Install in StartScene 3")]
    public static void Install()
    {
        RequireProject();
        if (Application.isPlaying) throw new InvalidOperationException("Install outside Play Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open StartScene 3 before installing the archive.");
        var recall = Object.FindObjectsByType<RecallUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(c => c.gameObject.scene == scene);
        if (recall.GetComponent<ArchiveBookController>() != null)
        {
            ArchiveArtRefiner.Apply();
            WireEntrance();
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            throw new InvalidOperationException("Archive prefab already exists; do not overwrite authored content.");
        Directory.CreateDirectory("Temp/ArchiveValidation");
        EditorSceneManager.SaveScene(scene, "Temp/ArchiveValidation/BeforeInstall.unity", true);
        PrepareArt();
        PrepareFonts();
        Undo.RegisterFullObjectHierarchyUndo(recall.gameObject, "Install archive");
        var book = recall.GetComponentInChildren<BookLayoutController>(true);
        var panel = book.transform.parent.gameObject;
        var motion = book.GetComponent<BookCloseMotion>();
        var archive = Undo.AddComponent<ArchiveBookController>(recall.gameObject);
        var archiveSo = new SerializedObject(archive);
        Ref(archiveSo, "panel", panel); Ref(archiveSo, "recall", recall); Ref(archiveSo, "book", book); Ref(archiveSo, "motion", motion);

        // Keep all existing roots (including the nominally empty Layout0) byte-for-byte in the source prefab.
        int codexIndex = book.LayoutCount;
        var codex = BuildSpread(book, archive, "Archive_Codex", false);
        int achievementIndex = book.LayoutCount;
        BuildSpread(book, archive, "Archive_Achievements", true);
        archiveSo.FindProperty("codexLayout").intValue = codexIndex;
        archiveSo.FindProperty("achievementLayout").intValue = achievementIndex;
        Ref(archiveSo, "codex", codex);
        BuildHeader(panel.transform, archiveSo);
        var nav = archiveSo.FindProperty("recallNavigation");
        var navigation = book.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Previous" || t.name == "Next" || t.name == "LeftPageIndexText" || t.name == "RightPageIndexText")
            .Select(t => t.gameObject).ToArray();
        nav.arraySize = navigation.Length;
        for (int i = 0; i < navigation.Length; i++) nav.GetArrayElementAtIndex(i).objectReferenceValue = navigation[i];
        archiveSo.ApplyModifiedPropertiesWithoutUndo();
        SetRef(recall, "archive", archive);
        var oldTitle = panel.transform.Find("Title");
        if (oldTitle != null) oldTitle.gameObject.SetActive(false);
        // Fade the new header with the already-authored book open/close motion.
        var motionSo = new SerializedObject(motion);
        var graphics = panel.transform.Find("ArchiveNavigation").GetComponentsInChildren<Graphic>(true);
        var fades = motionSo.FindProperty("titleGraphics");
        fades.arraySize = graphics.Length;
        for (int i = 0; i < graphics.Length; i++) fades.GetArrayElementAtIndex(i).objectReferenceValue = graphics[i];
        motionSo.ApplyModifiedPropertiesWithoutUndo();

        var close = book.GetComponentInChildren<BookCloseButton>(true);
        ClearClicks(close);
        UnityEventTools.AddVoidPersistentListener(close.onClick, archive.Close);
        // Record overrides before saving a variant; never apply them to RecallRoot or BookUIBase.
        foreach (var component in recall.GetComponentsInChildren<Component>(true))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        PrefabUtility.SaveAsPrefabAssetAndConnect(recall.gameObject, PrefabPath, InteractionMode.AutomatedAction);
        ArchiveArtRefiner.Apply();

        var codexController = Object.FindObjectsByType<CodexUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(c => c.gameObject.scene == scene);
        SetRef(codexController, "archive", archive);
        PrefabUtility.RecordPrefabInstancePropertyModifications(codexController);
        var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .First(t => t.name == "SettingButtons");
        var entrance = menu.Find("HistoryBtn").GetComponent<Button>();
        Undo.RegisterFullObjectHierarchyUndo(entrance.gameObject, "Unify archive menu");
        ClearClicks(entrance);
        WireEntrance();
        foreach (var label in entrance.GetComponentsInChildren<TMP_Text>(true))
        {
            label.text = "아카이브";
            var localized = label.GetComponent<LocalizedText>() ?? label.gameObject.AddComponent<LocalizedText>();
            localized.Link("archive.title", "아카이브");
        }
        foreach (string oldButton in new[] { "CodexBtn", "AchievementBtn" })
        {
            var target = menu.Find(oldButton);
            if (target == null) continue;
            Undo.RecordObject(target.gameObject, "Unify archive menu");
            target.gameObject.SetActive(false);
        }
        // Preserve the user's original scene objects and button references; only the visible entry changes.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = archive;
        Debug.Log("[Archive] Installed archive variant with additional layouts " + codexIndex + " and " + achievementIndex);
    }

    [MenuItem("Tools/Archive/Connect Main Menu Button")]
    public static void WireEntrance()
    {
        RequireProject();
        if (Application.isPlaying) throw new InvalidOperationException("Connect outside Play Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new InvalidOperationException("Open StartScene 3 before connecting the archive.");
        var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .First(t => t.name == "SettingButtons");
        var customize = menu.Find("CustomizeBtn").GetComponent<Button>();
        var ui = Enumerable.Range(0, customize.onClick.GetPersistentEventCount())
            .Select(i => customize.onClick.GetPersistentTarget(i)).OfType<UIClickEvent>().First();
        var entrance = menu.Find("HistoryBtn").GetComponent<Button>();
        Undo.RecordObject(entrance, "Connect archive through UI controller");
        bool replaced = false;
        for (int i = 0; i < entrance.onClick.GetPersistentEventCount(); i++)
        {
            if (!(entrance.onClick.GetPersistentTarget(i) is ArchiveBookController) &&
                entrance.onClick.GetPersistentMethodName(i) != nameof(UIClickEvent.OnClick_OpenArchive)) continue;
            UnityEventTools.RegisterVoidPersistentListener(entrance.onClick, i, ui.OnClick_OpenArchive);
            replaced = true;
        }
        if (!replaced) UnityEventTools.AddVoidPersistentListener(entrance.onClick, ui.OnClick_OpenArchive);
        if (PrefabUtility.IsPartOfPrefabInstance(entrance)) PrefabUtility.RecordPrefabInstancePropertyModifications(entrance);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static ArchiveCatalogView BuildSpread(BookLayoutController book, ArchiveBookController archive, string name, bool achievements)
    {
        var left = Stretch(name, book.Turner.LeftPage);
        var right = Stretch(name, book.Turner.RightPage);
        var view = left.gameObject.AddComponent<ArchiveCatalogView>();
        var so = new SerializedObject(view);
        so.FindProperty("achievements").boolValue = achievements;
        Ref(so, "book", book); Ref(so, "archive", archive);
        Ref(so, "completeStamp", Sprite("Complete")); Ref(so, "inProgressStamp", Sprite("InProgress"));
        Ref(so, "achievementFallbackIcon", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resource/Sprites/UI/Icons/땅문서.png"));
        var list = Scroll(left, "Entries", 28, achievements ? 30 : 140, 492, achievements ? 605 : 495, false);
        Ref(so, "listScroll", list);
        var layout = list.content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 7;
        layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        var fitter = list.content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var template = BuildRow(list.content, achievements);
        template.gameObject.SetActive(false);
        Ref(so, "rowTemplate", template);
        Ref(so, "count", Text(left, "EntryCount", "0 / 0", 190, 645, 180, 30, 19, bodyFont, TextAlignmentOptions.Center));
        var empty = Text(left, "EmptyState", "아직 표시할 기록이 없어요.", 45, 270, 455, 120, 23, bodyFont, TextAlignmentOptions.Center);
        empty.color = Brown;
        Ref(so, "empty", empty);
        var heading = Text(right, "DetailTitle", achievements ? "업적" : "도감", 42, 35, 460, 105, 70, titleFont, TextAlignmentOptions.Center);
        heading.enableAutoSizing = true; heading.fontSizeMin = 30; heading.fontSizeMax = 70;
        heading.fontSharedMaterial = titleMaterial;
        Ref(so, "detailTitle", heading);
        ImageAt(right, "Divider", 45, achievements ? 145 : 432, 475, 2, new Color32(210, 175, 147, 255));

        if (achievements)
        {
            var stamp = ImageAt(right, "AchievementStamp", 430, 10, 105, 105, Color.white, Sprite("InProgress"));
            stamp.preserveAspect = true; stamp.rectTransform.localEulerAngles = new Vector3(0, 0, -15);
            Ref(so, "detailStamp", stamp);
            var caption = Text(right, "ProgressLabel", "진행도", 45, 163, 105, 45, 29, titleFont);
            caption.gameObject.AddComponent<LocalizedText>().Link("archive.progress", "진행도");
            var track = ImageAt(right, "ProgressTrack", 150, 174, 365, 26.25f, Color.white);
            ArchiveArtRefiner.ConfigureProgressTrack(track, so, book);
            Ref(so, "progressValue", Text(right, "ProgressValue", "0 / 0", 150, 199, 365, 25, 16, bodyFont, TextAlignmentOptions.Right));
        }
        else
        {
            var frame = ImageAt(right, "IconFrame", 166, 175, 220, 220, new Color32(247, 236, 226, 255), BuiltinRound());
            frame.type = Image.Type.Sliced;
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(210, 175, 147, 255); outline.effectDistance = new Vector2(3, -3);
            var icon = ImageAt(right, "DetailIcon", 205, 214, 142, 142, Color.white);
            icon.preserveAspect = true; icon.enabled = false;
            Ref(so, "detailIcon", icon);
            var buttons = so.FindProperty("categoryButtons"); buttons.arraySize = 4;
            string[] keys = { "item", "plant", "curse", "bug" };
            for (int i = 0; i < 4; i++)
            {
                var image = ImageAt(left, "Category_" + keys[i], 30 + i * 92, -7, 78, 125, Color.clear);
                image.raycastTarget = true;
                var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                ArchiveArtRefiner.BuildBookmark(button, i);
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = button;
            }
        }
        var detail = Scroll(right, "Description", 45, achievements ? 230 : 447, 475, achievements ? 405 : 188, true);
        var text = Text(detail.content, "Text", "", 20, 20, 422, 100, 19, bodyFont, TextAlignmentOptions.TopLeft);
        text.color = Brown; text.fontSharedMaterial = bodyFont.material; text.textWrappingMode = TextWrappingModes.Normal;
        var detailLayout = detail.content.gameObject.AddComponent<VerticalLayoutGroup>();
        detailLayout.padding = new RectOffset(22, 25, 22, 22); detailLayout.childControlWidth = true;
        detailLayout.childControlHeight = true; detailLayout.childForceExpandHeight = false;
        var detailFitter = detail.content.gameObject.AddComponent<ContentSizeFitter>();
        detailFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Ref(so, "detailText", text); Ref(so, "detailScroll", detail);
        so.ApplyModifiedPropertiesWithoutUndo();

        var bookSo = new SerializedObject(book);
        var layouts = bookSo.FindProperty("layouts");
        int index = layouts.arraySize; layouts.InsertArrayElementAtIndex(index);
        var entry = layouts.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("name").stringValue = achievements ? "아카이브_업적" : "아카이브_도감";
        entry.FindPropertyRelative("leftContent").objectReferenceValue = left;
        entry.FindPropertyRelative("rightContent").objectReferenceValue = right;
        entry.FindPropertyRelative("onShow.m_PersistentCalls.m_Calls").arraySize = 0;
        bookSo.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddVoidPersistentListener(book.Layouts[index].onShow, view.Refresh);
        EditorUtility.SetDirty(book);
        left.gameObject.SetActive(false); right.gameObject.SetActive(false);
        return view;
    }

    private static ArchiveEntryRow BuildRow(Transform parent, bool achievements)
    {
        var background = ImageAt(parent, "RowTemplate", 0, 0, 473, 80, Color.white, Sprite("EntryBackground"));
        background.type = Image.Type.Sliced; background.pixelsPerUnitMultiplier = 1.6f; background.raycastTarget = true;
        var element = background.gameObject.AddComponent<LayoutElement>(); element.preferredHeight = 80;
        var button = background.gameObject.AddComponent<Button>(); button.targetGraphic = background;
        var row = background.gameObject.AddComponent<ArchiveEntryRow>();
        var so = new SerializedObject(row);
        Ref(so, "button", button); Ref(so, "background", background);
        var icon = ImageAt(background.transform, "Icon", 16, 10, 61, 61, Color.white); icon.preserveAspect = true;
        Ref(so, "icon", icon);
        var label = Text(background.transform, "Name", "", 100, 13, achievements ? 275 : 345, 54, 28, bodyFont);
        label.enableAutoSizing = true; label.fontSizeMin = 20; label.fontSizeMax = 28;
        Ref(so, "label", label);
        var stamp = ImageAt(background.transform, "Stamp", 393, 0, 80, 80, Color.white);
        stamp.preserveAspect = true; stamp.rectTransform.localEulerAngles = new Vector3(0, 0, -9.34f);
        Ref(so, "stamp", stamp);
        so.ApplyModifiedPropertiesWithoutUndo();
        return row;
    }

    private static void BuildHeader(Transform panel, SerializedObject so)
    {
        var navigation = Stretch("ArchiveNavigation", panel);
        // The sidebar stays outside BookContent, so it never becomes part of a turning page.
        var icon = ImageAt(navigation, "CurrentTabIcon", 35, 25, 235, 235, Color.white, Sprite("RecallTab"));
        icon.preserveAspect = true; icon.rectTransform.localEulerAngles = new Vector3(0, 0, -16.83f);
        var title = Text(navigation, "CurrentTabTitle", "회상", 185, 130, 260, 110, 108, titleFont);
        title.enableAutoSizing = true; title.fontSizeMin = 40; title.fontSizeMax = 108;
        title.fontSharedMaterial = titleMaterial;
        Ref(so, "currentIcon", icon); Ref(so, "title", title);
        var icons = so.FindProperty("tabIcons"); icons.arraySize = 3;
        string[] names = { "RecallTab", "CodexTab", "AchievementTab" };
        for (int i = 0; i < 3; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = Sprite(names[i]);
        var menu = At("OtherTabs", navigation, 90, 295, 145, 330);
        var group = menu.gameObject.AddComponent<CanvasGroup>();
        Ref(so, "menuRoot", menu); Ref(so, "menuGroup", group);
        var buttons = so.FindProperty("menuButtons"); buttons.arraySize = 2;
        var pictures = so.FindProperty("menuIcons"); pictures.arraySize = 2;
        var labels = so.FindProperty("menuLabels"); labels.arraySize = 2;
        for (int i = 0; i < 2; i++)
        {
            var hit = ImageAt(menu, "Tab" + i, 0, i * 155, 145, 145, Color.clear);
            hit.raycastTarget = true;
            var button = hit.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
            var picture = ImageAt(hit.transform, "Icon", 19, 0, 108, 108, new Color(1, 1, 1, 0.65f), Sprite(names[i + 1]));
            picture.preserveAspect = true; picture.rectTransform.localEulerAngles = new Vector3(0, 0, 15);
            var label = Text(hit.transform, "Label", i == 0 ? "도감" : "업적", 0, 108, 145, 35, 30, menuFont, TextAlignmentOptions.Center);
            label.color = new Color(1, 1, 1, 0.65f);
            buttons.GetArrayElementAtIndex(i).objectReferenceValue = button;
            pictures.GetArrayElementAtIndex(i).objectReferenceValue = picture;
            labels.GetArrayElementAtIndex(i).objectReferenceValue = label;
        }
    }

    private static ScrollRect Scroll(Transform parent, string name, float x, float y, float width, float height, bool background)
    {
        var root = At(name, parent, x, y, width, height);
        var hit = root.gameObject.AddComponent<Image>(); hit.raycastTarget = true;
        hit.sprite = background ? Sprite("EntryBackground") : null;
        hit.type = Image.Type.Sliced; hit.pixelsPerUnitMultiplier = 1.6f;
        hit.color = new Color(1, 1, 1, background ? 0.5f : 0);
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36;
        var viewport = Stretch("Viewport", root); viewport.offsetMax = new Vector2(-18, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Stretch("Content", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
        scroll.viewport = viewport; scroll.content = content;
        var rail = ImageAt(root, "Scrollbar", width - 10, 0, 8, height, new Color(1, 1, 1, 0.25f), BuiltinRound()); rail.type = Image.Type.Sliced;
        var bar = rail.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
        var handle = Stretch("Handle", rail.transform);
        var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.sprite = BuiltinRound(); handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(1, 1, 1, 0.65f); bar.handleRect = handle; bar.targetGraphic = handleImage;
        scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        return scroll;
    }

    private static void PrepareArt()
    {
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (Path.GetFileNameWithoutExtension(path) == "EntryBackground") importer.spriteBorder = new Vector4(22, 22, 22, 22);
            importer.SaveAndReimport();
        }
    }

    private static void PrepareFonts()
    {
        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Tenada/Tenada SDF.asset");
        bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Maplestory Bold SDF.asset");
        menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/OneMobilePop/ONE Mobile POP SDF.asset");
        titleMaterial = FontMaterial(titleFont, "ArchiveTitle", new Color32(210, 129, 82, 255), 0.16f);
        bodyMaterial = FontMaterial(bodyFont, "ArchiveLabel", new Color32(168, 129, 99, 255), 0.12f);
    }

    private static Material FontMaterial(TMP_FontAsset font, string name, Color outline, float width)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(Art + name + ".mat");
        if (existing != null) return existing;
        var material = new Material(font.material) { name = name };
        material.SetFloat("_OutlineWidth", width); material.SetColor("_OutlineColor", outline);
        material.EnableKeyword("OUTLINE_ON"); material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", outline); material.SetFloat("_UnderlayOffsetX", 0.5f); material.SetFloat("_UnderlayOffsetY", -0.7f);
        material.SetFloat("_UnderlayDilate", 0.12f); material.SetFloat("_UnderlaySoftness", 0);
        AssetDatabase.CreateAsset(material, Art + name + ".mat");
        return material;
    }

    private static RectTransform At(string name, Transform parent, float x, float y, float w, float h)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        var rect = At(name, parent, 0, 0, 0, 0);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image ImageAt(Transform parent, string name, float x, float y, float w, float h, Color color, Sprite sprite = null)
    {
        var image = At(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
        image.color = color; image.sprite = sprite; image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(Transform parent, string name, string value, float x, float y, float w, float h, float size,
        TMP_FontAsset font, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var text = At(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSharedMaterial = font == bodyFont ? bodyMaterial : font.material;
        text.fontSize = size; text.text = value; text.color = Color.white; text.alignment = alignment;
        text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static Color Brown => new Color32(144, 90, 59, 255);
    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png") ?? throw new InvalidOperationException("Missing archive art: " + name);
    private static Sprite BuiltinRound() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    private static void Ref(SerializedObject so, string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
    private static void SetRef(Object target, string field, Object value) { var so = new SerializedObject(target); Ref(so, field, value); so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void ClearClicks(Button button) { for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i); }
    private static void RequireProject()
    {
        const string allowed = "C:/Users/pingguin1234/Seungbu/00. Work Directory/01. Game Development/03. Fork/WeGrowPeas";
        if (!string.Equals(Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/'), allowed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Archive authoring is restricted to WeGrowPeas.");
    }
}
