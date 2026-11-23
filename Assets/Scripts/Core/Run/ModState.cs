using System;
using System.Collections.Generic;

/// <summary>기간제 보정(벌레 스프레이, 표지판, 아드레날린 등). 만료와 합성 규칙만 안다.</summary>
public sealed class ModState
{
    private int nextId = 1;

    public List<Mod> Mods { get; } = new List<Mod>();

    public int AddTimed(StatId stat, int param, ModOp op, float value, int today, int durationDays, string sourceTag = null)
        => Add(new Mod
        {
            key = new ModKey(stat, param),
            op = op,
            value = op == ModOp.Multiply ? Math.Max(0f, value) : value,
            expireDay = today + durationDays + 1,
            sourceTag = sourceTag,
        });

    public int Add(Mod mod)
    {
        if (mod.id == 0) mod.id = nextId;
        nextId = Math.Max(nextId, mod.id + 1); // 불러온 것과 번호가 겹치지 않게
        Mods.Add(mod);
        return mod.id;
    }

    public void Remove(int id) => Mods.RemoveAll(m => m.id == id);

    /// <summary>날이 바뀔 때 만료된 것을 빼고, 뺀 것을 돌려준다.</summary>
    public List<Mod> ExpireOn(int day)
    {
        List<Mod> expired = Mods.FindAll(m => m.expireDay <= day);
        Mods.RemoveAll(m => m.expireDay <= day);
        return expired;
    }

    public float GetMul(StatId stat, int param, int day, float minCap = 0f, float maxCap = float.PositiveInfinity)
    {
        float mul = 1f;
        Mod best = null;

        foreach (Mod m in Mods)
        {
            if (!IsActive(m, stat, param, day)) continue;
            if (m.op == ModOp.Override)
            {
                if (best == null || m.priority > best.priority) best = m;
            }
            else if (m.op == ModOp.Multiply) mul *= m.value;
            else if (m.op == ModOp.Add) mul *= Math.Max(0f, 1f + m.value);
        }

        float value = best != null ? best.value : mul;
        if (!float.IsInfinity(maxCap)) value = Math.Min(value, maxCap);
        if (!float.IsNaN(minCap)) value = Math.Max(value, minCap);
        return value;
    }

    public float GetAdd(StatId stat, int param, int day)
    {
        float sum = 0f;
        Mod best = null;

        foreach (Mod m in Mods)
        {
            if (!IsActive(m, stat, param, day)) continue;
            if (m.op == ModOp.Override)
            {
                if (best == null || m.priority > best.priority) best = m;
            }
            else if (m.op == ModOp.Add) sum += m.value;
            else if (m.op == ModOp.Multiply) sum += m.value - 1f;
        }
        return best != null ? best.value : sum;
    }

    private static bool IsActive(Mod m, StatId stat, int param, int day)
        => m.key.stat == stat && m.key.param == param && m.expireDay > day;
}
