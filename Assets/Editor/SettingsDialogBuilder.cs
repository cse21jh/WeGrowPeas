using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using WeGrowPeas.UI;

/// <summary>Reproducible Figma 183:70 import. Only writes the independent settings assets and test scene.</summary>
public static class SettingsDialogBuilder
{
    public const string Folder = "Assets/Resource/UI/SettingsDialog";
    public const string PrefabPath = Folder + "/SettingsDialog.prefab";
    public const string ScenePath = "Assets/Scenes/TestFolder/SettingsDialogTest.unity";
    public const string SharedFontFolder = "Assets/TextMesh Pro/Fonts";
    private const float OriginX = 671f, OriginY = 347f;
    private static TMP_FontAsset bodyFont, tabFont;
    private static Material panelBlend, contentBlend;
    private static readonly Color Cream = new Color32(245, 203, 164, 255);

    [MenuItem("Tools/UI/Settings Dialog/Create Prefab and Test Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building the settings prefab.");
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save the current scene before opening the isolated test scene.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ImportSprites();
        bodyFont = Font("Cafe24Ssurround");
        tabFont = Font("Tenada");
        var woodColor = new Color32(184, 96, 57, 255);
        panelBlend = BlendMaterial("OverWood", woodColor);
        contentBlend = BlendMaterial("OverContent", Color.Lerp(woodColor, Color.white, 0.25f));
        var root = Rect("SettingsDialog", null, 0, 0, 2552, 1437);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2552, 1437);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var view = root.gameObject.AddComponent<SettingsDialogView>();

        var modal = Rect("Modal", root, 0, 0, 0, 0); Stretch(modal);
        var dim = Rect("Dimmer", modal, 0, 0, 0, 0); Stretch(dim);
        var dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0, 0, 0, 0.5f);
        var dialog = Rect("Window", modal, 0, 0, 1385, 927);
        dialog.anchorMin = dialog.anchorMax = dialog.pivot = new Vector2(0.5f, 0.5f);
        dialog.anchoredPosition = Vector2.zero;
        // Transparent source padding is retained; visible artwork aligns with the original layer geometry.
        Art("WoodPanel", dialog, "Panel", 21, 63, 2864, 1432);
        Art("Gear", dialog, "Gear", 1066, 347, 232.5121f, 241.6933f);
        var title = Art("Title", dialog, "Title", 1264.0737f, 420.6401f, 284.4963f, 152.1597f);
        var translatedTitle = Text("TranslatedTitle", dialog, "Settings", 590, 68, 480, 170, 120, tabFont, Color.white, TextAlignmentOptions.Left);
        translatedTitle.enableAutoSizing = true; translatedTitle.fontSizeMin = 72; translatedTitle.fontSizeMax = 120;
        var titleOutline = translatedTitle.gameObject.AddComponent<Outline>(); titleOutline.effectColor = new Color32(37, 19, 16, 255); titleOutline.effectDistance = new Vector2(5, -5);
        translatedTitle.gameObject.SetActive(false);
        var close = Art("Close", dialog, "Close", 1899, 440, 114.5f, 132.5f);
        var closeButton = Button(close);

        string[] names = { "일반", "그래픽", "사운드", "조작" };
        var tabs = new List<Button>();
        var groups = new List<CanvasGroup>();
        for (int i = 0; i < names.Length; i++)
        {
            var tab = DesignRect("Tab_" + names[i], dialog, 803 + i * 282, 598, 265, 78);
            var group = tab.gameObject.AddComponent<CanvasGroup>(); group.alpha = i == 0 ? 1 : 0.5f;
            groups.Add(group);
            var background = Image("Background", tab, "Tab", 0, 0, 265, 78);
            background.material = panelBlend;
            tabs.Add(Button(background));
            var shadow = Text("LabelShadow", tab, names[i], 39, 13, 190, 60, 48, tabFont, new Color32(146, 62, 45, 255), TextAlignmentOptions.Top);
            var label = Text("Label", tab, names[i], 37, 9, 190, 60, 48, tabFont, Color.white, TextAlignmentOptions.Top);
            shadow.margin = label.margin = Vector4.zero;
            shadow.enableAutoSizing = label.enableAutoSizing = true;
            shadow.fontSizeMin = label.fontSizeMin = 30; shadow.fontSizeMax = label.fontSizeMax = 48;
        }

        Art("ContentBackground", dialog, "Content", 766, 712, 1181, 451).material = panelBlend;
        var pages = new List<GameObject>();
        SettingsEntry[][] entries = {
            new[] { SettingsEntry.Language, SettingsEntry.BreedPopup, SettingsEntry.MessageAlarm, SettingsEntry.SpeechBubbleTransparency, SettingsEntry.EndlessMode, SettingsEntry.Codex, SettingsEntry.MainMenu, SettingsEntry.Quit, SettingsEntry.AddGold },
            new[] { SettingsEntry.Resolution, SettingsEntry.WindowMode, SettingsEntry.Quality, SettingsEntry.VSync, SettingsEntry.FrameRate },
            new[] { SettingsEntry.BgmVolume, SettingsEntry.EffectVolume },
            new[] { SettingsEntry.Skip, SettingsEntry.WaveSkip, SettingsEntry.Breed, SettingsEntry.Shovel, SettingsEntry.Scouter, SettingsEntry.Phone, SettingsEntry.ResetKeys }
        };
        for (int tab = 0; tab < names.Length; tab++)
        {
            var page = Rect("Page_" + names[tab], dialog, 0, 0, 1385, 927);
            pages.Add(page.gameObject);
            var viewport = DesignRect("Viewport", page, 798, 732, 543, 421);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hitArea = viewport.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;
            var content = Rect("Rows", viewport, 0, 0, 543, Mathf.Max(421, entries[tab].Length * 71 + 20));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var track = Art("Scrollbar", page, "ScrollTrack", 1966, 712, 14, 451); track.material = panelBlend; track.raycastTarget = true;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var sliding = Rect("SlidingArea", track.rectTransform, 0, 0, 14, 451); Stretch(sliding);
            var thumb = Image("Handle", sliding, "ScrollThumb", 0, 0, 14, 182); Stretch(thumb.rectTransform); thumb.raycastTarget = true; thumb.type = UnityEngine.UI.Image.Type.Sliced;
            scrollbar.handleRect = thumb.rectTransform; scrollbar.targetGraphic = thumb; scrollbar.value = 1;
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            for (int row = 0; row < entries[tab].Length; row++)
            {
                var entry = entries[tab][row];
                var item = Rect(entry.ToString(), content, 10, 10 + row * 71, 523, 71);
                var binding = item.gameObject.AddComponent<SettingsDialogRow>(); binding.entry = entry;
                var hit = item.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                Image("Bullet", item, "Bullet", 0, 20, 14, 14);
                binding.label = Text("Label", item, Loc.T(binding.LabelKey), 32, 0, 235, 62, 32, bodyFont, Cream, TextAlignmentOptions.TopLeft);
                binding.label.enableAutoSizing = true; binding.label.fontSizeMin = 23; binding.label.fontSizeMax = 32;
                if (entry == SettingsEntry.BgmVolume || entry == SettingsEntry.EffectVolume || entry == SettingsEntry.SpeechBubbleTransparency) CreateSlider(item, binding);
                else if (entry >= SettingsEntry.Skip && entry != SettingsEntry.EndlessMode)
                {
                    var field = Image("Action", item, "Field", 280, 6, 243, 41); field.type = UnityEngine.UI.Image.Type.Sliced; field.material = contentBlend;
                    binding.button = Button(field);
                    binding.valueText = Text("Value", field.rectTransform, "", 6, 0, 231, 41, 24, bodyFont, Color.white, TextAlignmentOptions.Center);
                    binding.valueText.enableAutoSizing = true; binding.valueText.fontSizeMin = 17; binding.valueText.fontSizeMax = 24;
                }
                else binding.dropdown = Dropdown(item);
            }
            page.gameObject.SetActive(tab == 0);
        }
        var heading = DesignRect("DescriptionHeading", dialog, 1385, 733, 527, 46);
        var headingText = Text("Label", heading, "설명", 0, 0, 527, 46, 36, bodyFont, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Top);
        headingText.enableAutoSizing = true; headingText.fontSizeMin = 26; headingText.fontSizeMax = 36;
        Art("Divider", dialog, "Divider", 1410.5f, 786.5f, 476.5f, 5).material = contentBlend;
        var textArea = DesignRect("Description", dialog, 1385, 806, 527, 285);
        var description = Text("Label", textArea, "", 14, 0, 499, 285, 26, bodyFont, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Top);
        description.textWrappingMode = TextWrappingModes.Normal; description.enableAutoSizing = true; description.fontSizeMin = 21; description.fontSizeMax = 26;
        var notice = DesignRect("SaveNotice", dialog, 1385, 1112, 527, 36);
        var saveText = Text("Label", notice, "", 0, 0, 527, 36, 22, bodyFont, new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Center);

        var serialized = new SerializedObject(view);
        serialized.FindProperty("modal").objectReferenceValue = modal.gameObject;
        serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
        Assign(serialized, "tabButtons", tabs.Cast<UnityEngine.Object>().ToArray());
        Assign(serialized, "tabVisuals", groups.Cast<UnityEngine.Object>().ToArray());
        Assign(serialized, "pages", pages.Cast<UnityEngine.Object>().ToArray());
        serialized.FindProperty("description").objectReferenceValue = description;
        serialized.FindProperty("descriptionTitle").objectReferenceValue = headingText;
        serialized.FindProperty("saveNotice").objectReferenceValue = saveText;
        serialized.FindProperty("koreanTitle").objectReferenceValue = title.gameObject;
        serialized.FindProperty("translatedTitle").objectReferenceValue = translatedTitle;
        Assign(serialized, "definitions", CreateDefinitions().Cast<UnityEngine.Object>().ToArray());
        serialized.ApplyModifiedPropertiesWithoutUndo();
        ConfigureEffects(view);
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.AutomatedAction);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(70, 82, 81, 255);
        camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0, 0, -10);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        CreateReopenButton(view);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssetIfDirty(bodyFont);
        AssetDatabase.SaveAssetIfDirty(tabFont);
        Selection.activeGameObject = root.gameObject;
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        Debug.Log("Settings dialog created: " + PrefabPath + " | " + ScenePath);
    }

    [MenuItem("Tools/UI/Settings Dialog/Update Effects and Dropdowns")]
    public static void UpdateEffects()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before updating the prefab.");
        ImportSprites();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureEffects(root.GetComponent<SettingsDialogView>());
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path == ScenePath)
        {
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("Settings effects updated; existing layout and localization preserved.");
    }

    private static void ConfigureEffects(SettingsDialogView view)
    {
        var modal = view.transform.Find("Modal");
        var window = modal.Find("Window") as RectTransform;
        var group = modal.GetComponent<CanvasGroup>();
        if (group == null) group = modal.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1; group.interactable = true; group.blocksRaycasts = true;
        var motion = view.GetComponent<SettingsDialogMotion>();
        if (motion == null) motion = view.gameObject.AddComponent<SettingsDialogMotion>();
        var motionData = new SerializedObject(motion);
        motionData.FindProperty("window").objectReferenceValue = window;
        motionData.FindProperty("fadeGroup").objectReferenceValue = group;
        motionData.ApplyModifiedPropertiesWithoutUndo();
        var data = new SerializedObject(view);
        data.FindProperty("motion").objectReferenceValue = motion;
        data.ApplyModifiedPropertiesWithoutUndo();

        foreach (var selectable in view.GetComponentsInChildren<Selectable>(true))
        {
            var colors = selectable.colors;
            colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
            selectable.colors = colors;
        }
        var close = window.Find("Close").GetComponent<Image>();
        var button = close.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        close.sprite = null; close.color = Color.clear; close.raycastTarget = true;
        var art = (close.transform.Find("ClickFeedback/Artwork") ?? close.transform.Find("Artwork"))?.GetComponent<Image>();
        if (art == null)
        {
            art = Image("Artwork", close.transform, "Close", 0, 0, close.rectTransform.rect.width, close.rectTransform.rect.height);
            Stretch(art.rectTransform); art.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        var hover = close.GetComponent<SettingsCloseButtonHover>();
        if (hover == null) hover = close.gameObject.AddComponent<SettingsCloseButtonHover>();
        var hoverData = new SerializedObject(hover);
        hoverData.FindProperty("button").objectReferenceValue = button;
        hoverData.FindProperty("artwork").objectReferenceValue = art;
        hoverData.FindProperty("normalSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Sprites/Close.png");
        hoverData.FindProperty("hoverSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Sprites/CloseHover.png");
        hoverData.ApplyModifiedPropertiesWithoutUndo();
        ConfigureButtonFeedback(view);

        var dividerBlend = BlendMaterial("OverDropdown", new Color32(190, 117, 79, 255));
        foreach (var row in view.GetComponentsInChildren<SettingsDialogRow>(true))
        {
            if (row.dropdown == null) continue;
            var dropdown = row.dropdown as SettingsDropdown;
            if (dropdown == null)
            {
                var previous = row.dropdown;
                var caption = previous.captionText;
                var graphic = previous.targetGraphic;
                var options = previous.options;
                var changed = previous.onValueChanged;
                bool interactable = previous.interactable;
                UnityEngine.Object.DestroyImmediate(previous);
                dropdown = row.gameObject.transform.Find("Dropdown").gameObject.AddComponent<SettingsDropdown>();
                dropdown.captionText = caption; dropdown.targetGraphic = graphic;
                dropdown.options = options; dropdown.onValueChanged = changed; dropdown.interactable = interactable;
                row.dropdown = dropdown;
            }
            dropdown.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var colors = dropdown.colors; colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f); dropdown.colors = colors;
            BuildDropdownTemplate(dropdown, dividerBlend);
        }
    }

    [MenuItem("Tools/UI/Settings Dialog/Update Button Feedback")]
    public static void UpdateButtonFeedback()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before updating the prefab.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureButtonFeedback(root.GetComponent<SettingsDialogView>());
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path == ScenePath) EditorSceneManager.SaveScene(scene);
        Debug.Log("Settings tab and close button feedback updated.");
    }

    private static void ConfigureButtonFeedback(SettingsDialogView view)
    {
        var window = view.transform.Find("Modal/Window");
        var data = new SerializedObject(view);
        var tabs = data.FindProperty("tabButtons");
        for (int i = 0; i < tabs.arraySize; i++)
        {
            var button = (Button)tabs.GetArrayElementAtIndex(i).objectReferenceValue;
            var tab = button.transform.parent;
            var visual = tab.Find("Feedback") as RectTransform;
            if (visual == null)
            {
                visual = FeedbackRect("Feedback", tab);
                var source = button.GetComponent<Image>();
                var background = FeedbackRect("Background", visual).gameObject.AddComponent<Image>();
                EditorUtility.CopySerialized(source, background);
                background.raycastTarget = false;
                // Retain the user's existing text, fonts, outlines and positions.
                foreach (string name in new[] { "LabelShadow", "Label" })
                {
                    var label = tab.Find(name);
                    if (label != null) label.SetParent(visual, false);
                }
                source.sprite = null; source.color = Color.clear; source.material = null;
            }
            button.transition = Selectable.Transition.None;
            ConfigureFeedback(button, visual, true);
        }
        var close = window.Find("Close").GetComponent<Button>();
        var punch = close.transform.Find("ClickFeedback") as RectTransform;
        if (punch == null)
        {
            punch = FeedbackRect("ClickFeedback", close.transform);
            close.transform.Find("Artwork").SetParent(punch, false);
        }
        // A separate scale layer lets the existing hover enlargement and spin finish independently.
        ConfigureFeedback(close, punch, false);
    }

    private static RectTransform FeedbackRect(string name, Transform parent)
    {
        var rect = Rect(name, parent, 0, 0, 0, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Stretch(rect);
        return rect;
    }

    private static void ConfigureFeedback(Button button, RectTransform visual, bool animateHover)
    {
        var feedback = button.GetComponent<SettingsButtonFeedback>();
        if (feedback == null) feedback = button.gameObject.AddComponent<SettingsButtonFeedback>();
        var data = new SerializedObject(feedback);
        data.FindProperty("button").objectReferenceValue = button;
        data.FindProperty("artwork").objectReferenceValue = visual;
        data.FindProperty("animateHover").boolValue = animateHover;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildDropdownTemplate(TMP_Dropdown dropdown, Material dividerBlend)
    {
        var previous = dropdown.transform.Find("Template");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        float width = ((RectTransform)dropdown.transform).rect.width;
        var template = Rect("Template", dropdown.transform, 0, 49, width, 346);
        var background = template.gameObject.AddComponent<Image>();
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Sprites/DropdownPanel.png");
        background.type = UnityEngine.UI.Image.Type.Sliced;
        var scroll = template.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
        var viewport = Rect("Viewport", template, 5, 5, width - 10, 336);
        Stretch(viewport); viewport.offsetMin = new Vector2(5, 5); viewport.offsetMax = new Vector2(-5, -5);
        viewport.gameObject.AddComponent<RectMask2D>();
        // Clip highlights to the INNER radius (13 px outer radius minus the 5 px border).
        var roundedClip = viewport.gameObject.AddComponent<Image>();
        roundedClip.sprite = background.sprite; roundedClip.type = UnityEngine.UI.Image.Type.Sliced;
        roundedClip.pixelsPerUnitMultiplier = 13f / 8f;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var content = Rect("Content", viewport, 0, 0, width - 10, 48);
        var item = Rect("Item", content, 0, 0, width - 10, 48);
        var hit = item.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        var toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = hit; toggle.transition = Selectable.Transition.None; toggle.toggleTransition = Toggle.ToggleTransition.None;
        var highlight = Rect("Highlight", item, 0, 0, width - 10, 48).gameObject.AddComponent<Image>();
        highlight.raycastTarget = false; highlight.color = Color.clear;
        var check = Image("SelectedCheck", item, "DropdownCheck", width - 39, 11, 24, 23.497f);
        toggle.graphic = check; toggle.isOn = false;
        dropdown.itemText = Text("ItemLabel", item, "-", 8, 3, width - 52, 42, 24, dropdown.captionText.font, Color.white, TextAlignmentOptions.Center);
        dropdown.itemText.enableAutoSizing = true; dropdown.itemText.fontSizeMin = 17; dropdown.itemText.fontSizeMax = 24;
        var divider = Image("Divider", item, "DropdownDivider", 14, 45.5f, width - 38, 5);
        divider.type = UnityEngine.UI.Image.Type.Sliced; divider.material = dividerBlend;
        var visual = item.gameObject.AddComponent<SettingsDropdownOption>();
        var visualData = new SerializedObject(visual);
        visualData.FindProperty("highlight").objectReferenceValue = highlight;
        visualData.FindProperty("divider").objectReferenceValue = divider;
        visualData.ApplyModifiedPropertiesWithoutUndo();
        scroll.viewport = viewport; scroll.content = content;
        dropdown.template = template; dropdown.alphaFadeSpeed = 0.08f;
        template.gameObject.SetActive(false);
    }

    private static TMP_Dropdown Dropdown(RectTransform parent)
    {
        var field = Image("Dropdown", parent, "Field", 280, 6, 243, 41); field.type = UnityEngine.UI.Image.Type.Sliced;
        field.material = contentBlend;
        field.raycastTarget = true;
        var dropdown = field.gameObject.AddComponent<SettingsDropdown>();
        dropdown.targetGraphic = field;
        dropdown.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        dropdown.captionText = Text("Value", field.rectTransform, "", 9, 0, 199, 41, 24, bodyFont, Color.white, TextAlignmentOptions.Center);
        dropdown.captionText.enableAutoSizing = true; dropdown.captionText.fontSizeMin = 17; dropdown.captionText.fontSizeMax = 24;
        Image("Arrow", field.rectTransform, "Arrow", 212, 15.5f, 22, 13);
        dropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("-") };
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static void CreateSlider(RectTransform parent, SettingsDialogRow row)
    {
        var root = Rect("Slider", parent, 280, 0, 180, 54);
        var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        var track = Image("Track", root, "Field", 0, 17, 180, 13); track.type = UnityEngine.UI.Image.Type.Sliced; track.color = new Color(1, 1, 1, 0.35f);
        var fillArea = Rect("FillArea", root, 10, 17, 160, 13);
        var fill = Image("Fill", fillArea, "Field", 0, 0, 160, 13); Stretch(fill.rectTransform); fill.type = UnityEngine.UI.Image.Type.Sliced;
        var handleArea = Rect("HandleArea", root, 10, 15, 160, 24);
        var handle = Image("Handle", handleArea, "Bullet", 0, 0, 24, 0);
        handle.rectTransform.pivot = new Vector2(0.5f, 0.5f); handle.raycastTarget = true;
        row.slider = root.gameObject.AddComponent<Slider>(); row.slider.minValue = 0; row.slider.maxValue = 1;
        row.slider.fillRect = fill.rectTransform; row.slider.handleRect = handle.rectTransform; row.slider.targetGraphic = handle;
        row.valueText = Text("Percent", parent, "0%", 470, 0, 53, 54, 24, bodyFont, Color.white, TextAlignmentOptions.Center);
    }

    private static void CreateReopenButton(SettingsDialogView view)
    {
        var root = Rect("TestControls", null, 0, 0, 1920, 1080);
        root.gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var r = Rect("OpenSettings", root, 32, 30, 200, 60);
        var bg = r.gameObject.AddComponent<Image>(); bg.color = new Color32(145, 76, 48, 255);
        var button = Button(bg); Text("Label", r, "설정 열기", 0, 0, 200, 60, 27, bodyFont, Color.white, TextAlignmentOptions.Center);
        UnityEventTools.AddPersistentListener(button.onClick, view.Open);
    }

    private static void ImportSprites()
    {
        foreach (string file in Directory.GetFiles(Folder + "/Sprites", "*.png"))
        {
            string path = file.Replace('\\', '/');
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Path.GetFileNameWithoutExtension(path) == "Panel" ? 100 : Path.GetFileNameWithoutExtension(path) == "Content" ? 200 : 400; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            string name = Path.GetFileNameWithoutExtension(path);
            float border = name == "Field" ? 52 : name == "DropdownPanel" ? 72 : name.StartsWith("Scroll") ? 28 : 0;
            importer.spriteBorder = new Vector4(border, border, border, border);
            if (name == "DropdownDivider") importer.spriteBorder = new Vector4(10, 0, 10, 0);
            var platform = importer.GetPlatformTextureSettings("WebGL"); platform.overridden = false; importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
        }
    }

    private static TMP_FontAsset Font(string name)
    {
        string path = SharedFontFolder + "/" + name + "/" + name + " SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing == null) throw new InvalidOperationException("Missing shared TMP font: " + path);
        return existing;
    }

    private static SettingsDialogDefinition[] CreateDefinitions()
    {
        Directory.CreateDirectory(Folder + "/Descriptions");
        var result = new List<SettingsDialogDefinition>();
        foreach (SettingsEntry entry in Enum.GetValues(typeof(SettingsEntry)))
        {
            string path = Folder + "/Descriptions/" + entry + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<SettingsDialogDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SettingsDialogDefinition>();
                var data = new SerializedObject(asset);
                data.FindProperty("entry").enumValueIndex = (int)entry;
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(asset, path);
            }
            // Rebuilding the visual prefab must preserve authored descriptions.
            result.Add(asset);
        }
        return result.ToArray();
    }

    private static Material BlendMaterial(string name, Color background)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("WeGrowPeas/UI/Figma Alpha Blend")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetVector("_BackgroundLinear", (Vector4)background.linear);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    private static void Assign(SerializedObject obj, string name, UnityEngine.Object[] values)
    { var p = obj.FindProperty(name); p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
    private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        if (parent != null) r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        return r;
    }
    private static RectTransform DesignRect(string name, Transform parent, float x, float y, float w, float h) => Rect(name, parent, x - OriginX, y - OriginY, w, h);
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    private static Image Art(string name, Transform parent, string sprite, float x, float y, float w, float h) => Image(name, parent, sprite, x - OriginX, y - OriginY, w, h);
    private static Image Image(string name, Transform parent, string sprite, float x, float y, float w, float h)
    { var r = Rect(name, parent, x, y, w, h); var image = r.gameObject.AddComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Sprites/" + sprite + ".png"); image.raycastTarget = false; return image; }
    private static Button Button(Image image)
    {
        image.raycastTarget = true; var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var c = button.colors; c.highlightedColor = c.selectedColor = Color.white; c.pressedColor = new Color(0.9f, 0.9f, 0.9f); c.fadeDuration = 0.07f; button.colors = c; return button;
    }
    private static TextMeshProUGUI Text(string name, Transform parent, string value, float x, float y, float w, float h, float size, TMP_FontAsset font, Color color, TextAlignmentOptions alignment)
    {
        var t = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font; t.text = value; t.fontSize = size; t.color = color; t.alignment = alignment;
        t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false;
        return t;
    }
}
