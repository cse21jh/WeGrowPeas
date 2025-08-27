using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ModOp { Multiply, Add, Override }

public enum StatId
{
    BugSpeedMul = 1,             // 전체 벌레 속도 곱
    WaveWeightMul = 2,           // 웨이브별 가중치 곱 (param = (int)WaveType)
    PriceMul = 3,                // 가격 곱 (param = 카테고리 등 필요시)
    PlantResistAdd = 4,          // 식물 저항 가산 (param = (int)WaveType) 등
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
    private readonly List<Mod> mods = new();
    private int nextId = 1;

    private int Day => GameManager.Instance.stage;

    // -------- 등록/해제/만료 --------
    public int AddTimedMultiplier(StatId stat, int param, float multiplier, int durationDays, string sourceTag = null)
        => Add(new Mod
        {
            id = nextId++,
            key = new ModKey(stat, param),
            op = ModOp.Multiply,
            value = Mathf.Max(0f, multiplier),
            expireDay = Day + durationDays,
            sourceTag = sourceTag
        });

    public int AddTimedAdditive(StatId stat, int param, float addValue, int durationDays, string sourceTag = null)
        => Add(new Mod
        {
            id = nextId++,
            key = new ModKey(stat, param),
            op = ModOp.Add,
            value = addValue,
            expireDay = Day + durationDays,
            sourceTag = sourceTag
        });

    public int Add(Mod m)
    {
        if (m.id == 0) m.id = nextId++;
        mods.Add(m);
        return m.id;
    }

    public void Remove(int id) => mods.RemoveAll(x => x.id == id);

    public void OnNewDay(int day) // 하루 경과 시 호출
    {
        mods.RemoveAll(m => m.expireDay <= day);
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
}