using System;
using UnityEngine;

/// <summary>벌레 접촉과 보상이 일으키는 일.</summary>
public interface IBug
{
    GridModifiers Modifiers { get; }
    void AddGold(int amount);
    void AddPestResistance(float value);
    /// <summary>먹어서 죽였으면 true. 페트병이나 무당벌레가 막았으면 false.</summary>
    bool EatPlant(PlantState plant);
    void OnPlantEaten();
    void StartEating();
    void Die();
    void OnKilled();
}

/// <summary>벌레의 접촉과 보상 규칙.</summary>
public static class BugRules
{
    public const int BaseGold = 100;
    public const int NepenthesBaseGold = 100;
    /// <summary>벌레 한 마리를 잡을 때마다 오르는 해충 저항력.</summary>
    public const float PestResistancePerKill = 0.002f;
    /// <summary>식물에 닿은 벌레가 멈춰 있는 시간(초).</summary>
    public const float EatingSeconds = 1f;
    /// <summary>나타나고 움직이기 시작할 때까지의 시간(초).</summary>
    public const float WarningSeconds = 1f;
    /// <summary>갑옷 벌레가 한 번 맞고 다시 움직일 때까지의 시간(초).</summary>
    public const float ReviveSeconds = 1f;

    /// <summary>벌레가 식물에 닿았을 때.</summary>
    public static void Touch(IBug bug, PlantState plant)
    {
        if (plant.Kind == PlantKind.Nepenthes)
        {
            bug.AddGold(GetNepenthesGold(bug.Modifiers));
            bug.Die();
            return;
        }

        if (bug.EatPlant(plant)) bug.OnPlantEaten();
        bug.StartEating();
    }

    /// <summary>벌레가 죽었을 때.</summary>
    public static void Reward(IBug bug, int baseGold)
    {
        bug.AddPestResistance(PestResistancePerKill);
        bug.OnKilled();
        bug.AddGold(GetKillGold(baseGold, bug.Modifiers));
    }

    public static int GetKillGold(int baseGold, GridModifiers modifiers)
        => Math.Max(0, baseGold + modifiers.AdditionalBugGold - DawnSystem.Current.bugPriceReduction); // 새벽: 벌레 기본 가격 감소

    public static int GetNepenthesGold(GridModifiers modifiers) => NepenthesBaseGold + modifiers.AdditionalNepenthesGold;

    public static float GetSpeed(float baseSpeed, GridModifiers modifiers, float speedMultiplier)
        => baseSpeed * (1f - modifiers.BugSpeedDecreasement) * speedMultiplier;
}

/// <summary>벌레 등장 시각과 종류. 게임은 <see cref="BugSpawner"/>가 프리팹을 만들고, 시뮬레이터는 상태만 만든다.</summary>
public sealed class BugSpawnState
{
    public const int Ladybug = -1;

    /// <summary>기본 소환 주기(초). 저장된다.</summary>
    public float Interval { get; set; } = 15f;
    /// <summary>마지막 소환 뒤 지난 시간. 저장된다.</summary>
    public float Elapsed { get; set; }

    private float todayInterval;

    /// <summary>하루를 시작하며 오늘의 소환 주기를 정한다.</summary>
    public void BeginDay(int stage, float intervalMultiplier)
    {
        // 새벽: 벌레 등장 딜레이 감소. 저주(벌레 대발생)도 추가 감소. 최소 0.5초 보장.
        float interval = Math.Max(0.5f, Interval - DawnSystem.Current.bugDelayReduction - CurseState.BugFestivalDelayReduce);
        todayInterval = interval * intervalMultiplier;
        if (stage == BugSchedule.AppearStage) Elapsed = 0f;
    }

    /// <summary>시간을 흘리고 지금 소환할 수를 준다.</summary>
    public int Tick(float deltaTime, GridModifiers modifiers, bool hasPlants)
    {
        Elapsed += deltaTime;
        if (Elapsed <= todayInterval * (1f + modifiers.BugSpawnIntervalIncreasement) || !hasPlants) return 0;
        Elapsed = 0f;
        return CurseState.BugFestival ? 2 : 1; // 저주: 벌레 대발생
    }

    /// <summary>소환할 종류의 번호. 무당벌레면 <see cref="Ladybug"/>.</summary>
    public static int RollKind(GridModifiers modifiers, int ladybugCount, int stage, int kindCount)
    {
        bool ladybug = GameRun.Rng.Bug.Range(0, 100) < modifiers.LadybugSpawnProbability * 100;
        // 무당벌레가 가득 찼으면 일반 벌레가 나온다
        if (ladybug && (modifiers.MaxLadybugCount <= 0 || ladybugCount < modifiers.MaxLadybugCount)) return Ladybug;
        return GameRun.Rng.Bug.Range(0, Math.Min(BugSchedule.GetVarietyCount(stage), kindCount));
    }
}

public enum BugMove
{
    /// <summary>식물을 앞 칸부터 차례로 쫓는다.</summary>
    Chase,
    /// <summary>맞은편으로 곧장 지나간다.</summary>
    Straight,
    /// <summary>화면 안을 떠돈다.</summary>
    Wander,
}

/// <summary>벌레가 보는 밭.</summary>
public interface IBugTargets
{
    int CellCount { get; }
    bool TryGetPlant(int cell, out Vector2 position, out bool isNepenthes);
}

/// <summary>벌레 한 마리의 움직임. 화면, 클릭, 충돌은 <see cref="Bug"/>가 맡는다.</summary>
public sealed class BugMotion
{
    private const float WanderX = 9f, WanderY = 5f;
    private const float ArriveDistance = 0.5f;

    private readonly BugMove move;
    private readonly bool stopsToEat;

    private int targetCell = 999;
    private int lureCell = -1;
    private bool eating;
    private float eatLeft;

    private Vector2 from, control1, control2, to;
    private float progress;

    public BugMotion(BugMove move, float speed, bool stopsToEat)
    {
        this.move = move;
        this.stopsToEat = stopsToEat;
        Speed = speed;
    }

    public float Speed { get; }
    public Vector2 Position { get; set; }
    /// <summary>나타난 화면 가장자리. 위, 오른쪽, 아래, 왼쪽.</summary>
    public int Edge { get; set; }

    public void PlaceAtRandomEdge()
    {
        Edge = GameRun.Rng.Bug.Range(0, 4);
        switch (Edge)
        {
            case 0: Position = new Vector2(GameRun.Rng.Bug.Range(-9f, 9f), 6f); break;
            case 1: Position = new Vector2(10f, GameRun.Rng.Bug.Range(-5f, 5f)); break;
            case 2: Position = new Vector2(GameRun.Rng.Bug.Range(-9f, 9f), -6f); break;
            default: Position = new Vector2(-10f, GameRun.Rng.Bug.Range(-5f, 5f)); break;
        }
    }

    /// <summary>움직이기 시작할 때. 네펜데스에게서 풀려났을 때도 길을 새로 정한다.</summary>
    public void Begin()
    {
        if (move == BugMove.Straight) to = -Position;
        else if (move == BugMove.Wander) PickWanderPath();
    }

    /// <summary>식물에 닿았다. 먹는 동안 멈춘다.</summary>
    public void Eat()
    {
        if (eating) return;
        eating = true;
        eatLeft = BugRules.EatingSeconds;
    }

    /// <summary>네펜데스의 페로몬에 이끌린다.</summary>
    public void LureTo(int cell) => lureCell = cell;

    /// <param name="canMove">낮이고 맞거나 죽지 않았는가.</param>
    /// <returns>떠나서 사라졌으면 false.</returns>
    public bool Step(float deltaTime, bool canMove, IBugTargets targets)
    {
        if (lureCell >= 0)
        {
            if (!canMove) return true;
            if (targets.TryGetPlant(lureCell, out Vector2 trap, out bool isNepenthes) && isNepenthes)
            {
                MoveToward(trap, Speed * 0.5f * deltaTime);
                return true;
            }
            lureCell = -1;
            Begin();
        }

        if (eating && stopsToEat)
        {
            eatLeft -= deltaTime;
            if (eatLeft > 0f) return true;
            eating = false;
        }

        switch (move)
        {
            case BugMove.Chase:
                if (!targets.TryGetPlant(targetCell, out Vector2 plant, out _)) return FindNewTarget(targets);
                if (canMove) MoveToward(plant, Speed * deltaTime);
                return true;

            case BugMove.Straight:
                if (Vector2.Distance(Position, to) <= ArriveDistance) return false;
                if (canMove) MoveToward(to, Speed * deltaTime);
                return true;

            default:
                if (canMove) Wander(deltaTime);
                return true;
        }
    }

    private void MoveToward(Vector2 target, float distance) => Position = Vector2.MoveTowards(Position, target, distance);

    // 지금 목표의 다음 칸부터 네펜데스가 아닌 식물을 찾는다
    private bool FindNewTarget(IBugTargets targets)
    {
        for (int i = 0, cell = targetCell + 1; i < targets.CellCount; i++, cell++)
        {
            if (cell >= targets.CellCount) cell = 0;
            if (!targets.TryGetPlant(cell, out _, out bool isNepenthes) || isNepenthes) continue;
            targetCell = cell;
            return true;
        }
        return false;
    }

    private void Wander(float deltaTime)
    {
        progress += deltaTime / 10f * Speed;
        if (Vector2.Distance(Position, to) < ArriveDistance) PickWanderPath();

        Vector2 a = Vector2.Lerp(from, control1, progress);
        Vector2 b = Vector2.Lerp(control1, control2, progress);
        Vector2 c = Vector2.Lerp(control2, to, progress);
        Vector2 d = Vector2.Lerp(a, b, progress);
        Vector2 e = Vector2.Lerp(b, c, progress);
        MoveToward(Vector2.Lerp(d, e, progress), Speed * deltaTime);
    }

    private void PickWanderPath()
    {
        progress = 0f;
        from = Position;
        to = RandomPoint();
        control1 = RandomPoint();
        control2 = RandomPoint();
    }

    private static Vector2 RandomPoint()
        => new Vector2(GameRun.Rng.Bug.Range(-WanderX, WanderX), GameRun.Rng.Bug.Range(-WanderY, WanderY));
}
