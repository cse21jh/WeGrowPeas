using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using WeGrowPeas.RecallBook;
using WeGrowPeas.UI.Book;

/// <summary>Exercises different layouts on a disposable book without saving the user's scene.</summary>
public static class BookLayoutValidation
{
    private static GameObject root;
    private static IEnumerator routine;
    private static float previousTimeScale;
    private static int lastFrame;
    public static string Result { get; private set; } = "Not run";

    [MenuItem("Tools/Book UI/Validate In Play Mode")]
    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Validation requires Play Mode.");
        if (routine != null) return;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        Result = "Running";
        root = new GameObject("Book Layout Validation", typeof(RectTransform), typeof(Canvas));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 30000;
        routine = Check();
        lastFrame = -1;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!Application.isPlaying || root == null) { Cleanup(); return; }
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try { if (!routine.MoveNext()) Cleanup(); }
        catch (Exception exception)
        {
            Result = "FAIL: " + exception;
            Debug.LogException(exception);
            Cleanup();
        }
    }

    private static IEnumerator Check()
    {
        var frame = new GameObject("Frame", typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(root.transform, false);
        frame.sizeDelta = new Vector2(1280, 920);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BookUIBuilder.PrefabPath);
        var book = UnityEngine.Object.Instantiate(prefab, frame);
        var controller = book.GetComponent<BookLayoutController>();
        var turner = controller.Turner;
        var scaler = book.GetComponent<BookRootScaler>();
        var group = turner.LeftPage.parent.GetComponent<CanvasGroup>();
        var first = controller.Layouts[0];
        var second = controller.Layouts[1];
        var binding = scaler.Content.Find("PageTurnOverlay/Binding");
        Vector3 bindingPosition = binding.localPosition;
        Label(first.leftContent, "INVENTORY", new Vector2(0, 235));
        Label(first.rightContent, "ITEM DETAILS", new Vector2(0, 235));
        for (int i = 0; i < 6; i++)
            Panel(first.leftContent, new Vector2((i % 2) * 155 - 78, 105 - (i / 2) * 145), new Vector2(125, 115),
                new Color(0.55f, 0.69f, 0.37f));
        Panel(first.rightContent, new Vector2(0, 40), new Vector2(320, 300), new Color(0.76f, 0.64f, 0.46f));
        Label(second.leftContent, "COLLECTION", new Vector2(0, 235));
        Panel(second.leftContent, new Vector2(0, 10), new Vector2(310, 355), new Color(0.58f, 0.72f, 0.76f));
        var heading = Label(second.rightContent, "SETTINGS", new Vector2(0, 235));
        var sliderRect = Panel(second.rightContent, new Vector2(0, 100), new Vector2(320, 35), new Color(0.7f, 0.7f, 0.7f));
        var slider = sliderRect.gameObject.AddComponent<Slider>();
        var fill = Panel(sliderRect, Vector2.zero, sliderRect.sizeDelta, new Color(0.9f, 0.65f, 0.23f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        slider.fillRect = fill;
        slider.value = 0.42f;
        int shown = 0;
        bool reentered = false;
        second.onShow.AddListener(() => { shown++; heading.text = "SETTINGS / READY"; reentered |= controller.TryShow(0); });
        yield return null;
        Assert.IsTrue(controller.TryValidate(out _));
        Assert.IsNull(book.GetComponent<RecallBookExample>());
        Assert.AreEqual(0, controller.CurrentIndex);
        Assert.IsTrue(first.leftContent.gameObject.activeInHierarchy);
        Assert.IsFalse(second.leftContent.gameObject.activeInHierarchy);
        Assert.IsFalse(controller.TryShow(-1));
        Assert.IsFalse(controller.TryShow(controller.LayoutCount));
        CheckRootSizing(book);
        var oldRoot = second.leftContent;
        second.leftContent = turner.LeftPage;
        Assert.IsFalse(controller.TryValidate(out _), "The paper itself cannot be a layout root.");
        second.leftContent = first.leftContent;
        Assert.IsFalse(controller.TryValidate(out _), "Layouts cannot share content roots.");
        second.leftContent = oldRoot;
        Export(book, frame, "01-inventory.png");
        var bookRect = (RectTransform)book.transform;
        bookRect.sizeDelta = scaler.ReferenceSize * 0.5f;
        Canvas.ForceUpdateCanvases();
        scaler.Content.Find("Next").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(turner.IsTurning, "Layout changes must use the real page animation.");
        Assert.AreEqual(1, controller.CurrentIndex);
        Assert.AreEqual(1, shown);
        Assert.IsFalse(reentered, "On Show cannot recursively switch layouts.");
        Assert.IsFalse(controller.TryShow(0, false), "Even immediate requests must wait for an active turn.");
        Assert.IsFalse(first.leftContent.gameObject.activeSelf);
        Assert.IsFalse(first.rightContent.gameObject.activeSelf);
        Assert.IsTrue(second.leftContent.gameObject.activeSelf);
        Assert.IsTrue(second.rightContent.gameObject.activeSelf);
        Assert.IsFalse(slider.IsInteractable());
        var sheet = book.GetComponentInChildren<BookPageCurlGraphic>(true);
        AssertSheetMatches(sheet, turner.RightPage, 0);
        AssertSheetMatches(sheet, turner.LeftPage, 1);
        // The user may resize the outer book while the animation is running as well.
        bookRect.sizeDelta = scaler.ReferenceSize * 0.6f;
        Canvas.ForceUpdateCanvases();
        AssertSheetMatches(sheet, turner.RightPage, 0);
        AssertSheetMatches(sheet, turner.LeftPage, 1);
        sheet.SetProgress(0.72f);
        Export(book, frame, "02-layout-turn.png");
        float deadline = Time.realtimeSinceStartup + 5;
        while (turner.IsTurning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(turner.IsTurning);
        Assert.IsTrue(slider.IsInteractable());
        Assert.AreApproximatelyEqual(0.42f, slider.value);
        Assert.AreEqual("SETTINGS / READY", heading.text);
        Assert.AreEqual(bindingPosition, binding.localPosition);
        // Allow controls and TMP to complete their normal post-transition frame before review.
        yield return null;
        Canvas.ForceUpdateCanvases();
        Assert.IsTrue(heading.gameObject.activeInHierarchy);
        Assert.IsTrue(heading.canvasRenderer.GetMesh().vertexCount > 0, "Live text remains rendered after the turn.");
        Export(book, frame, "03-settings.png");
        Assert.IsTrue(controller.TryShow(0));
        Assert.IsTrue(turner.IsTurning);
        deadline = Time.realtimeSinceStartup + 5;
        while (turner.IsTurning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(turner.IsTurning);
        Assert.IsTrue(controller.TryShow(1, false));
        Assert.IsFalse(turner.IsTurning);
        book.SetActive(false);
        book.SetActive(true);
        Assert.AreEqual(1, controller.CurrentIndex, "Reopening keeps the last layout.");
        Assert.AreApproximatelyEqual(0.42f, slider.value);
        turner.ReduceMotion = true;
        Assert.IsTrue(controller.TryShow(0));
        Assert.IsFalse(turner.IsTurning);
        turner.ReduceMotion = false;
        Assert.IsTrue(controller.TryShow(1));
        book.SetActive(false);
        Assert.IsFalse(turner.IsTurning);
        Assert.IsTrue(group.interactable);
        book.SetActive(true);
        Assert.IsTrue(controller.TryShow(0));
        Assert.IsTrue(turner.IsTurning);
        book.SetActive(false);
        Result = "PASS: grid/detail to illustration/slider layouts, forward/back animation, data before capture, " +
            "input locking, reentrancy/repeated requests, invalid roots, immediate/reduced motion, close/reopen state, " +
            "timeScale=0, root Width/Height and Scale resizing, resized turn alignment and unchanged binding. PNGs: Temp/BookLayoutValidation";
        Debug.Log("[Book UI] " + Result);
    }

    public static void CheckRootSizing(GameObject book)
    {
        var rect = (RectTransform)book.transform;
        var scaler = book.GetComponent<BookRootScaler>();
        var originalSize = rect.sizeDelta;
        var originalScale = rect.localScale;
        var turner = book.GetComponent<BookPageTurner>();
        var targets = new[] { scaler.Content.Find("Cover") as RectTransform, turner.LeftPage, turner.RightPage,
            scaler.Content.Find("PageTurnOverlay/Binding") as RectTransform,
            scaler.Content.Find("Next") as RectTransform, book.GetComponent<BookLayoutController>().Layouts[0].leftContent };
        try
        {
            rect.sizeDelta = scaler.ReferenceSize;
            Canvas.ForceUpdateCanvases();
            var baseline = new Vector3[targets.Length][];
            for (int i = 0; i < targets.Length; i++) baseline[i] = CornersInViewport(targets[i], rect);
            rect.sizeDelta = scaler.ReferenceSize * 0.5f;
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(Vector3.Distance(scaler.Content.localScale, new Vector3(0.5f, 0.5f, 1)) < 0.001f);
            for (int i = 0; i < targets.Length; i++)
            {
                var corners = CornersInViewport(targets[i], rect);
                for (int corner = 0; corner < 4; corner++)
                    Assert.IsTrue(Vector3.Distance(corners[corner], baseline[i][corner] * 0.5f) < 0.03f,
                        targets[i].name + " must shrink and move with the outer book.");
            }
            rect.sizeDelta = new Vector2(scaler.ReferenceSize.x * 0.5f, scaler.ReferenceSize.y);
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(Vector3.Distance(scaler.Content.localScale, new Vector3(0.5f, 0.5f, 1)) < 0.001f,
                "Changing Width alone still shrinks the complete book without distortion.");
            var before = new Vector3[4];
            turner.LeftPage.GetWorldCorners(before);
            rect.localScale = originalScale * 0.7f;
            var after = new Vector3[4];
            turner.LeftPage.GetWorldCorners(after);
            Assert.AreApproximatelyEqual((before[2] - before[0]).magnitude * 0.7f,
                (after[2] - after[0]).magnitude, 0.03f);
        }
        finally
        {
            rect.sizeDelta = originalSize;
            rect.localScale = originalScale;
            Canvas.ForceUpdateCanvases();
        }
    }

    private static Vector3[] CornersInViewport(RectTransform target, RectTransform viewport)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++)
            corners[i] = viewport.InverseTransformPoint(corners[i]) - (Vector3)viewport.rect.center;
        return corners;
    }

    private static void AssertSheetMatches(BookPageCurlGraphic sheet, RectTransform page, float progress)
    {
        sheet.SetProgress(progress);
        Canvas.ForceUpdateCanvases();
        var vertices = sheet.canvasRenderer.GetMesh().vertices;
        Assert.IsTrue(vertices.Length > 0);
        var actual = new Bounds(sheet.transform.TransformPoint(vertices[0]), Vector3.zero);
        foreach (var vertex in vertices) actual.Encapsulate(sheet.transform.TransformPoint(vertex));
        var corners = new Vector3[4];
        page.GetWorldCorners(corners);
        var expected = new Bounds(corners[0], Vector3.zero);
        foreach (var corner in corners) expected.Encapsulate(corner);
        Assert.IsTrue(Vector3.Distance(actual.min, expected.min) < 0.03f &&
            Vector3.Distance(actual.max, expected.max) < 0.03f, "The turning sheet must match the resized paper.");
    }

    private static RectTransform Panel(Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var rect = new GameObject("Test Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text Label(Transform parent, string text, Vector2 position)
    {
        var label = new GameObject("Test Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.rectTransform.sizeDelta = new Vector2(430, 50);
        label.rectTransform.anchoredPosition = position;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Mongtori/Mongtori.asset");
        label.fontSize = 27;
        label.color = new Color(0.4f, 0.23f, 0.12f);
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        return label;
    }

    private static void Export(GameObject book, RectTransform frame, string filename)
    {
        RenderTexture target = null;
        var previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            book.GetComponent<BookPageSnapshot>().Capture(frame, ref target);
            RenderTexture.active = target;
            image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Temp/BookLayoutValidation");
            File.WriteAllBytes("Temp/BookLayoutValidation/" + filename, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (image != null) UnityEngine.Object.Destroy(image);
            BookPageSnapshot.Release(ref target);
        }
    }

    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
        routine = null;
        Time.timeScale = previousTimeScale;
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        root = null;
    }
}
