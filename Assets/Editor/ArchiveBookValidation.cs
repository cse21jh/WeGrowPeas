using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeGrowPeas.RecallBook;
using WeGrowPeas.UI.Book;
using Object = UnityEngine.Object;

/// <summary>Exercises a disposable archive instance without writing profile data or saving scenes.</summary>
public static class ArchiveBookValidation
{
    private static GameObject root;
    private static IEnumerator routine;
    private static float originalTimeScale;
    private static int lastFrame;
    private static string originalLanguage;
    public static string Result { get; private set; } = "Not run";
    private static readonly List<string> checks = new List<string>();

    [MenuItem("Tools/Archive/Validate In Play Mode")]
    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Validation requires Play Mode.");
        if (routine != null) throw new InvalidOperationException("Validation is already running.");
        checks.Clear();
        originalTimeScale = Time.timeScale;
        originalLanguage = Loc.Language;
        Time.timeScale = 0;
        Result = "Running";
        routine = Check();
        lastFrame = -1;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode || routine == null) return;
        Result = "Cancelled: Play Mode ended";
        Cleanup();
    }

    private static void OnBeforeAssemblyReload()
    {
        if (routine == null) return;
        Result = "Cancelled: Scripts reloaded";
        Cleanup();
    }

    private static void Tick()
    {
        if (!Application.isPlaying) { Result = "Cancelled: Play Mode ended"; Cleanup(); return; }
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try { if (!routine.MoveNext()) Cleanup(); }
        catch (Exception error) { Result = "FAIL: " + error; Debug.LogException(error); Cleanup(); }
    }

    private static IEnumerator Check()
    {
        var sceneArchive = Object.FindFirstObjectByType<ArchiveBookController>(FindObjectsInactive.Include);
        var entrance = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(b => b.name == "HistoryBtn" && b.gameObject.scene == sceneArchive.gameObject.scene);
        Assert.AreEqual(nameof(UIClickEvent.OnClick_OpenArchive), entrance.onClick.GetPersistentMethodName(0));
        var customize = entrance.transform.parent.Find("CustomizeBtn").GetComponent<Button>();
        Assert.IsTrue(entrance.onClick.GetPersistentTarget(0) is UIClickEvent);
        Assert.AreEqual(customize.onClick.GetPersistentTarget(0), entrance.onClick.GetPersistentTarget(0));
        Assert.IsFalse(entrance.transform.parent.Find("CodexBtn").gameObject.activeSelf);
        Assert.IsFalse(entrance.transform.parent.Find("AchievementBtn").gameObject.activeSelf);
        Assert.IsTrue(entrance.GetComponentsInChildren<TMP_Text>(true).All(t => t.text == Loc.T("archive.title")));
        checks.Add("StartScene 3 routes the archive entrance through the same UI controller as Customize.");
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ArchiveBookBuilder.PrefabPath));
        root.name = "Archive Validation (Disposable)";
        var archive = root.GetComponent<ArchiveBookController>();
        var book = root.GetComponentInChildren<BookLayoutController>(true);
        var panel = book.transform.parent.GetComponent<RectTransform>();
        var canvas = root.GetComponent<Canvas>();
        canvas.sortingOrder = 30000;
        var motion = book.GetComponent<BookCloseMotion>();
        var header = panel.Find("ArchiveNavigation");
        var menu = header.Find("OtherTabs").GetComponent<CanvasGroup>();
        var menuRect = menu.GetComponent<RectTransform>();
        var icon = header.Find("CurrentTabIcon").GetComponent<Image>();
        var title = header.Find("CurrentTabTitle").GetComponent<TMP_Text>();
        var restingPosition = menuRect.anchoredPosition;
        var restingRotation = icon.rectTransform.localRotation;
        var popupGraphics = new Graphic[] {
            icon, title,
            header.Find("OtherTabs/Tab0/Icon").GetComponent<Image>(),
            header.Find("OtherTabs/Tab0/Label").GetComponent<TMP_Text>(),
            header.Find("OtherTabs/Tab1/Icon").GetComponent<Image>(),
            header.Find("OtherTabs/Tab1/Label").GetComponent<TMP_Text>() };
        var popupColors = popupGraphics.Select(g => g.color).ToArray();
        var popupPositions = popupGraphics.Select(g => g.rectTransform.anchoredPosition3D).ToArray();
        var popupScales = popupGraphics.Select(g => g.rectTransform.localScale).ToArray();
        Assert.IsTrue(book.TryValidate(out string problem), problem);
        Assert.AreEqual(5, book.LayoutCount, "Original three layouts plus two archive layouts.");
        archive.Open();
        float deadline = Time.realtimeSinceStartup + 5;
        while (motion.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(archive.IsOpen);
        Assert.AreEqual(ArchiveBookController.Tab.Recall, archive.CurrentTab);
        Assert.AreEqual(1, book.CurrentIndex);
        Assert.IsTrue(archive.CanSwitch);
        AssertRaycast(header.Find("OtherTabs/Tab0").GetComponent<Button>());
        checks.Add("Existing recall layout opens and is interactive at timeScale=0.");
        var feedbackButtons = new[] {
            header.Find("CurrentTabHit").GetComponent<Button>(),
            header.Find("OtherTabs/Tab0").GetComponent<Button>(),
            header.Find("OtherTabs/Tab1").GetComponent<Button>() };
        for (int row = 0; row < feedbackButtons.Length; row++)
        {
            var button = feedbackButtons[row];
            var hit = (RectTransform)button.transform;
            var hitPosition = hit.anchoredPosition3D;
            var hitSize = hit.rect.size;
            AssertRaycast(button);
            var pointer = new PointerEventData(EventSystem.current) {button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            deadline = Time.realtimeSinceStartup + .16f;
            while (Time.realtimeSinceStartup < deadline) yield return null;
            var artwork = popupGraphics[row * 2];
            float hoverSize = artwork.rectTransform.localScale.x;
            Assert.IsTrue(hoverSize < popupScales[row * 2].x * .99f);
            Assert.IsTrue(popupGraphics[row * 2 + 1].rectTransform.localScale.x < popupScales[row * 2 + 1].x * .99f);
            Assert.IsTrue(artwork.color.r < popupColors[row * 2].r * .95f);
            Assert.IsTrue(Vector3.Distance(hitPosition,hit.anchoredPosition3D) < .001f && Vector2.Distance(hitSize,hit.rect.size) < .001f);
            AssertRaycast(button);
            if (row < 2) Export(book,panel,row == 0 ? "18-current-tab-hover.png" : "19-menu-tab-hover.png");
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            deadline = Time.realtimeSinceStartup + .04f;
            while (Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(artwork.rectTransform.localScale.x < hoverSize * .95f);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            if (row == 0)
            {
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                float min = hoverSize, max = hoverSize;
                deadline = Time.realtimeSinceStartup + .3f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    min = Mathf.Min(min,artwork.rectTransform.localScale.x);
                    max = Mathf.Max(max,artwork.rectTransform.localScale.x);
                    yield return null;
                }
                Assert.IsTrue(min < hoverSize * .95f && max > hoverSize * 1.03f,"Click compresses then rebounds.");
                Assert.AreEqual(ArchiveBookController.Tab.Recall,archive.CurrentTab);
            }
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            deadline = Time.realtimeSinceStartup + .16f;
            while (Time.realtimeSinceStartup < deadline) yield return null;
            AssertPopupRestored(popupGraphics,popupColors,popupPositions,popupScales);
        }
        checks.Add("All three left tabs tint/shrink on hover, keep fixed raycast areas, press and rebound on click, and restore on exit at timeScale=0.");
        Export(book, panel, "01-recall.png");

        // Real button listener, not just the controller entry point.
        header.Find("OtherTabs/Tab0").GetComponent<Button>().onClick.Invoke();
        Assert.IsTrue(archive.IsSwitching);
        archive.SelectTab(2); // Must not interrupt the first accepted switch.
        yield return null;
        Assert.IsFalse(menu.interactable);
        bool sawBottomFirst = false, sawTopFirst = false, sawStagger = false, sawPop = false;
        bool sawTurn = false, sawHeaderExit = false, sawHeaderEntry = false, sawAllHidden = false;
        bool captured = false;
        deadline = Time.realtimeSinceStartup + 8;
        while (archive.IsSwitching && Time.realtimeSinceStartup < deadline)
        {
            Assert.IsTrue(Vector2.Distance(restingPosition, menuRect.anchoredPosition) < .01f, "Popup keeps the menu in place.");
            if (archive.CurrentTab == ArchiveBookController.Tab.Recall)
            {
                sawBottomFirst |= popupGraphics[4].color.a < .4f && popupGraphics[2].color.a > .6f && icon.color.a > .95f;
                sawStagger |= popupGraphics[4].color.a < popupGraphics[5].color.a - .1f;
                if (!sawHeaderExit && icon.color.a < .5f && title.color.a < .8f)
                {
                    sawHeaderExit = true;
                    Export(book,panel,"16-current-tab-popup-exit.png");
                }
            }
            else
            {
                sawTopFirst |= icon.color.a > .1f && popupGraphics[2].color.a < .01f && popupGraphics[4].color.a < .01f;
                if (!sawHeaderEntry && icon.color.a > .1f && title.color.a > .1f && popupGraphics[2].color.a < .01f)
                {
                    sawHeaderEntry = true;
                    Export(book,panel,"17-current-tab-popup-entry.png");
                }
            }
            sawAllHidden |= popupGraphics.All(g => g.color.a < .001f);
            sawPop |= popupGraphics.Any(g => g.rectTransform.localScale.x > 1.02f);
            sawTurn |= book.IsTurning;
            Assert.IsTrue(Quaternion.Angle(icon.rectTransform.localRotation, restingRotation) < .01f,
                "Current tab icon stays at its authored angle throughout the transition.");
            Assert.AreEqual(int.MaxValue,title.maxVisibleCharacters,"Title appears as a whole popup, without typewriter animation.");
            if (book.IsTurning && !captured)
            {
                Export(book, panel, "02-turn.png");
                captured = true;
            }
            yield return null;
        }
        Assert.IsFalse(archive.IsSwitching, "Tab switch finishes at timeScale=0.");
        Assert.IsTrue(sawBottomFirst && sawTopFirst && sawStagger && sawPop && sawTurn && sawHeaderExit && sawHeaderEntry && sawAllHidden,
            $"Animation phases: bottom-first={sawBottomFirst}, top-first={sawTopFirst}, icon/text stagger={sawStagger}, pop={sawPop}, turn={sawTurn}, current-tab exit={sawHeaderExit}, current-tab entry={sawHeaderEntry}, all hidden before replacement={sawAllHidden}");
        Assert.AreEqual(ArchiveBookController.Tab.Codex, archive.CurrentTab);
        Assert.AreEqual(3, book.CurrentIndex);
        Assert.AreApproximatelyEqual(1, menu.alpha);
        Assert.IsTrue(Vector2.Distance(restingPosition, menuRect.anchoredPosition) < 0.01f);
        Assert.IsTrue(Quaternion.Angle(restingRotation, icon.rectTransform.localRotation) < 0.01f);
        Assert.IsTrue(archive.CanSwitch);
        AssertPopupRestored(popupGraphics,popupColors,popupPositions,popupScales);
        checks.Add("All three tab icons and labels pop out bottom-to-top and pop in top-to-bottom. Current icon/title both disappear and reappear; no rotation or typewriter. Page curl and repeated-input guard remain functional.");

        var codex = book.Layouts[3].leftContent.GetComponent<ArchiveCatalogView>();
        Assert.AreEqual(CodexCatalog.Get(CodexProgress.Category.Item).Count, codex.EntryCount);
        Assert.IsTrue(codex.EntryCount > 0);
        codex.SelectEntry(Mathf.Min(1, codex.EntryCount - 1));
        Assert.AreEqual(Mathf.Min(1, codex.EntryCount - 1), codex.SelectedIndex);
        var list = book.Layouts[3].leftContent.Find("Entries").GetComponent<ScrollRect>();
        float beforeScroll = list.content.anchoredPosition.y;
        list.OnScroll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { scrollDelta = new Vector2(0, -4) });
        yield return null;
        Assert.IsTrue(list.content.anchoredPosition.y > beforeScroll, "The catalog accepts scroll input.");
        list.verticalNormalizedPosition = 1;
        AssertRaycast(book.Layouts[3].leftContent.Find("Category_plant").GetComponent<Button>());
        Export(book, panel, "03-codex.png");
        var bookmark = book.Layouts[3].leftContent.Find("Category_item").GetComponent<ArchiveBookmark>();
        var palette = new SerializedObject(bookmark);
        var faceColor = palette.FindProperty("faceColor").colorValue;
        var border = bookmark.transform.Find("SelectedShape/Outline").GetComponent<Image>();
        var originalBorderColor = border.color;
        palette.FindProperty("faceColor").colorValue = Color.cyan;
        palette.ApplyModifiedPropertiesWithoutUndo(); bookmark.ApplyPalette();
        Assert.AreEqual(Color.cyan, bookmark.transform.Find("SelectedShape/Face").GetComponent<Image>().color);
        Assert.AreEqual(originalBorderColor, border.color);
        palette.FindProperty("faceColor").colorValue = faceColor;
        palette.ApplyModifiedPropertiesWithoutUndo(); bookmark.ApplyPalette();
        ExportKnownCatalog(book, panel, codex);
        checks.Add("Separate white bookmark PNG regions tint independently through Inspector properties.");
        codex.SelectCategory(1);
        Assert.IsTrue(book.IsTurning);
        codex.SelectCategory(2); // Reject during the real turn.
        deadline = Time.realtimeSinceStartup + 5;
        while (book.IsTurning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(CodexProgress.Category.Plant, codex.Category);
        Assert.AreEqual(CodexCatalog.Get(CodexProgress.Category.Plant).Count, codex.EntryCount);
        Export(book, panel, "04-plants.png");
        checks.Add("Real catalog counts, entry selection, category page turn, and category input guard.");

        archive.SelectTab(2);
        deadline = Time.realtimeSinceStartup + 8;
        while (archive.IsSwitching && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(4, book.CurrentIndex);
        var achievements = book.Layouts[4].leftContent.GetComponent<ArchiveCatalogView>();
        var definitions = AchievementSystem.All.Where(a => a != null && !string.IsNullOrEmpty(a.id)).ToArray();
        Assert.AreEqual(definitions.Length, achievements.EntryCount);
        Assert.IsTrue(achievements.EntryCount > 0);
        var data = definitions[0];
        var progress = book.Layouts[4].rightContent.Find("ProgressTrack").GetComponent<ArchiveProgressBar>();
        bool known = !data.hidden || AchievementSystem.IsUnlocked(data.id);
        Assert.AreApproximatelyEqual(known ? Mathf.Clamp01(AchievementSystem.GetProgress(data) / (float)Mathf.Max(1, data.target)) : 0, progress.Value);
        Export(book, panel, "05-achievements.png");
        checks.Add("Achievement rows and progress match the actual read-only save data.");

        // Preview only this disposable view; achievement and player-save state never change.
        var fill = progress.transform.Find("Fill").GetComponent<Image>();
        var stripes = fill.GetComponentInChildren<RawImage>(true);
        var progressLabel = book.Layouts[4].rightContent.Find("ProgressValue").GetComponent<TMP_Text>();
        progress.SetProgress(0);
        Assert.IsFalse(fill.gameObject.activeSelf);
        progress.SetProgress(.5f); progressLabel.text = "5 / 10";
        yield return null;
        Export(book, panel, "08-progress-half.png");
        float initialPhase = stripes.uvRect.x;
        deadline = Time.realtimeSinceStartup + .2f;
        while (Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(Mathf.Abs(initialPhase - stripes.uvRect.x) > .01f, "Progress stripes must scroll at timeScale=0.");
        Export(book, panel, "09-progress-scrolled.png");
        progress.SetProgress(1); progressLabel.text = "10 / 10";
        yield return null;
        Export(book, panel, "10-progress-full.png");
        book.Turner.ReduceMotion = true;
        initialPhase = stripes.uvRect.x;
        deadline = Time.realtimeSinceStartup + .1f;
        while (Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreApproximatelyEqual(initialPhase, stripes.uvRect.x);
        book.Turner.ReduceMotion = false;
        achievements.Refresh();
        checks.Add("Rounded progress bar handles empty/partial/full states; stripes scroll while paused and respect Reduce Motion.");

        Loc.SetLanguage("en", false);
        yield return null;
        yield return null;
        Assert.AreEqual(Loc.T("archive.achievement"), title.text);
        Assert.AreEqual(definitions[0].LocalizedTitle, book.Layouts[4].rightContent.Find("DetailTitle").GetComponent<TMP_Text>().text);
        Export(book, panel, "06-english.png");
        Loc.SetLanguage(originalLanguage, false);
        yield return null;
        checks.Add("Language changes refresh the tab title and selected content without saving preferences.");

        archive.SelectTab(1);
        yield return null;
        archive.Close(); // Close during the menu lift.
        deadline = Time.realtimeSinceStartup + 5;
        while (archive.IsOpen && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(archive.IsOpen);
        Assert.IsFalse(archive.IsSwitching);
        Assert.IsFalse(book.IsTurning);
        archive.Open();
        deadline = Time.realtimeSinceStartup + 5;
        while (motion.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(ArchiveBookController.Tab.Achievement, archive.CurrentTab);
        Assert.AreEqual(4, book.CurrentIndex);
        Assert.IsTrue(Vector2.Distance(restingPosition, menuRect.anchoredPosition) < 0.01f);
        Assert.AreApproximatelyEqual(1, menu.alpha);
        Assert.IsTrue(archive.CanSwitch);
        AssertPopupRestored(popupGraphics,popupColors,popupPositions,popupScales);
        checks.Add("Closing during a transition cancels safely; reopening restores the committed tab and input.");

        book.Turner.ReduceMotion = true;
        archive.SelectTab(0);
        Assert.AreEqual(1, book.CurrentIndex);
        Assert.IsFalse(archive.IsSwitching);
        Assert.IsFalse(book.IsTurning);
        archive.SelectTab(-1); archive.SelectTab(3);
        Assert.AreEqual(1, book.CurrentIndex);
        book.Turner.ReduceMotion = false;
        checks.Add("Reduce Motion and invalid tab indices.");

        archive.SelectTab(1);
        deadline = Time.realtimeSinceStartup + 5;
        while (!book.IsTurning && Time.realtimeSinceStartup < deadline) yield return null;
        root.SetActive(false);
        Assert.IsFalse(archive.IsSwitching);
        Assert.IsFalse(book.IsTurning);
        root.SetActive(true);
        yield return null;
        Assert.IsFalse(archive.IsSwitching);
        AssertPopupRestored(popupGraphics,popupColors,popupPositions,popupScales);
        checks.Add("Disabling during the page turn restores animation and input state.");
        Result = "PASS: " + string.Join("\n", checks);
        Debug.Log("[Archive] " + Result);
    }

    private static void AssertPopupRestored(Graphic[] graphics,Color[] colors,Vector3[] positions,Vector3[] scales)
    {
        for (int i = 0; i < graphics.Length; i++)
        {
            Assert.IsTrue(Vector3.Distance(graphics[i].rectTransform.anchoredPosition3D,positions[i]) < .01f);
            Assert.IsTrue(Vector3.Distance(graphics[i].rectTransform.localScale,scales[i]) < .001f);
            Assert.IsTrue(Mathf.Abs(graphics[i].color.a - colors[i].a) < .001f);
        }
    }

    private static void Export(BookLayoutController book, RectTransform panel, string filename)
    {
        RenderTexture texture = null;
        var previous = RenderTexture.active;
        Texture2D image = null;
        var snapshot = book.GetComponent<BookPageSnapshot>();
        var settings = new SerializedObject(snapshot);
        int originalWidth = settings.FindProperty("textureWidth").intValue;
        try
        {
            // Full-panel comparison needs a full-screen resolution; production page captures keep their own setting.
            settings.FindProperty("textureWidth").intValue = filename == "02-turn.png" ? 1024 : 1920;
            settings.ApplyModifiedPropertiesWithoutUndo();
            snapshot.Capture(panel, ref texture);
            RenderTexture.active = texture;
            image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Temp/ArchiveValidation");
            File.WriteAllBytes("Temp/ArchiveValidation/" + filename, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            settings.FindProperty("textureWidth").intValue = originalWidth;
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (image != null) Object.Destroy(image);
            BookPageSnapshot.Release(ref texture);
        }
    }

    private static void ExportKnownCatalog(BookLayoutController book, RectTransform panel, ArchiveCatalogView view)
    {
        // Only the disposable view's newly allocated catalog entries change; no discovery/save API is called.
        var field = typeof(ArchiveCatalogView).GetField("entries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var entries = (List<CodexEntry>)field.GetValue(view);
        var rows = book.Layouts[3].leftContent.Find("Entries/Viewport/Content");
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i]; entry.discovered = true; entry.locked = false;
            int selected = i;
            rows.Find("Entry " + i).GetComponent<ArchiveEntryRow>().Bind(entry.displayName, entry.icon, null, () => view.SelectEntry(selected));
        }
        view.SelectEntry(0);
        Export(book, panel, "07-known-codex-preview.png");
        view.Refresh();
    }

    private static void AssertRaycast(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        var hits = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, hits);
        Assert.IsTrue(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
            "The visible button is the top raycast target: " + button.name);
    }

    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        routine = null;
        Time.timeScale = originalTimeScale;
        if (root != null) Object.DestroyImmediate(root);
        root = null;
        Directory.CreateDirectory("Temp/ArchiveValidation");
        File.WriteAllText("Temp/ArchiveValidation/report.txt", Result);
        if (originalLanguage != null && Loc.Language != originalLanguage) Loc.SetLanguage(originalLanguage, false);
    }
}
