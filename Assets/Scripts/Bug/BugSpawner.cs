using System.Collections.Generic;
using UnityEngine;

/// <summary>낮 동안의 벌레와 무당벌레 소환을 맡는다. 시간은 교배 진행 쪽에서 <see cref="Tick"/>으로 넘겨 준다.</summary>
public class BugSpawner
{
    private readonly Grid grid;
    private readonly List<GameObject> bugPrefabs;
    private readonly GameObject ladybugPrefab;

    private float spawnTimeInterval = 15.0f;
    private float lastSpawnTime = 0f;
    private float effectiveInterval;

    public BugSpawner(Grid grid, List<GameObject> bugPrefabs, GameObject ladybugPrefab)
    {
        this.grid = grid;
        this.bugPrefabs = bugPrefabs;
        this.ladybugPrefab = ladybugPrefab;
    }

    /// <summary>하루를 시작하며 오늘의 소환 주기를 계산한다.</summary>
    public void BeginDay()
    {
        // 새벽: 벌레 등장 딜레이 감소(더 자주 등장). 저주(벌레 대발생)도 추가 감소. 최소 0.5초 보장.
        float dawnAdjustedBase = Mathf.Max(0.5f, spawnTimeInterval - DawnSystem.Current.bugDelayReduction - CurseState.BugFestivalDelayReduce);
        effectiveInterval = dawnAdjustedBase * ModManager.Instance.GetMul(StatId.BugSpawnIntervalMul, -1);

        if (GameManager.Instance.stage == BugSchedule.AppearStage)
            lastSpawnTime = 0f;
    }

    public void Tick(float deltaTime)
    {
        lastSpawnTime += deltaTime;

        if (lastSpawnTime > effectiveInterval * (1f + grid.Modifiers.BugSpawnIntervalIncreasement))
        {
            List<int> targetIdx = new List<int>(grid.plantGrid.Keys);
            if (targetIdx.Count > 0)
            {
                Spawn();
                if (CurseState.BugFestival) Spawn(); // 저주: 벌레 대발생(한 번에 2마리)
                lastSpawnTime = 0f;
            }
        }
    }

    public void Spawn(bool ignoreStageGate = false)
    {
        if (GameManager.Instance != null && GameManager.Instance.GetGameIsStopped())
            return;

        int stage = GameManager.Instance.stage;
        if (!ignoreStageGate && stage < BugSchedule.AppearStage) // 벌레 등장 전엔 스폰 X
            return;
        if (Random.Range(0, 100) < (grid.Modifiers.LadybugSpawnProbability * 100))
        {
            // 최대 무당벌레 수 제한 확인
            if (grid.Modifiers.MaxLadybugCount > 0 && grid.ladybugs.Count >= grid.Modifiers.MaxLadybugCount)
            {
                // 최대 수에 도달했으므로 일반 벌레 스폰
                int bugIndex = Random.Range(0, Mathf.Min(BugSchedule.GetVarietyCount(stage), bugPrefabs.Count));
                Object.Instantiate(bugPrefabs[bugIndex]);
                return;
            }
            Object.Instantiate(ladybugPrefab);
            return;
        }
        int i = Random.Range(0, Mathf.Min(BugSchedule.GetVarietyCount(stage), bugPrefabs.Count)); //벌레 해금 시기와 일치하도록 설정
        Object.Instantiate(bugPrefabs[i]);
        return;
    }

    public void Save(GridSave save)
    {
        save.bugSpawnTimeInterval = spawnTimeInterval;
        save.lastBugSpawnTimeInterval = lastSpawnTime;
    }

    public void Load(GridSave saveData)
    {
        spawnTimeInterval = saveData.bugSpawnTimeInterval;
        lastSpawnTime = saveData.lastBugSpawnTimeInterval;
    }
}
