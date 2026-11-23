using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// 정원 씬을 실제로 Play해서 시뮬레이터와 한 줄씩 대조한다. Tools > 시뮬레이터 대조 테스트
///
/// 같은 시드와 같은 조작(<see cref="ParityScript"/>)으로 양쪽을 돌리고 낮과 밤마다 남긴 상태를 비교한다.
/// 벌레는 Unity 물리를 쓰므로 대조하지 않는다. 특성은 시뮬레이터에 없어 쓰지 않는다.
/// 세이브는 Temp 폴더의 빈 프로필을 쓰므로 실제 세이브를 건드리지 않는다.
/// </summary>
[InitializeOnLoad]
public static class ParityRunner
{
    private const string GardenScene = "Assets/Scenes/Garden_GrassUpdate.unity";
    private const string StateKey = "Parity.State"; // 도메인을 다시 읽어도 남는다
    private const string Folder = "Temp/Parity";
    private const float TimeScale = 20f;
    private const float StuckSeconds = 60f;

    private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private struct Case { public string species; public int dawn, seed, days; public override string ToString() => $"{species} 새벽 {dawn} 시드 {seed} {days}일"; }

    private static List<Case> cases;
    private static int index;
    private static ParityScript script;
    private static GameParityActor actor;
    private static int lastDay, lastNight;
    private static double lastProgress;
    private static bool loading;
    private static string lastError = "";

    static ParityRunner()
    {
        string state = SessionState.GetString(StateKey, "");
        if (state == "") return;
        ProfileStore.EditorRootOverride = Path.GetFullPath(Folder + "/Save");
        bool start = state == "start";
        EditorApplication.CallbackFunction resume = null;
        resume = () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= resume;
            if (start) Begin();
            else Finish("중간에 스크립트를 다시 읽어 중단됐습니다.");
        };
        EditorApplication.update += resume;
    }

    [MenuItem("Tools/시뮬레이터 대조 테스트")]
    private static void Menu() => Start(DefaultCases());

    /// <summary>끝나면 결과를 콘솔과 Temp/Parity/report.txt에 남긴다.</summary>
    /// <param name="plan">"완두콩,0,시드,일수"를 ;로 이은 것. 시드 0은 아무 시드.</param>
    public static void Start(string plan)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/plan.txt", plan);
        File.WriteAllText(Folder + "/scene.txt", SceneManager.GetActiveScene().path);
        SessionState.SetString(StateKey, "start");
        // 세이브와 도감을 메모리에 들고 있는 코드가 많아서 앞뒤로 도메인을 다시 읽는다
        EditorUtility.RequestScriptReload();
    }

    private static string DefaultCases()
        => $"{PlantIds.Pea},0,0,30;{PlantIds.Peanut},0,0,30;{PlantIds.Pea},13,0,30;{PlantIds.Peanut},13,0,30";

    // ───── 게임 쪽 ─────

    private static void Begin()
    {
        SessionState.SetString(StateKey, "running");
        LoadPlan();
        index = -1;
        SavePlan();

        // 시뮬레이터의 '모든 아이템 해금'에 맞춘다 (빈 프로필에만 적용된다)
        foreach (ItemData item in Resources.LoadAll<ItemData>("Data/Item Data")) UnlockManager.Unlock(item.UnlockId);

        GameStartContext.SetStartType(GameStartType.NewGame);
        EditorSceneManager.OpenScene(GardenScene);
        EditorApplication.update += Update;
        Application.logMessageReceived += OnLog;
        SceneManager.sceneLoaded += OnSceneLoaded;
        lastProgress = EditorApplication.timeSinceStartup;
        EditorApplication.EnterPlaymode();
    }

    private static void LoadPlan()
    {
        cases = new List<Case>();
        foreach (string line in File.ReadAllText(Folder + "/plan.txt").Split(';'))
        {
            string[] v = line.Split(',');
            int seed = int.Parse(v[2]);
            cases.Add(new Case { species = v[0], dawn = int.Parse(v[1]), seed = seed != 0 ? seed : Guid.NewGuid().GetHashCode() & 0x7FFFFFFF, days = int.Parse(v[3]) });
        }
    }

    // 정해진 시드를 남겨 두면 같은 판을 다시 돌리거나 비교할 수 있다
    private static void SavePlan()
        => File.WriteAllText(Folder + "/plan.txt", string.Join(";", cases.ConvertAll(c => $"{c.species},{c.dawn},{c.seed},{c.days}")));

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Exception) return;
        lastError = message.Split('\n')[0];
        foreach (string line in stack.Split('\n'))
            if (line.Contains("Assets/Scripts")) { lastError += " < " + line.Trim(); break; }
    }

    private static void NextCase()
    {
        index++;
        lastError = "";
        if (index >= cases.Count) { EditorApplication.ExitPlaymode(); return; }

        Case c = cases[index];
        script = new ParityScript(c.seed);
        actor = null;
        lastDay = lastNight = 0;
        loading = true;
        GameStartContext.SetStartType(GameStartType.NewGame);
        DawnSystem.SetSelectedStage(c.dawn);
        GameRun.EditorNextSeed = c.seed;
        // 20배속에서 트윈이 몰리면 DOTween의 내부 목록이 깨질 수 있어, 넉넉히 잡고 판마다 비운다
        DOTween.Clear(true);
        DOTween.Init();
        DOTween.SetTweensCapacity(5000, 500);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 새 씬의 Awake 뒤, Start 앞
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!loading) return;
        loading = false;
        GameManager game = Object.FindAnyObjectByType<GameManager>();
        game.currentPlant = cases[index].species;
        actor = new GameParityActor(game);
        // 필수 메시지 팝업을 띄우지 않고 읽은 것으로 처리하게 한다
        typeof(MessengerApp).GetField("mandatoryMessagePopup", Hidden).SetValue(game.phoneManager.messengerApp, null);
        // 벌레가 나오지 않게 한다
        game.grid.Bugs.Load(new GridSave { bugSpawnTimeInterval = 1e9f });
    }

    private static void Update()
    {
        if (!EditorApplication.isPlaying)
        {
            if (index >= 0 && !EditorApplication.isPlayingOrWillChangePlaymode) Finish(null);
            return;
        }
        AudioListener.volume = 0f;
        if (EditorApplication.timeSinceStartup - lastProgress > StuckSeconds)
        {
            script?.Log.AppendLine($"멈춤: {lastDay}일차 낮, {lastNight}일차 밤까지 진행. {lastError}");
            SaveGameLog();
            lastProgress = EditorApplication.timeSinceStartup;
            NextCase();
            return;
        }
        if (index < 0)
        {
            if (Time.timeSinceLevelLoad > 1f) NextCase();
            return;
        }
        if (loading || actor == null || !actor.Ready) return;

        try { Drive(); }
        catch (Exception e)
        {
            script.Log.AppendLine("오류: " + e);
            SaveGameLog();
            NextCase();
        }
    }

    private static void Drive()
    {
        GameManager game = actor.Game;
        Time.timeScale = TimeScale;
        if (game.GetGameIsStopped()) return;

        if (game.grid.Breeder.GetIsBreeding() && lastDay != game.stage)
        {
            lastDay = game.stage;
            lastProgress = EditorApplication.timeSinceStartup;
            actor.BeginDay();
            script.Day(actor);
            if (lastDay > cases[index].days) { SaveGameLog(); NextCase(); return; }
            game.grid.Breeder.SkipBreed();
        }
        else if (game.phoneManager.IsPhonePhase && lastNight != game.stage)
        {
            lastNight = game.stage;
            lastProgress = EditorApplication.timeSinceStartup;
            actor.BeginNight();
            script.Night(actor);
            game.phoneManager.SkipPhoneTime();
        }
        else if (game.grid.CheckGameOver())
        {
            SaveGameLog();
            NextCase();
        }
    }

    private static void SaveGameLog()
    {
        if (script != null) File.WriteAllText($"{Folder}/{index}_game.txt", script.Log.ToString());
        script = null;
        actor = null;
    }

    // ───── 비교 ─────

    private static void Finish(string error)
    {
        EditorApplication.update -= Update;
        Application.logMessageReceived -= OnLog;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioListener.volume = 1f;

        string report = BuildReport(error, out bool failed);
        File.WriteAllText(Folder + "/report.txt", report);
        string title = failed ? "[대조 테스트] 다른 곳이 있습니다\n" : "[대조 테스트] 모두 일치\n";
        if (failed) Debug.LogError(title + report);
        else Debug.Log(title + report);

        string scene = File.ReadAllText(Folder + "/scene.txt");
        if (scene != "" && !EditorApplication.isPlayingOrWillChangePlaymode) EditorSceneManager.OpenScene(scene);
        SessionState.EraseString(StateKey);
        ProfileStore.EditorRootOverride = null;
        EditorUtility.RequestScriptReload(); // 빈 프로필의 값이 메모리에 남지 않게
    }

    /// <summary>게임을 다시 돌리지 않고 지난 게임 기록과 지금 시뮬레이터를 비교한다. 시뮬레이터만 고쳤을 때 쓴다.</summary>
    [MenuItem("Tools/시뮬레이터 대조 (지난 기록)")]
    public static void CompareLast()
    {
        if (!File.Exists(Folder + "/plan.txt")) { Debug.LogWarning("[대조 테스트] 지난 기록이 없습니다."); return; }
        LoadPlan();
        string report = BuildReport(null, out bool failed);
        File.WriteAllText(Folder + "/report.txt", report);
        if (failed) Debug.LogError("[대조 테스트] 다른 곳이 있습니다\n" + report);
        else Debug.Log("[대조 테스트] 모두 일치\n" + report);
    }

    private static string BuildReport(string error, out bool anyFailed)
    {
        var report = new StringBuilder();
        if (error != null) report.AppendLine(error);
        anyFailed = error != null;
        for (int i = 0; cases != null && i < cases.Count; i++)
        {
            string result = Compare(i);
            if (result != null) anyFailed = true;
            report.AppendLine($"{(result == null ? "일치" : "다름")}  {cases[i]}");
            if (result != null) report.AppendLine(result);
        }
        return report.ToString();
    }

    private static string Compare(int i)
    {
        Case c = cases[i];
        string gamePath = $"{Folder}/{i}_game.txt";
        if (!File.Exists(gamePath)) return "  게임 기록이 없습니다.";

        var config = new SimConfig { runs = 1, days = c.days + 1, species = c.species, dawnStage = c.dawn, bugs = false };
        string simText;
        try { simText = RunSimulator.RunScript(config, c.seed); }
        catch (Exception e) { return "  시뮬레이터 오류: " + e; }
        File.WriteAllText($"{Folder}/{i}_sim.txt", simText);

        string[] game = Lines(File.ReadAllText(gamePath)), sim = Lines(simText);
        for (int n = 0; n < game.Length; n++)
        {
            if (n < sim.Length && game[n] == sim[n]) continue;
            int head = n;
            while (head > 0 && !game[head].StartsWith("[")) head--;
            return $"  {n + 1}번째 줄, {game[head]}\n  게임: {game[n]}\n  시뮬: {(n < sim.Length ? sim[n] : "(끝)")}";
        }
        // 게임은 마지막 날 낮까지만 기록한다
        return game.Length < 100 ? "  기록이 너무 짧습니다." : null;
    }

    private static string[] Lines(string text) => text.Replace("\r", "").TrimEnd('\n').Split('\n');

    // ───── 게임을 조작하는 손 ─────

    private sealed class GameParityActor : IParityActor
    {
        public readonly GameManager Game;
        private readonly Grid grid;
        private ShopController shop;

        public GameParityActor(GameManager game)
        {
            Game = game;
            grid = game.grid;
        }

        public bool Ready => Game != null && Game.enemyController.CurrentWave != null;

        public void BeginDay() => shop = null;
        public void BeginNight() => shop = new ShopController();

        private static object Get(object target, string name) => FindField(target, name).GetValue(target);
        private static void Set(object target, string name, object value) => FindField(target, name).SetValue(target, value);

        private static FieldInfo FindField(object target, string name)
        {
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, Hidden | BindingFlags.Public);
                if (f != null) return f;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }

        private static object Call(object target, string name, params object[] args)
        {
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                MethodInfo m = t.GetMethod(name, Hidden | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (m != null) return m.Invoke(target, args);
            }
            throw new MissingMethodException(target.GetType().Name, name);
        }

        public FieldState Field => grid.Field;
        public ShopContext Shop => ShopContext.ForGame;
        public string Species => Game.currentPlant;
        public int Stage => Game.stage;
        public int Gold => Game.economyManager.GetGold();
        public int WaveSkips => Game.enemyController.WaveSkipCount;
        public int UsedBreeds => (int)Get(grid.Breeder, "breedCount");
        public int MaxBreeds => (int)Call(grid.Breeder, "GetEffectiveMaxBreedCount");
        public TaxState Tax => TaxManager.Instance.State;
        public RequestBoard Requests => RequestManager.Instance.Board;
        public ShopInventory Inventory => shop.GetInventory();

        public void AddGold(int amount) => Game.economyManager.AddGold(amount);
        public void Spawn(List<GeneticTrait> traits) => grid.Spawner.AddMovablePlant(traits);

        public void Breed(PlantState a, PlantState b)
        {
            Set(grid.Breeder, "breedObj1", grid.Board.Plants[a.GridIndex].gameObject);
            Set(grid.Breeder, "breedObj2", grid.Board.Plants[b.GridIndex].gameObject);
            Call(grid.Breeder, "ExecuteBreeding");
        }

        public void Move(PlantState plant, int cell)
        {
            Plant view = grid.Board.Plants[plant.GridIndex];
            Physics.SyncTransforms();
            Vector3 screen = Camera.main.WorldToScreenPoint(grid.GetSoilTransform(cell).position);
            if (!grid.TryPlacePlant(view, screen) || plant.GridIndex != cell)
                throw new InvalidOperationException($"식물을 {cell}번 칸으로 옮기지 못했습니다 ({plant.GridIndex}번 칸).");
            Call(view, "TryRootByDawn");
        }

        public void Sell(PlantState plant)
            => Object.FindAnyObjectByType<Shovel>(FindObjectsInactive.Include).Sell(grid.Board.Plants[plant.GridIndex]);

        public void SkipWave() => Game.enemyController.WaveSkip();
        public void PayTax() => TaxManager.Instance.TryPay();
        public void Claim(RequestInstance request) => request.GrantRewardOnce();

        public void Buy(ItemData item, int option, int cell)
        {
            if (option >= 0) item.SetSelectedOption(Shop, option);
            if (cell < 0) { shop.Buy(item); return; }

            // 칸을 고르는 화면 대신 바로 고르고, 나머지는 그대로
            item.SelectCell(cell);
            item.StartEffect(Shop, () => Call(shop, "TryChargeAndCommit", item, null), _ => { });
        }

        public void Reroll()
        {
            if (!shop.TryReroll()) throw new InvalidOperationException("리롤하지 못했습니다.");
        }

        public void AppendExtra(StringBuilder sb)
            => ParityScript.AppendShared(sb, Game.enemyController.CurrentWave.WaveType, Game.enemyController.NextWave.WaveType,
                Game.enemyController.GetNextSecondWave()?.WaveType, CurseManager.Instance.Board, Game.modManager.Mods, shop?.GetInventory());
    }
}
