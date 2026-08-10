using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ModOp { Multiply, Add, Override }

public enum StatId
{
    BugSpeedMul = 1,             // 전체 벌레 속도 곱
    WaveWeightMul = 2,           // 웨이브별 가중치 곱 (param = (int)WaveType)
    BugSpawnIntervalMul = 3,     // 스폰 간격에 곱 (0.5면 2배 빨리 스폰)
    BreedingPhaseDurationMul = 4,// 교배 단계 시간에 곱 (2면 2배 길어짐)
    BreedingAttemptsMul = 5,     // 교배 가능 횟수에 곱 (2면 2배)

}

[Serializable]
public struct ModKey
{
    public StatId stat;
    public int param; // 대상 스코프 (없으면 -1)
    public ModKey(StatId s, int p = -1) { stat = s; param = p; }
}

[Serializable]
public class Mod
{
    public int id;
    public ModKey key;
    public ModOp op;
    public float value;      // Multiply면 곱(예: 0.5), Add면 가산값, Override면 절대값
    public int expireDay;    // 이 날 '이전'까지 유효 (GameManager.stage 기준)
    public string sourceTag; // "BugSpray", "SignPost_Wind" 같이 추적용
    public int priority;     // Override 충돌 시 우선순위(높을수록 우선)
}

public class ModManager : Singleton<ModManager>
{
    private int nextId = 1;

    private int Day => GameManager.Instance.stage;


    // 저장 필요
    private readonly List<Mod> mods = new();

    public List<Mod> Mods => mods;

    // -------- 등록/해제/만료 --------
    public int AddTimedMultiplier(StatId stat, int param, float multiplier, int durationDays, string sourceTag = null)
        => Add(new Mod
        {
            id = nextId++,
            key = new ModKey(stat, param),
            op = ModOp.Multiply,
            value = Mathf.Max(0f, multiplier),
            expireDay = Day + durationDays + 1,
            sourceTag = sourceTag,
        });

    public int AddTimedAdditive(StatId stat, int param, float addValue, int durationDays, string sourceTag = null)
        => Add(new Mod
        {
            id = nextId++,
            key = new ModKey(stat, param),
            op = ModOp.Add,
            value = addValue,
            expireDay = Day + durationDays + 1,
            sourceTag = sourceTag,
        });

    public int Add(Mod m)
    {
        if (m.id == 0) m.id = nextId++;
        mods.Add(m);
        if (m.sourceTag.Contains("SignPost"))
            GameManager.Instance.enemyController.signPost.SetSignPost(m.key.param);
        return m.id;
    }

    public void Remove(int id) => mods.RemoveAll(x => x.id == id);

    public void OnNewDay(int day) // 하루 경과 시 호출
    {
        foreach(var m in mods.ToList())
        {
            if(m.expireDay <= day)
            {
                if (m.sourceTag.Contains("SignPost"))
                    GameManager.Instance.enemyController.signPost.HideSignPost();
                mods.Remove(m);
            }
        }
    }

    // -------- 조회(합성) --------
    public float GetMul(StatId stat, int param = -1, float minCap = 0f, float maxCap = float.PositiveInfinity)
    {
        float mul = 1f;
        int? bestOverride = null;
        float overrideValue = 1f;

        foreach (var m in mods)
        {
            if (m.key.stat != stat) continue;
            if (m.key.param != param) continue;
            if (m.expireDay <= Day) continue;

            if (m.op == ModOp.Override)
            {
                if (bestOverride == null || m.priority > bestOverride.Value)
                {
                    bestOverride = m.priority;
                    overrideValue = m.value;
                }
            }
            else if (m.op == ModOp.Multiply)
            {
                mul *= m.value;
            }
            else if (m.op == ModOp.Add)
            {
                // 곱 스탯에 Add를 허용하고 싶지 않으면 빼도 됨.
                mul *= Mathf.Max(0f, 1f + m.value);
            }
        }

        float v = bestOverride != null ? overrideValue : mul;
        if (!float.IsInfinity(maxCap)) v = Mathf.Min(v, maxCap);
        if (!float.IsNaN(minCap)) v = Mathf.Max(v, minCap);
        return v;
    }

    public float GetAdd(StatId stat, int param = -1)
    {
        float sum = 0f;
        int? bestOverride = null;
        float overrideValue = 0f;

        foreach (var m in mods)
        {
            if (m.key.stat != stat) continue;
            if (m.key.param != param) continue;
            if (m.expireDay <= Day) continue;

            if (m.op == ModOp.Override)
            {
                if (bestOverride == null || m.priority > bestOverride.Value)
                {
                    bestOverride = m.priority;
                    overrideValue = m.value;
                }
            }
            else if (m.op == ModOp.Add) sum += m.value;
            else if (m.op == ModOp.Multiply) sum += (m.value - 1f); // 필요 시 정책 조정
        }

        return bestOverride != null ? overrideValue : sum;
    }

    public void LoadModManager(SaveData saveData)
    {
        foreach (var m in saveData.mods)
        {
            Add(m);
        }
    }
}