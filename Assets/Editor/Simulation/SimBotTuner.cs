using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

/// <summary>계획 봇의 조절값을 시뮬레이션으로 찾는다. 밸런스를 크게 바꾸면 다시 돌린다.</summary>
public static class SimBotTuner
{
    private static readonly FieldInfo[] Fields = Array.FindAll(typeof(SimBotParams).GetFields(), f => f.FieldType == typeof(float));

    /// <summary>버틴 날의 평균(클리어하면 보너스). 클수록 좋다.</summary>
    public static float Evaluate(SimConfig config)
    {
        float sum = 0f;
        foreach (SimRunResult r in RunSimulator.RunMany(config)) sum += r.Cleared ? config.days + 10 : r.DeathDay;
        return sum / config.runs;
    }

    public static SimBotParams Copy(SimBotParams source)
    {
        var copy = new SimBotParams();
        foreach (FieldInfo f in Fields) f.SetValue(copy, f.GetValue(source));
        return copy;
    }

    /// <summary>
    /// 값을 몇 개씩 흔들어 보고 나아지면 받아들인다. 같은 시드끼리 견주고, 받아들일 때마다 시드를 바꿔 특정 시드에 맞춰지지 않게 한다.
    /// </summary>
    /// <param name="configs">함께 맞출 설정들. 점수는 평균이다.</param>
    public static SimBotParams Tune(SimConfig[] configs, SimBotParams start, double seconds, int seed, StringBuilder log)
    {
        var rng = new Random(seed);
        SimBotParams best = Copy(start);
        int firstSeed = 1 + rng.Next(1000000);
        float bestScore = Score(configs, best, firstSeed);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        int tries = 0, accepted = 0;
        while (watch.Elapsed.TotalSeconds < seconds)
        {
            SimBotParams candidate = Copy(best);
            int changes = 1 + rng.Next(4);
            for (int i = 0; i < changes; i++)
            {
                FieldInfo f = Fields[rng.Next(Fields.Length)];
                float value = (float)f.GetValue(candidate);
                f.SetValue(candidate, (float)Math.Max(0.0, value * Math.Exp((rng.NextDouble() - 0.5) * 1.6)));
            }
            tries++;
            float score = Score(configs, candidate, firstSeed);
            if (score <= bestScore) continue;

            // 다른 시드에서도 나은지 한 번 더 본다
            int other = 1 + rng.Next(1000000);
            float again = Score(configs, candidate, other), before = Score(configs, best, other);
            if (again + score <= before + bestScore + 0.1f) continue;
            best = candidate;
            bestScore = again;
            firstSeed = other;
            accepted++;
        }
        log?.AppendLine($"{tries}번 시도, {accepted}번 채택, 점수 {bestScore:F2}");
        return best;
    }

    private static float Score(SimConfig[] configs, SimBotParams p, int firstSeed)
    {
        float sum = 0f;
        foreach (SimConfig config in configs)
        {
            config.botParams = p;
            config.firstSeed = firstSeed;
            sum += Evaluate(config);
        }
        return sum / configs.Length;
    }

    public static string Describe(SimBotParams p)
    {
        var sb = new StringBuilder();
        foreach (FieldInfo f in Fields) sb.Append($"{f.Name}={((float)f.GetValue(p)).ToString(System.Globalization.CultureInfo.InvariantCulture)};");
        return sb.ToString();
    }

    public static SimBotParams Parse(string text)
    {
        var p = new SimBotParams();
        foreach (string part in text.Split(';'))
        {
            string[] kv = part.Trim().Split('=');
            if (kv.Length != 2) continue;
            FieldInfo f = Array.Find(Fields, x => x.Name == kv[0]);
            if (f != null) f.SetValue(p, float.Parse(kv[1], System.Globalization.CultureInfo.InvariantCulture));
        }
        return p;
    }
}
