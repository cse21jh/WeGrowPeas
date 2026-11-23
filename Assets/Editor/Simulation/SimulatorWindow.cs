using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>판을 여러 번 돌려 결과를 보는 창. Tools > 판 시뮬레이터</summary>
public class SimulatorWindow : EditorWindow
{
    [SerializeField] private SimConfig config = new SimConfig();
    private string report = "";
    private List<SimRunResult> results;
    private Vector2 scroll;

    [MenuItem("Tools/판 시뮬레이터")]
    private static void Open() => GetWindow<SimulatorWindow>("판 시뮬레이터");

    private void OnGUI()
    {
        config.runs = Mathf.Max(1, EditorGUILayout.IntField("판 수", config.runs));
        config.firstSeed = EditorGUILayout.IntField("첫 시드", config.firstSeed);
        config.days = Mathf.Clamp(EditorGUILayout.IntField("목표 일차", config.days), 1, 200);
        int plant = EditorGUILayout.Popup("식물", config.species == PlantIds.Peanut ? 1 : 0, new[] { PlantIds.Pea, PlantIds.Peanut });
        config.species = plant == 1 ? PlantIds.Peanut : PlantIds.Pea;
        config.bot = (SimBotKind)EditorGUILayout.EnumPopup("봇", config.bot);
        config.dawnStage = Mathf.Clamp(EditorGUILayout.IntField("새벽 단계", config.dawnStage), 0, DawnSystem.StageCount);
        config.startGold = EditorGUILayout.IntField("시작 골드", config.startGold);
        config.breedAttempts = EditorGUILayout.IntField("교배 횟수", config.breedAttempts);
        config.buyItems = EditorGUILayout.Toggle("아이템 구매", config.buyItems);
        config.allUnlocked = EditorGUILayout.Toggle("모든 아이템 해금", config.allUnlocked);
        config.rotationCount = Mathf.Max(0, EditorGUILayout.IntField("로테이션 칸 수", config.rotationCount));
        config.buySeedlings = EditorGUILayout.Toggle("모종 구매", config.buySeedlings);
        config.payTax = EditorGUILayout.Toggle("세금", config.payTax);

        EditorGUILayout.HelpBox("저주, 벌레, 의뢰, 특성, 특수 아이템은 아직 없습니다. 벌레 관련 아이템은 사지 않습니다. "
            + "절대 수치보다 값을 바꾸기 전후의 비교로 읽어 주세요.", MessageType.Info);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            if (GUILayout.Button("돌리기", GUILayout.Height(28))) Run();
        using (new EditorGUI.DisabledScope(results == null))
            if (GUILayout.Button("CSV로 저장")) SaveCsv();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Run()
    {
        var watch = Stopwatch.StartNew();
        try
        {
            results = RunSimulator.RunMany(config, i =>
                !EditorUtility.DisplayCancelableProgressBar("판 시뮬레이터", $"{i}/{config.runs}", (float)i / config.runs));
        }
        finally { EditorUtility.ClearProgressBar(); }
        report = BuildReport(config, results) + $"\n{watch.Elapsed.TotalSeconds:F1}초";
    }

    public static string BuildReport(SimConfig config, List<SimRunResult> results)
    {
        int n = results.Count, cleared = 0, taxDeaths = 0;
        var deaths = new int[config.days + 1];
        var hits = new long[Wave.NumberOfWave];
        var faced = new long[Wave.NumberOfWave];
        var wipes = new int[Wave.NumberOfWave];
        foreach (var r in results)
        {
            if (r.Cleared) cleared++;
            else deaths[r.DeathDay]++;
            if (r.DiedToTax) taxDeaths++;
            else if (!r.Cleared) wipes[(int)r.DeathWave]++;
            for (int i = 0; i < hits.Length; i++) { hits[i] += r.WaveHits[i]; faced[i] += r.WaveFaced[i]; }
        }

        (double low, double high) = Wilson(cleared, n);
        var sb = new StringBuilder();
        sb.AppendLine($"{n}판, {config.species}, 봇 {config.bot}, 새벽 {config.dawnStage}, 시드 {config.firstSeed}~");
        sb.AppendLine($"{config.days}일 클리어 {100.0 * cleared / n:F1}%  (95% 구간 {low * 100:F1}~{high * 100:F1}%)");
        sb.AppendLine($"세금 때문에 전멸 {taxDeaths}판");
        sb.AppendLine();
        sb.AppendLine("일차 | 도달 전 전멸(누적) | 그날 전멸 | 남은 식물 평균 | 골드 평균");
        int dead = 0;
        for (int day = 1; day <= config.days; day++)
        {
            dead += deaths[day];
            if (day % 5 != 0 && day != 1 && day != config.days) continue;
            double plants = 0, gold = 0; int alive = 0;
            foreach (var r in results)
                if (r.Plants.Count >= day) { alive++; plants += r.Plants[day - 1]; gold += r.GoldByDay[day - 1]; }
            sb.AppendLine(alive == 0 ? $"{day,4} | {100.0 * dead / n,6:F1}% | {deaths[day],5} | - | -"
                : $"{day,4} | {100.0 * dead / n,6:F1}% | {deaths[day],5} | {plants / alive,5:F1} | {gold / alive,8:F0}");
        }
        sb.AppendLine();
        sb.AppendLine("웨이브 | 한 번에 못 버틴 식물 평균 | 전멸시킨 판");
        for (int i = 0; i < hits.Length; i++)
            if (faced[i] > 0) sb.AppendLine($"{(WaveType)i} | {(double)hits[i] / faced[i]:F2} | {wipes[i]}");
        AppendItems(sb, results);
        return sb.ToString();
    }

    // 늦게 산 아이템은 오래 산 판에만 있어 좋아 보인다(생존 편향). 그래서 처음 산 날 이후 며칠을 더 버텼는지와,
    // 같은 날까지 살아 있던 다른 판이 버틴 날을 견준다.
    private static void AppendItems(StringBuilder sb, List<SimRunResult> results)
    {
        var buyers = new Dictionary<string, List<SimRunResult>>();
        foreach (var r in results)
            foreach (string name in r.FirstBought.Keys)
            {
                if (!buyers.TryGetValue(name, out var list)) buyers[name] = list = new List<SimRunResult>();
                list.Add(r);
            }
        if (buyers.Count == 0) return;

        var survivedFrom = new Dictionary<int, double>();
        double AverageAfter(int day)
        {
            if (survivedFrom.TryGetValue(day, out double cached)) return cached;
            double sum = 0; int n = 0;
            foreach (var r in results)
                if (r.Plants.Count >= day) { sum += r.Plants.Count - day; n++; }
            return survivedFrom[day] = n == 0 ? 0 : sum / n;
        }

        var rows = new List<(string name, int count, double diff)>();
        foreach (var kv in buyers)
        {
            double diff = 0; int n = 0;
            foreach (var r in kv.Value)
            {
                int day = r.FirstBought[kv.Key];
                if (r.Plants.Count < day) continue;
                diff += r.Plants.Count - day - AverageAfter(day);
                n++;
            }
            if (n > 0) rows.Add((kv.Key, n, diff / n));
        }
        rows.Sort((a, b) => b.diff.CompareTo(a.diff));
        sb.AppendLine().AppendLine("아이템 | 산 판 | 산 뒤 더 버틴 날(같은 날 생존 판 평균 대비)");
        foreach (var row in rows) sb.AppendLine($"{row.name} | {row.count} | {row.diff:+0.0;-0.0;0.0}");
    }

    /// <summary>비율의 95% 신뢰 구간.</summary>
    private static (double, double) Wilson(int success, int n)
    {
        if (n == 0) return (0, 1);
        const double z = 1.96;
        double p = (double)success / n, d = 1 + z * z / n;
        double c = (p + z * z / (2 * n)) / d, h = z * Math.Sqrt(p * (1 - p) / n + z * z / (4.0 * n * n)) / d;
        return (Math.Max(0, c - h), Math.Min(1, c + h));
    }

    private void SaveCsv()
    {
        string path = EditorUtility.SaveFilePanel("결과 저장", "", "sim.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        var sb = new StringBuilder("seed,cleared,deathDay,diedToTax,deathWave,gold\n");
        foreach (var r in results)
            sb.AppendLine($"{r.Seed},{(r.Cleared ? 1 : 0)},{r.DeathDay},{(r.DiedToTax ? 1 : 0)},{r.DeathWave},{r.Gold}");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }
}
