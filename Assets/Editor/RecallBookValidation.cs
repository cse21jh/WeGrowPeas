using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using WeGrowPeas.RecallBook;

/// <summary>Play-mode integration checks; never opens, saves, or replaces the current scene.</summary>
public static class RecallBookValidation
{
    private static float originalTimeScale;
    private static bool restoreTime;
    private static GameObject root;
    private static IEnumerator routine;
    private static int lastFrame;
    public static string Result { get; private set; } = "Not run";
    private const string Output = "Temp/RecallBookValidation";

    [MenuItem("Tools/Recall Book/Validate In Play Mode")]
    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Validation requires Play Mode.");
        if (routine != null) return;
        Result = "Running";
        root = new GameObject("Recall Book Validation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.hideFlags = HideFlags.DontSave;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 1000);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        routine = Check();
        lastFrame = -1;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!Application.isPlaying || root == null) { Cleanup(); return; }
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        try { if (!routine.MoveNext()) Cleanup(); }
        catch (Exception exception) { Fail(exception); Cleanup(); }
    }

    private static IEnumerator Check()
    {
        var frame = new GameObject("Preview Frame", typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(root.transform, false);
        frame.sizeDelta = new Vector2(1280, 920);
        var book = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Prefabs/Recall/Book/RecallBookExample"), frame);
        var example = book.GetComponent<RecallBookExample>();
        var turner = book.GetComponent<BookPageTurner>();
        var capture = book.GetComponent<BookPageSnapshot>();
        var sheet = book.GetComponentInChildren<BookPageCurlGraphic>(true);
        var pages = (RectTransform)book.transform.Find("Pages");
        var leftPage = (RectTransform)pages.Find("LeftPage");
        var rightPage = (RectTransform)pages.Find("RightPage");
        var leftSize = leftPage.sizeDelta;
        var rightSize = rightPage.sizeDelta;
        var leftPosition = leftPage.anchoredPosition;
        var rightPosition = rightPage.anchoredPosition;
        var binding = (RectTransform)book.transform.Find("PageTurnOverlay/Binding");
        var bindingPosition = binding.anchoredPosition;
        var bindingSize = binding.rect.size;
        var pageGroup = pages.GetComponent<CanvasGroup>();
        var pagePosition = pages.anchoredPosition;
        originalTimeScale = Time.timeScale;
        restoreTime = true;
        Time.timeScale = 0;
        yield return null;
        bool ready = false;
        try
        {
            Directory.CreateDirectory(Output);
            Export(capture, frame, "01-open.png");
            Assert.AreEqual(0, example.SpreadIndex);
            example.Next();
            Assert.IsTrue(turner.IsTurning, "Snapshot must succeed, without falling back to an instant change.");
            Assert.AreEqual(1, example.SpreadIndex);
            Assert.IsFalse(pageGroup.interactable);
            Assert.IsNotNull(sheet.GetComponent<CanvasRenderer>(), "A custom Graphic needs a CanvasRenderer.");
            Assert.AreEqual(book.transform, pages.parent);
            Assert.AreEqual(pagePosition, pages.anchoredPosition);
            Assert.AreEqual(leftSize, leftPage.sizeDelta);
            Assert.AreEqual(rightSize, rightPage.sizeDelta);
            Assert.AreEqual(leftPosition, leftPage.anchoredPosition);
            Assert.AreEqual(rightPosition, rightPage.anchoredPosition);
            AssertSheetMatches(sheet, rightPage, 0);
            AssertSheetMatches(sheet, leftPage, 1);
            Assert.AreEqual(bindingPosition, binding.anchoredPosition);
            Assert.AreEqual(bindingSize, binding.rect.size);
            example.Next();
            Assert.AreEqual(1, example.SpreadIndex, "Repeated input must be ignored during a turn.");
            sheet.SetProgress(0.35f);
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(sheet.canvasRenderer.GetMesh().vertexCount > 100, "The curved sheet must have renderable geometry.");
            Export(capture, frame, "02-forward-front.png");
            sheet.SetProgress(0.72f);
            Export(capture, frame, "03-forward-back.png");
            ready = true;
        }
        catch (Exception e) { Fail(e); }
        if (!ready) yield break;
        float deadline = Time.realtimeSinceStartup + 5;
        while (turner.IsTurning && Time.realtimeSinceStartup < deadline) yield return null;
        ready = false;
        try
        {
            Assert.IsFalse(turner.IsTurning, "Animation must finish while timeScale is zero.");
            Assert.IsTrue(pageGroup.interactable);
            Export(capture, frame, "04-next-spread.png");
            example.Previous();
            Assert.IsTrue(turner.IsTurning);
            Assert.AreEqual(0, example.SpreadIndex);
            AssertSheetMatches(sheet, leftPage, 0);
            AssertSheetMatches(sheet, rightPage, 1);
            sheet.SetProgress(0.72f);
            Export(capture, frame, "05-reverse-back.png");
            ready = true;
        }
        catch (Exception e) { Fail(e); }
        if (!ready) yield break;
        deadline = Time.realtimeSinceStartup + 5;
        while (turner.IsTurning && Time.realtimeSinceStartup < deadline) yield return null;
        try
        {
            Assert.IsFalse(turner.IsTurning);
            example.Previous();
            Assert.IsFalse(turner.IsTurning, "First spread must not turn backwards.");
            // Reproduce art/layout edits between turns on this disposable instance only.
            // Include unequal sizes, offsets, scale and pivot, while the overlay stays stale.
            leftPage.sizeDelta += new Vector2(42, 18);
            leftPage.anchoredPosition += new Vector2(-6, 9);
            leftPage.localScale = new Vector3(0.93f, 1.04f, 1);
            leftPage.pivot = new Vector2(0.6f, 0.45f);
            rightPage.sizeDelta += new Vector2(18, 44);
            rightPage.anchoredPosition += new Vector2(8, -5);
            rightPage.localScale = new Vector3(1.06f, 0.98f, 1);
            example.Next();
            Assert.IsTrue(turner.IsTurning);
            AssertSheetMatches(sheet, rightPage, 0);
            AssertSheetMatches(sheet, leftPage, 1);
            Assert.AreEqual(bindingPosition, binding.anchoredPosition);
            Assert.AreEqual(bindingSize, binding.rect.size);
            book.SetActive(false);
            book.SetActive(true);
            turner.ReduceMotion = true;
            example.Next();
            Assert.AreEqual(1, example.SpreadIndex);
            Assert.IsFalse(turner.IsTurning);
            example.Next();
            Assert.AreEqual(2, example.SpreadIndex);
            example.Next();
            Assert.AreEqual(2, example.SpreadIndex, "Last spread must not overrun.");
            turner.ReduceMotion = false;
            example.Previous();
            book.SetActive(false);
            Assert.IsFalse(turner.IsTurning);
            Assert.IsTrue(pageGroup.interactable);
            book.SetActive(true);
            Assert.AreEqual(0, example.SpreadIndex);
            example.Next();
            Assert.IsTrue(turner.IsTurning, "Reopening must reacquire released render textures.");
            book.SetActive(false);
            Result = "PASS: forward/back endpoint size and position, page resize/scale/pivot edits, unchanged binding and page layout, opaque snapshots, sheet mesh, bounds, repeated input, timeScale=0, reduced motion, close/reopen and hierarchy restoration. PNGs for visual review: " + Output;
            Debug.Log("[Recall Book] " + Result);
        }
        catch (Exception e) { Fail(e); }
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
        Assert.IsTrue(Vector3.Distance(actual.min, expected.min) < 0.02f &&
            Vector3.Distance(actual.max, expected.max) < 0.02f,
            $"Turn endpoint {progress} must match {page.name}. Actual {actual}, expected {expected}");
    }

    private static void Export(BookPageSnapshot capture, RectTransform root, string filename)
    {
        RenderTexture target = null;
        var previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            capture.Capture(root, ref target);
            RenderTexture.active = target;
            image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Output, filename), image.EncodeToPNG());
            Assert.IsTrue(image.GetPixel(target.width / 4, target.height / 2).a > 0.9f, "Snapshot must contain opaque paper.");
        }
        finally
        {
            RenderTexture.active = previous;
            if (image != null) UnityEngine.Object.Destroy(image);
            BookPageSnapshot.Release(ref target);
        }
    }

    private static void Fail(Exception exception)
    {
        Result = "FAIL: " + exception;
        Debug.LogException(exception);
    }

    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
        routine = null;
        if (restoreTime) Time.timeScale = originalTimeScale;
        restoreTime = false;
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        root = null;
    }
}
