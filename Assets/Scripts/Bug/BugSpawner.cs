using System.Collections.Generic;
using UnityEngine;

/// <summary>낮 동안의 벌레와 무당벌레 소환을 맡는다. 시간은 교배 진행 쪽에서 <see cref="Tick"/>으로 넘겨 준다.</summary>
public class BugSpawner
{
    private readonly PlantBoard board;
    private readonly GridModifiers modifiers;
    private readonly List<GameObject> bugPrefabs;
    private readonly GameObject ladybugPrefab;

    private readonly BugSpawnState state = new BugSpawnState();

    public BugSpawner(PlantBoard board, GridModifiers modifiers, List<GameObject> bugPrefabs, GameObject ladybugPrefab)
    {
        this.board = board;
        this.modifiers = modifiers;
        this.bugPrefabs = bugPrefabs;
        this.ladybugPrefab = ladybugPrefab;
    }

    /// <summary>하루를 시작하며 오늘의 소환 주기를 계산한다.</summary>
    public void BeginDay()
        => state.BeginDay(GameManager.Instance.stage, ModManager.Instance.GetMul(StatId.BugSpawnIntervalMul, -1));

    public void Tick(float deltaTime)
    {
        for (int count = state.Tick(deltaTime, modifiers, board.Plants.Count > 0); count > 0; count--)
            Spawn();
    }

    public void Spawn(bool ignoreStageGate = false)
    {
        if (GameManager.Instance?.Tutorial?.CanSpawnBugs == false) return;
        if (GameManager.Instance != null && GameManager.Instance.GetGameIsStopped())
            return;

        int stage = GameManager.Instance.stage;
        if (!ignoreStageGate && stage < BugSchedule.AppearStage) // 벌레 등장 전엔 스폰 X
            return;

        int kind = BugSpawnState.RollKind(modifiers, board.LadybugCount, stage, bugPrefabs.Count);
        Object.Instantiate(kind == BugSpawnState.Ladybug ? ladybugPrefab : bugPrefabs[kind]);
    }

    public void Save(GridSave save)
    {
        save.bugSpawnTimeInterval = state.Interval;
        save.lastBugSpawnTimeInterval = state.Elapsed;
    }

    public void Load(GridSave saveData)
    {
        state.Interval = saveData.bugSpawnTimeInterval;
        state.Elapsed = saveData.lastBugSpawnTimeInterval;
    }
}
