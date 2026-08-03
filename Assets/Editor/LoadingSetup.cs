using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로딩창 관련 에셋/프리팹 생성.
/// - TmiConfig: 로딩 문구 목록(Resources/Data)
/// - LoadingScreen 프리팹: 퍼센트 바 + 스피너 + TMI 텍스트 (스타일은 이후 인스펙터에서 보완)
/// </summary>
public static class LoadingSetup
{
    private const string DataDir = "Assets/Resources/Data";
    private const string PrefabDir = "Assets/Resources/Prefabs/Loading";

    [MenuItem("Tools/Loading/Create Tmi Config")]
    public static void CreateTmiConfig()
    {
        EnsureFolder(DataDir);
        string path = $"{DataDir}/TmiConfig.asset";

        var existing = AssetDatabase.LoadAssetAtPath<TmiConfig>(path);
        if (existing != null)
        {
            Debug.Log("[Loading] TmiConfig 에셋이 이미 있습니다.");
            Selection.activeObject = existing;
            return;
        }

        var so = ScriptableObject.CreateInstance<TmiConfig>();
        so.tips.AddRange(TmiPool.DefaultTips);
        AssetDatabase.CreateAsset(so, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Loading] TmiConfig 생성 ({so.tips.Count}개 문구). 에셋에서 자유롭게 추가·수정하세요.");
        Selection.activeObject = so;
    }

    [MenuItem("Tools/Loading/Create Loading Screen Prefab")]
    public static void CreateLoadingScreenPrefab()
    {
        EnsureFolder(PrefabDir);
        string path = $"{PrefabDir}/LoadingScreen.prefab";

        // ── 루트: Canvas (최상단에 그려지도록 sortingOrder 높게) ──
        var go = new GameObject("LoadingScreen",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        // ── Root (켜고 끄는 대상) ──
        var root = NewUI("Root", go.transform);
        Stretch(root);
        AddImage(root, new Color(0.04f, 0.05f, 0.08f, 1f)); // 배경

        // ── 스피너 ──
        var spinner = NewUI("Spinner", root.transform);
        Anchor(spinner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-60, -60), new Vector2(60, 60));
        AddImage(spinner, Color.white);
        spinner.AddComponent<SpinnerRotator>();

        // ── 퍼센트 바 ──
        var barRoot = NewUI("ProgressBar", root.transform);
        Anchor(barRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400, 180), new Vector2(400, 210));
        AddImage(barRoot, new Color(1f, 1f, 1f, 0.15f));

        var slider = barRoot.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;

        var fillArea = NewUI("Fill Area", barRoot.transform);
        Stretch(fillArea);
        var fill = NewUI("Fill", fillArea.transform);
        Stretch(fill);
        var fillImg = AddImage(fill, new Color(0.45f, 0.75f, 1f, 1f));
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImg;

        // ── 퍼센트 텍스트 ──
        var percent = NewText("PercentText", root.transform, "0%", 32, TextAlignmentOptions.Center);
        Anchor(percent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-200, 215), new Vector2(200, 265));

        // ── TMI 텍스트 ──
        var tmi = NewText("TmiText", root.transform, "", 28, TextAlignmentOptions.Center);
        Anchor(tmi, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-600, 90), new Vector2(600, 165));

        // ── 컴포넌트 연결 ──
        var screen = go.AddComponent<LoadingScreen>();
        var so = new SerializedObject(screen);
        so.FindProperty("root").objectReferenceValue = root;
        so.FindProperty("canvasGroup").objectReferenceValue = group;
        so.FindProperty("progressBar").objectReferenceValue = slider;
        so.FindProperty("percentText").objectReferenceValue = percent.GetComponent<TMP_Text>();
        so.FindProperty("tmiText").objectReferenceValue = tmi.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"[Loading] LoadingScreen 프리팹 생성: {path}\n" +
                  "시작 씬(StartScene 등)에 배치하면 DontDestroyOnLoad로 계속 유지됩니다.");
        Selection.activeObject = prefab;
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
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
