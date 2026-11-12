using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeGrowPeas.PeaMerge;

/// <summary>Authors ordinary editable scene objects. Existing scenes and source pea assets are untouched.</summary>
public static class PeaMergeSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/PeaMerge.unity";
    public const string DataFolder = "Assets/Resource/PeaMerge";
    public const string SettingsPath = DataFolder + "/PeaMergeSettings.asset";
    private const string SourcePrefab = "Assets/Resource/Prefabs/StartScene/StartPeaSoftPrefab.prefab";
    private static readonly Color Ink = new Color(0.2f, 0.25f, 0.22f);
    private static readonly Color Panel = new Color(0.9f, 0.92f, 0.88f);
    private static TMP_FontAsset font;

    [MenuItem("Tools/Pea Merge/Create Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Create the scene outside Play Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            return;
        }
        Directory.CreateDirectory(DataFolder);
        AssetDatabase.Refresh();
        var settings = CreateSettings();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard/Pretendard-Medium SDF.asset");
        if (font == null) font = TMP_Settings.defaultFontAsset;
        var previous = SceneManager.GetActiveScene();
        var previousLights = new System.Collections.Generic.List<Light2D>();
        foreach (var existing in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (existing.enabled && existing.lightType == Light2D.LightType.Global) previousLights.Add(existing);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            // URP registers lights across additive scenes. Restore these immediately after closing
            // the temporary scene so authoring never reports duplicate global lights.
            foreach (var existing in previousLights) existing.enabled = false;
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 9.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.97f, 0.97f, 0.94f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            var light = new GameObject("Global Light 2D", typeof(Light2D)).GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            var lightData = new SerializedObject(light);
            var layers = lightData.FindProperty("m_ApplyToSortingLayers");
            layers.arraySize = SortingLayer.layers.Length;
            for (int i = 0; i < SortingLayer.layers.Length; i++) layers.GetArrayElementAtIndex(i).intValue = SortingLayer.layers[i].id;
            lightData.ApplyModifiedPropertiesWithoutUndo();

            var game = new GameObject("Pea Merge Game", typeof(PeaMergeGame)).GetComponent<PeaMergeGame>();
            var pieces = new GameObject("Runtime Peas").transform;
            pieces.SetParent(game.transform);
            var board = new GameObject("Board").transform;
            board.SetParent(game.transform);
            float h = settings.wallTopY - settings.floorY;
            Boundary("Left Wall", board, new Vector2(-settings.halfWidth - 0.12f, settings.floorY + h / 2), new Vector2(0.24f, h + 0.3f));
            Boundary("Right Wall", board, new Vector2(settings.halfWidth + 0.12f, settings.floorY + h / 2), new Vector2(0.24f, h + 0.3f));
            Boundary("Floor", board, new Vector2(0, settings.floorY - 0.12f), new Vector2(settings.halfWidth * 2 + 0.48f, 0.24f));
            var lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(DataFolder + "/BoardLines.mat");
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(lineMaterial, DataFolder + "/BoardLines.mat");
            }
            Line("Container Outline", board, lineMaterial, new Color(0.4f, 0.48f, 0.41f), 0.08f,
                new Vector3(-settings.halfWidth, settings.wallTopY), new Vector3(-settings.halfWidth, settings.floorY),
                new Vector3(settings.halfWidth, settings.floorY), new Vector3(settings.halfWidth, settings.wallTopY));
            var guide = Line("Drop Guide", board, lineMaterial, new Color(0.48f, 0.59f, 0.47f, 0.35f), 0.025f,
                new Vector3(0, settings.dropY), new Vector3(0, settings.floorY));

            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvas.sortingOrder = 10;
            var root = Rect("Portrait UI - 1080 x 1920", canvas.transform, Vector2.zero, new Vector2(1080, 1920));
            var layout = canvas.gameObject.AddComponent<PeaMergePortraitLayout>();
            layout.gameCamera = camera;
            layout.portraitRoot = root;
            // Opaque bars outside the portrait root also clear wide desktop aspect ratios.
            var bars = Rect("Outside Portrait", canvas.transform, Vector2.zero, Vector2.zero);
            bars.SetAsFirstSibling();
            // Cameras clear their viewport. The overlay mask below covers the rest of the display.
            var mask = canvas.gameObject.AddComponent<PeaMergeLetterbox>();
            mask.portraitRoot = root;
            mask.barsRoot = bars;

            var view = root.gameObject.AddComponent<PeaMergeUI>();
            Label("Title", root, new Vector2(-120, 865), new Vector2(660, 80), "완두콩 합치기", 49, TextAlignmentOptions.MidlineLeft);
            view.pauseButton = Button("Pause Button", root, new Vector2(372, 865), new Vector2(156, 72), "일시정지", 27);
            var score = Image("Score Panel", root, new Vector2(-235, 735), new Vector2(430, 150), Panel);
            Label("Label", score.transform, new Vector2(0, 43), new Vector2(360, 40), "점수", 27);
            view.scoreText = Label("Score", score.transform, new Vector2(0, -24), new Vector2(360, 75), "0", 52);
            var best = Image("Best Panel", root, new Vector2(235, 735), new Vector2(430, 150), Panel);
            Label("Label", best.transform, new Vector2(0, 43), new Vector2(360, 40), "최고 점수", 27);
            view.bestText = Label("Best Score", best.transform, new Vector2(0, -24), new Vector2(360, 75), "0", 52);
            Label("Drop Label", root, new Vector2(-245, 590), new Vector2(410, 48), "같은 완두콩끼리 합쳐보세요", 26, TextAlignmentOptions.MidlineLeft);
            view.nextText = Label("Next Label", root, new Vector2(235, 600), new Vector2(230, 44), "다음", 27);
            var next = Rect("Next Pea", root, new Vector2(400, 600), new Vector2(80, 80));
            view.nextBody = Image("Body", next, Vector2.zero, new Vector2(80, 80), Color.white);
            view.nextBody.sprite = settings.tiers[0].bodyIcon;
            view.nextBody.preserveAspect = true;
            view.nextAccessory = Image("Accessory", next, Vector2.zero, new Vector2(80, 80), Color.white);
            view.nextAccessory.preserveAspect = true;
            view.nextAccessory.enabled = false;

            var inputImage = Image("Drop Input Area", root, new Vector2(0, -65), new Vector2(920, 1160), Color.clear);
            inputImage.raycastTarget = true;
            var input = inputImage.gameObject.AddComponent<PeaMergeInput>();
            input.game = game;
            input.gameCamera = camera;
            view.dangerLine = Image("Danger Line", root, new Vector2(0, settings.dangerY * 100), new Vector2(settings.halfWidth * 200, 3), new Color(0.66f, 0.53f, 0.48f, 0.65f));
            view.dangerText = Label("Danger Label", root, new Vector2(0, settings.dangerY * 100 + 35), new Vector2(850, 40), "이 선을 넘겨 쌓이면 게임 종료", 24);
            view.hintText = Label("Controls Hint", root, new Vector2(0, -665), new Vector2(960, 70), "좌우로 움직인 뒤 손을 떼면 떨어집니다", 27);
            Label("Evolution Label", root, new Vector2(0, -755), new Vector2(920, 44), "합체 순서", 27);
            var evolution = Rect("Evolution Row", root, new Vector2(0, -835), new Vector2(920, 110));
            for (int i = 0; i < settings.tiers.Length; i++)
            {
                var tier = settings.tiers[i];
                var slot = Rect($"Tier {i + 1} - {tier.label}", evolution, new Vector2((i - 4) * 102, 0), new Vector2(86, 105));
                var body = Image("Body", slot, new Vector2(0, 12), new Vector2(68, 68), Color.white);
                body.sprite = tier.bodyIcon;
                body.preserveAspect = true;
                var accessory = Image("Accessory", slot, new Vector2(0, 12), new Vector2(68, 68), tier.accessoryTint);
                accessory.sprite = tier.accessoryIcon;
                accessory.preserveAspect = true;
                accessory.enabled = tier.accessoryIcon != null;
                Label("Stage", slot, new Vector2(0, -39), new Vector2(86, 30), (i + 1).ToString(), 22);
            }
            var modal = Image("Pause and Game Over Overlay", root, Vector2.zero, new Vector2(1080, 1920), new Color(0.13f, 0.18f, 0.15f, 0.64f));
            modal.raycastTarget = true;
            var card = Image("Dialog", modal.transform, Vector2.zero, new Vector2(840, 560), new Color(0.98f, 0.98f, 0.96f));
            view.modal = modal.gameObject;
            view.modalTitle = Label("Title", card.transform, new Vector2(0, 177), new Vector2(750, 75), "일시정지", 53);
            view.modalScore = Label("Result", card.transform, new Vector2(0, 78), new Vector2(750, 65), "점수 0", 32);
            view.resumeButton = Button("Resume Button", card.transform, new Vector2(0, -44), new Vector2(650, 92), "계속하기", 33);
            view.restartButton = Button("Restart Button", card.transform, new Vector2(0, -167), new Vector2(650, 92), "다시 시작", 33);
            modal.gameObject.SetActive(false);
            game.Configure(settings, view, pieces, guide);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            layout.Apply();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssetIfDirty(settings);
        }
        finally
        {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            foreach (var existing in previousLights) if (existing != null) existing.enabled = true;
        }
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
    }

    [MenuItem("Tools/Pea Merge/Open Scene")]
    public static void OpenScene()
    {
        if (EditorApplication.isPlaying) return;
        CreateScene();
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    private static PeaMergeSettings CreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PeaMergeSettings>(SettingsPath);
        if (settings != null) return settings;
        settings = ScriptableObject.CreateInstance<PeaMergeSettings>();
        settings.peaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
        if (settings.peaPrefab == null) throw new InvalidOperationException("StartPeaSoftPrefab is missing.");
        int[] appearances = { 0, 2, 1, 3, 5, 4, 7, 8, 6 };
        string[] labels = { "완두콩", "마른콩", "갈색콩", "바람콩", "비옷콩", "얼음콩", "벌레콩", "더위콩", "물방울콩" };
        float[] radii = { 0.32f, 0.42f, 0.54f, 0.67f, 0.82f, 1f, 1.2f, 1.43f, 1.7f };
        var source = new SerializedObject(settings.peaPrefab.GetComponent<StartPeaAppearanceController>());
        var entries = source.FindProperty("appearances");
        settings.tiers = new PeaMergeSettings.Tier[appearances.Length];
        for (int i = 0; i < appearances.Length; i++)
        {
            var entry = entries.GetArrayElementAtIndex(appearances[i]);
            settings.tiers[i] = new PeaMergeSettings.Tier
            {
                label = labels[i], appearanceIndex = appearances[i], radius = radii[i], mergeScore = 1 << (i + 1),
                bodyIcon = entry.FindPropertyRelative("body").objectReferenceValue as Sprite,
                accessoryIcon = entry.FindPropertyRelative("accessory").objectReferenceValue as Sprite,
                accessoryTint = entry.FindPropertyRelative("accessoryTint").colorValue
            };
        }
        AssetDatabase.CreateAsset(settings, SettingsPath);
        return settings;
    }

    private static void Boundary(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.GetComponent<BoxCollider2D>().size = size;
    }

    private static LineRenderer Line(string name, Transform parent, Material material, Color color, float width, params Vector3[] points)
    {
        var line = new GameObject(name, typeof(LineRenderer)).GetComponent<LineRenderer>();
        line.transform.SetParent(parent, false);
        line.sharedMaterial = material;
        line.startColor = line.endColor = color;
        line.startWidth = line.endWidth = width;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.sortingOrder = -20;
        return line;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.text = text;
        label.color = Ink;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static Button Button(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize)
    {
        var image = Image(name, parent, position, size, Panel);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Label("Label", image.transform, Vector2.zero, size, text, fontSize);
        return button;
    }
}
