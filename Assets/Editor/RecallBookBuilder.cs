using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeGrowPeas.RecallBook;

/// <summary>Creates a self-contained example prefab without opening or saving a gameplay scene.</summary>
public static class RecallBookBuilder
{
    public const string Folder = "Assets/Resources/Prefabs/Recall/Book";
    public const string PrefabPath = Folder + "/RecallBookExample.prefab";
    private static readonly Color Ink = new Color(0.63f, 0.30f, 0.16f);
    private static TMP_FontAsset font;

    [MenuItem("Tools/Recall Book/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        const string path = "Assets/Scenes/RecallBookDemo.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            return;
        }
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Create the demo scene outside Play Mode.");
        CreateExample();
        var previousScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.23f, 0.22f);
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 1000);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var book = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas.transform);
            ((RectTransform)book.transform).anchoredPosition = new Vector2(0, -12);
            var heading = Label("Heading", canvas.transform, new Vector2(0, 437), new Vector2(1000, 50), "GARDEN MEMORIES", 34, new Color(1, 0.91f, 0.77f));
            heading.alignment = TextAlignmentOptions.Center;
            var hint = Label("Hint", canvas.transform, new Vector2(0, -450), new Vector2(1100, 45), "Use < and > to turn pages  /  2D artwork + a curved UI sheet", 20, new Color(0.80f, 0.86f, 0.77f));
            hint.alignment = TextAlignmentOptions.Center;
            EditorSceneManager.SaveScene(scene, path);
        }
        finally
        {
            if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
    }

    [MenuItem("Tools/Recall Book/Create Example Prefab")]
    public static void CreateExample()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Mongtori/Mongtori.asset");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return;
        }
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var shader = Shader.Find("WeGrowPeas/UI/Toon Book Page");
        if (shader == null) throw new InvalidOperationException("Book shader has not imported yet.");
        string materialPath = Folder + "/ToonBookPage.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Mongtori/Mongtori.asset");
        if (font == null) font = TMP_Settings.defaultFontAsset;
        Texture2D photo = MakeGarden();
        var root = Rect("RecallBookExample", null);
        root.gameObject.SetActive(false);
        try
        {
            root.sizeDelta = new Vector2(1180, 800);
            var example = root.gameObject.AddComponent<RecallBookExample>();
            var turner = root.gameObject.AddComponent<BookPageTurner>();
            var snapshot = root.gameObject.AddComponent<BookPageSnapshot>();
            Panel("DropShadow", root, new Vector2(0, -12), new Vector2(1152, 760), new Color(0.1f, 0.07f, 0.04f, 0.3f));
            Panel("Cover", root, Vector2.zero, new Vector2(1140, 748), new Color(0.77f, 0.43f, 0.25f));
            // Paper thickness is part of the user's cover artwork.
            var spread = Rect("Pages", root);
            spread.sizeDelta = new Vector2(1080, 680);
            spread.anchoredPosition = new Vector2(0, 14);
            var group = spread.gameObject.AddComponent<CanvasGroup>();
            var titles = new TMP_Text[8];
            var descriptions = new TMP_Text[8];
            var photos = new RawImage[8];
            var buttons = new Button[8];
            TMP_Text leftNumber = null, rightNumber = null;
            for (int side = 0; side < 2; side++)
            {
                var page = Panel(side == 0 ? "LeftPage" : "RightPage", spread,
                    new Vector2(side == 0 ? -270 : 270, 0), new Vector2(540, 680), new Color(1f, 0.91f, 0.81f));
                Label("PageHeading", page, new Vector2(0, 300), new Vector2(455, 35),
                    side == 0 ? "OUR LITTLE GARDEN" : "MOMENTS TO KEEP", 20, Ink);
                for (int row = 0; row < 4; row++)
                {
                    int index = side * 4 + row;
                    float y = 209 - row * 130;
                    var card = Panel("Entry" + index, page, new Vector2(side == 0 ? -4 : 4, y),
                        new Vector2(462, 114), new Color(1, 0.97f, 0.91f));
                    var button = card.gameObject.AddComponent<Button>();
                    button.targetGraphic = card.GetComponent<Image>();
                    var colors = button.colors;
                    colors.highlightedColor = new Color(1, 0.94f, 0.82f);
                    colors.pressedColor = new Color(0.95f, 0.83f, 0.66f);
                    // Lock input during a turn without dimming the printed card underneath.
                    colors.disabledColor = colors.normalColor;
                    colors.fadeDuration = 0;
                    button.colors = colors;
                    buttons[index] = button;
                    bool photoFirst = row % 2 == 0;
                    var photoRect = Rect("Photo", card);
                    photoRect.sizeDelta = new Vector2(184, 96);
                    photoRect.anchoredPosition = new Vector2(photoFirst ? -130 : 130, 0);
                    photos[index] = photoRect.gameObject.AddComponent<RawImage>();
                    photos[index].texture = photo;
                    photos[index].raycastTarget = false;
                    float textX = photoFirst ? 92 : -92;
                    titles[index] = Label("Title", card, new Vector2(textX, 25), new Vector2(223, 38), "MEMORY 01", 25, Ink);
                    descriptions[index] = Label("Description", card, new Vector2(textX, -18), new Vector2(223, 48),
                        "DAY 12 / PEA GARDEN\n2026.09.01", 15, Ink);
                }
                var number = Label("PageNumber", page, new Vector2(0, -311), new Vector2(300, 30), side == 0 ? "1 / 6" : "2 / 6", 18, Ink);
                number.alignment = TextAlignmentOptions.Center;
                if (side == 0) leftNumber = number; else rightNumber = number;
            }
            var effects = Rect("PageTurnOverlay", root);
            effects.sizeDelta = spread.sizeDelta;
            effects.anchoredPosition = spread.anchoredPosition;
            var stationary = Rect("StationaryPage", effects).gameObject.AddComponent<RawImage>();
            stationary.raycastTarget = false;
            stationary.gameObject.SetActive(false);
            // Keep the binding above the resting page, but below the moving sheet so only
            // the part physically covered by that sheet is hidden.
            var binding = Rect("Binding", effects);
            Stretch(binding);
            var shadow = Rect("TurningShadow", effects).gameObject.AddComponent<Image>();
            shadow.raycastTarget = false;
            shadow.gameObject.SetActive(false);
            var sheetRect = Rect("TurningSheet", effects);
            Stretch(sheetRect);
            var sheet = sheetRect.gameObject.AddComponent<BookPageCurlGraphic>();
            sheet.raycastTarget = false;
            sheet.material = material;
            sheet.gameObject.SetActive(false);
            // Leave Binding empty for the user's artwork, beneath the moving paper.
            var previous = Navigation(root, "Previous", "<", new Vector2(-485, -324));
            var next = Navigation(root, "Next", ">", new Vector2(485, -324));
            Assign(turner, ("pageRoot", spread), ("snapshot", snapshot), ("stationaryPage", stationary),
                ("turningPage", sheet), ("castShadow", shadow), ("pageInteraction", group), ("pageMaterial", material),
                ("leftPage", spread.Find("LeftPage")), ("rightPage", spread.Find("RightPage")));
            Assign(example, ("turner", turner), ("leftNumber", leftNumber), ("rightNumber", rightNumber),
                ("previous", previous), ("next", next), ("demoPhoto", photo));
            AssignArray(example, "titles", titles);
            AssignArray(example, "descriptions", descriptions);
            AssignArray(example, "photos", photos);
            AssignArray(example, "entries", buttons);
            root.gameObject.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            Debug.Log("[Recall Book] Created " + PrefabPath + ". Place under a Canvas with an EventSystem.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root.gameObject); }
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var rect = Rect(name, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = name.StartsWith("Entry", StringComparison.Ordinal);
        return rect;
    }

    private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, Color color)
    {
        var rect = Rect(name, parent);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static Button Navigation(Transform parent, string name, string label, Vector2 position)
    {
        var rect = Panel(name, parent, position, new Vector2(66, 52), new Color(1, 1, 1, 0));
        rect.GetComponent<Image>().raycastTarget = true;
        var text = Label("Label", rect, Vector2.zero, new Vector2(66, 52), label, 32, Ink);
        text.alignment = TextAlignmentOptions.Center;
        return rect.gameObject.AddComponent<Button>();
    }

    private static void Assign(UnityEngine.Object target, params (string, UnityEngine.Object)[] fields)
    {
        var serialized = new SerializedObject(target);
        foreach (var field in fields) serialized.FindProperty(field.Item1).objectReferenceValue = field.Item2;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignArray<T>(UnityEngine.Object target, string name, T[] values) where T : UnityEngine.Object
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(name);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Texture2D MakeGarden()
    {
        string path = Folder + "/ExampleGarden.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        const int w = 368, h = 192;
        var texture = new Texture2D(w, h, TextureFormat.RGB24, false);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            Color color = ((x / 14 + y / 14) % 2 == 0) ? new Color(0.64f, 0.76f, 0.40f) : new Color(0.68f, 0.79f, 0.43f);
            if (x > 38 && x < 330 && y > 22 && y < 170)
            {
                color = new Color(0.47f, 0.29f, 0.15f);
                if (x > 46 && x < 322 && y > 30 && y < 162)
                {
                    color = (x / 24 % 2 == 0) ? new Color(0.66f, 0.44f, 0.23f) : new Color(0.72f, 0.51f, 0.28f);
                    float dx = (x - 52) % 44 - 18, dy = (y - 36) % 40 - 16;
                    if (dx * dx / 160 + dy * dy / 115 < 1) color = new Color(0.29f, 0.43f, 0.15f);
                    if (dx * dx / 112 + (dy - 2) * (dy - 2) / 72 < 1) color = new Color(0.73f, 0.84f, 0.39f);
                    if (Mathf.Abs(dx - 4) < 1.5f && Mathf.Abs(dy - 3) < 2 || Mathf.Abs(dx + 4) < 1.5f && Mathf.Abs(dy - 3) < 2)
                        color = new Color(0.18f, 0.27f, 0.13f);
                }
            }
            pixels[y * w + x] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
