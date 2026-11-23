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
    public const string SignPostTagPrefix = "SignPost_";

    public string sourceTag; // "BugSpray", "SignPost_Wind" 같이 추적용

    public bool IsSignPost => sourceTag != null && sourceTag.StartsWith(SignPostTagPrefix);
    public int priority;     // Override 충돌 시 우선순위(높을수록 우선)
}

public class ModManager : Singleton<ModManager>
{
    /// <summary>보정 목록과 합성 규칙. 이 컴포넌트는 오늘 일차와 표지판 표시를 더한다.</summary>
    public ModState State { get; } = new ModState();

    private int Day => GameManager.Instance.stage;


    public List<Mod> Mods => State.Mods;

    // -------- 등록/해제/만료 --------
    public int AddTimedMultiplier(StatId stat, int param, float multiplier, int durationDays, string sourceTag = null)
        => Show(State.AddTimed(stat, param, ModOp.Multiply, multiplier, Day, durationDays, sourceTag));

    public int AddTimedAdditive(StatId stat, int param, float addValue, int durationDays, string sourceTag = null)
        => Show(State.AddTimed(stat, param, ModOp.Add, addValue, Day, durationDays, sourceTag));

    public int Add(Mod m) => Show(State.Add(m));

    // 방금 넣은 것이 표지판이면 세운다
    private int Show(int id)
    {
        Mod m = Mods[Mods.Count - 1];
        if (m.IsSignPost)
            GameManager.Instance.enemyController.signPost.SetSignPost(m.key.param);
        return id;
    }

    public void Remove(int id) => State.Remove(id);

    public void OnNewDay(int day)
    {
        foreach (Mod m in State.ExpireOn(day))
        {
            if (m.IsSignPost)
                GameManager.Instance.enemyController.signPost.HideSignPost();
        }
    }

    // -------- 조회(합성) --------
    public float GetMul(StatId stat, int param = -1, float minCap = 0f, float maxCap = float.PositiveInfinity)
        => State.GetMul(stat, param, Day, minCap, maxCap);

    public float GetAdd(StatId stat, int param = -1) => State.GetAdd(stat, param, Day);

    /// <summary>보유 모드를 저장 데이터에 담는다. <see cref="LoadModManager"/>와 짝.</summary>
    public void SaveModManager(ModSave save)
    {
        save.mods = Mods;
    }

    public void LoadModManager(ModSave saveData)
    {
        foreach (var m in saveData.mods)
        {
            Add(m);
        }
    }
}