using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using WeGrowPeas.PeaMerge;

/// <summary>Integration checks against the actual scene, physics and UI. Run in the PeaMerge scene in Play Mode.</summary>
public static class PeaMergeValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string TestKey = "WeGrowPeas.PeaMerge.ValidationOnly";
    private static IEnumerator routine;
    private static PeaMergeGame game;
    private static PeaMergeSettings originalSettings;
    private static PeaMergeSettings testSettings;
    private static int oldBest;
    private static int oldTestValue;
    private static bool hadTestValue;
    private static EventSystem eventSystem;
    private static bool inputWasEnabled;
    private static int lastFrame;
    private static double startedAt;
    public static string Result { get; private set; } = "Not run";

    [MenuItem("Tools/Pea Merge/Validate In Play Mode")]
    public static void Run()
    {
        if (routine != null) return;
        game = UnityEngine.Object.FindFirstObjectByType<PeaMergeGame>();
        if (!Application.isPlaying || game == null) throw new InvalidOperationException("Open PeaMerge and enter Play Mode first.");
        eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        inputWasEnabled = eventSystem != null && eventSystem.enabled;
        if (eventSystem != null) eventSystem.enabled = false;
        originalSettings = game.Settings;
        oldBest = game.BestScore;
        hadTestValue = PlayerPrefs.HasKey(TestKey);
        oldTestValue = PlayerPrefs.GetInt(TestKey, 0);
        testSettings = UnityEngine.Object.Instantiate(originalSettings);
        testSettings.bestScoreKey = TestKey;
        Field("settings").SetValue(game, testSettings);
        Result = "Running";
        startedAt = EditorApplication.timeSinceStartup;
        routine = Check();
        stack.Clear();
        stack.Push(routine);
        lastFrame = -1;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!Application.isPlaying || game == null) { Finish("Interrupted"); return; }
        if (EditorApplication.timeSinceStartup - startedAt > 75) { Finish("FAIL: timed out"); return; }
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            while (stack.Count > 0)
            {
                var current = stack.Peek();
                if (!current.MoveNext()) { stack.Pop(); continue; }
                if (current.Current is IEnumerator nested) { stack.Push(nested); continue; }
                return;
            }
            Finish("PASS: portrait alignment, input ownership, real physics merge, unique scoring, all nine tiers, final-tier clear, pause, overflow grace, game over, restart");
        }
        catch (Exception exception) { Debug.LogException(exception); Finish("FAIL: " + exception.Message); }
    }

    private static IEnumerator Check()
    {
        game.Restart();
        yield return null;
        Assert.AreEqual(0, game.Pieces.Count);
        Assert.AreEqual(0, game.Score);
        Assert.IsNotNull(game.Preview.GetComponent<PeaSquashController>());
        Assert.IsTrue(game.Preview.GetComponent<PeaSquashController>().enabled);

        var layout = UnityEngine.Object.FindFirstObjectByType<PeaMergePortraitLayout>();
        layout.Apply();
        Canvas.ForceUpdateCanvases();
        Vector2 uiPoint = RectTransformUtility.WorldToScreenPoint(null, layout.portraitRoot.TransformPoint(new Vector3(100, 0, 0)));
        Vector2 worldPoint = layout.gameCamera.WorldToScreenPoint(Vector3.right);
        Assert.IsTrue(Vector2.Distance(uiPoint, worldPoint) < 2f, "UI and physics camera must share the same portrait rectangle.");
        game.Aim(-100);
        Assert.IsTrue(game.Preview.transform.position.x - game.Preview.Radius >= -game.Settings.halfWidth);
        game.Aim(100);
        Assert.IsTrue(game.Preview.transform.position.x + game.Preview.Radius <= game.Settings.halfWidth);

        var input = UnityEngine.Object.FindFirstObjectByType<PeaMergeInput>();
        var first = new PointerEventData(EventSystem.current) { pointerId = 0, position = layout.gameCamera.WorldToScreenPoint(new Vector3(0, 1, 0)) };
        var second = new PointerEventData(EventSystem.current) { pointerId = 1, position = layout.gameCamera.WorldToScreenPoint(new Vector3(2, 1, 0)) };
        input.OnPointerDown(first);
        input.OnPointerDown(second);
        input.OnPointerUp(second);
        Assert.AreEqual(0, game.Pieces.Count, "A second touch cannot release the first touch's pea.");
        int tier = game.Preview.TierIndex;
        Field("nextTier").SetValue(game, tier);
        input.OnPointerUp(first);
        Assert.AreEqual(1, game.Pieces.Count);
        Assert.IsFalse(game.Drop(), "Rapid duplicate drops must be rejected.");
        yield return Wait(1.7f);
        Assert.IsNotNull(game.Preview);
        Assert.AreEqual(tier, game.Preview.TierIndex);
        game.Aim(0f);
        Assert.IsTrue(game.Drop());
        yield return Wait(3f);
        Assert.AreEqual(1, game.Pieces.Count, "Equal falling peas must physically collide and merge.");
        Assert.AreEqual(tier + 1, game.Pieces[0].TierIndex);
        Assert.AreEqual(game.Settings.tiers[tier].mergeScore, game.Score);
        Assert.AreEqual(1, game.MergeCount);

        var resting = game.Pieces[0];
        var position = resting.Body.position;
        float clock = game.GameClock;
        game.TogglePause();
        Assert.AreEqual(PeaMergeGame.GameState.Paused, game.State);
        Assert.IsFalse(game.Drop());
        Assert.IsFalse(resting.Body.simulated);
        double until = EditorApplication.timeSinceStartup + 0.3;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Assert.AreEqual(clock, game.GameClock);
        Assert.AreEqual(position, resting.Body.position);
        game.TogglePause();

        game.Restart();
        var a = Spawn(0, new Vector2(-2, -5));
        var b = Spawn(0, new Vector2(0, -5));
        var c = Spawn(0, new Vector2(2, -5));
        yield return Wait(0.2f);
        Assert.IsTrue(game.TryMerge(a, b));
        Assert.IsFalse(game.TryMerge(a, c), "A reserved pea must not participate in two pairs.");
        Assert.IsFalse(game.TryMerge(b, a), "Opposite collision callbacks must not score twice.");
        yield return Wait(0.1f);
        Assert.AreEqual(2, game.Pieces.Count);
        Assert.AreEqual(game.Settings.tiers[0].mergeScore, game.Score);

        // Check every authored appearance/size and the terminal pair without waiting for hundreds of drops.
        for (int i = 1; i < game.Settings.tiers.Length; i++)
        {
            game.Restart();
            a = Spawn(i, new Vector2(-2, 0));
            b = Spawn(i, new Vector2(2, 0));
            a.Body.gravityScale = b.Body.gravityScale = 0f;
            yield return Wait(0.2f);
            Assert.AreEqual(game.Settings.tiers[i].appearanceIndex, a.GetComponent<StartPeaAppearanceController>().AppearanceIndex);
            Assert.IsTrue(game.TryMerge(a, b));
            yield return Wait(0.1f);
            Assert.AreEqual(game.Settings.tiers[i].mergeScore, game.Score);
            Assert.AreEqual(i == game.Settings.tiers.Length - 1 ? 0 : 1, game.Pieces.Count);
        }

        game.Restart();
        a = Spawn(0, new Vector2(0, game.Settings.dangerY + 0.1f));
        a.Body.gravityScale = 0;
        yield return Wait(game.Settings.spawnGraceSeconds + 0.3f);
        Assert.AreEqual(PeaMergeGame.GameState.Playing, game.State, "New peas above the line must get a grace period.");
        a.Body.position = new Vector2(0, game.Settings.dangerY - 1f);
        yield return Wait(0.1f);
        Assert.AreEqual(0f, a.OverflowTime);
        a.Body.position = new Vector2(0, game.Settings.dangerY + 0.1f);
        float end = game.GameClock + game.Settings.overflowSeconds + 0.3f;
        while (game.GameClock < end && game.State == PeaMergeGame.GameState.Playing) yield return null;
        Assert.AreEqual(PeaMergeGame.GameState.GameOver, game.State);
        Assert.IsFalse(game.Drop());
        Assert.IsFalse(a.Body.simulated);
        game.Restart();
        Assert.AreEqual(PeaMergeGame.GameState.Playing, game.State);
        Assert.AreEqual(0, game.Score);
        Assert.AreEqual(0, game.Pieces.Count);
        Assert.IsTrue(game.CanDrop);
    }

    private static IEnumerator Wait(float seconds)
    {
        float end = game.GameClock + seconds;
        while (game.GameClock < end) yield return null;
    }

    // Flatten nested enumerators; the editor update loop does not run Unity coroutines itself.
    private static readonly Stack<IEnumerator> stack = new();

    private static FieldInfo Field(string name) => typeof(PeaMergeGame).GetField(name, Private);

    private static PeaMergePiece Spawn(int tier, Vector2 position)
    {
        var pea = (PeaMergePiece)typeof(PeaMergeGame).GetMethod("SpawnPiece", Private).Invoke(game, new object[] { tier, position });
        pea.Drop(true);
        ((List<PeaMergePiece>)Field("pieces").GetValue(game)).Add(pea);
        return pea;
    }

    private static void Finish(string result)
    {
        EditorApplication.update -= Tick;
        routine = null;
        stack.Clear();
        Result = result;
        if (game != null && originalSettings != null)
        {
            game.Restart();
            Field("settings").SetValue(game, originalSettings);
            Field("bestScore").SetValue(game, oldBest);
            game.Restart();
        }
        if (hadTestValue) PlayerPrefs.SetInt(TestKey, oldTestValue);
        else PlayerPrefs.DeleteKey(TestKey);
        PlayerPrefs.Save();
        if (testSettings != null) UnityEngine.Object.DestroyImmediate(testSettings);
        if (eventSystem != null) eventSystem.enabled = inputWasEnabled;
        Directory.CreateDirectory("Temp/PeaMergeValidation");
        File.WriteAllText("Temp/PeaMergeValidation/result.txt", Result);
        Debug.Log("[PeaMergeValidation] " + Result);
    }
}
