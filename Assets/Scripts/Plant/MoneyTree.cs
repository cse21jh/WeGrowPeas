using UnityEngine;

public class MoneyTree : Plant
{
    private const int REWARD_GOLD = PlantState.MoneyTreeRewardGold;

    public override PlantKind Kind => PlantKind.MoneyTree;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = PlantIds.MoneyTree;
        base.Init(gridIndex, grid);
        plantID = 5; 
        
        // 이벤트 구독 (중복 방지)
        if (this.grid != null)
        {
            this.grid.Board.Changed -= OnGridStateChanged;
            this.grid.Board.Changed += OnGridStateChanged;
        }

        // 다음 프레임에 인접 식물 체크 (초기화 완료 후 체크)
        StartCoroutine(CheckAdjacentPlantsDelayed());
    }

    private void OnDestroy()
    {
        if (grid != null)
        {
            grid.Board.Changed -= OnGridStateChanged;
        }
    }

    private void OnGridStateChanged()
    {
        if (isDying) return;

        // 인접 식물 체크
        if (!CheckAdjacentPlants())
        {
            Debug.Log($"[MoneyTree] Grid 변경 감지: 인접 식물이 없어 사망합니다. GridIndex: {gridIndex}");
            Die(DeathCause.Other);
        }
    }

    private System.Collections.IEnumerator CheckAdjacentPlantsDelayed()
    {
        // 한 프레임 대기 (초기화 완료 보장)
        yield return null;
        
        // 인접 식물 체크
        if (!CheckAdjacentPlants())
        {
            Debug.Log($"[MoneyTree] 인접 식물이 없어 즉시 사망합니다. GridIndex: {gridIndex}");
            // 인접 식물이 없으면 즉시 사망
            Die(DeathCause.Other);
        }
        else
        {
            Debug.Log($"[MoneyTree] 인접 식물이 있어 생존합니다. GridIndex: {gridIndex}");
        }
    }

    public override void ResistWave(WaveType waveType)
    {
        // 턴을 올리고, 주변에 완두·땅콩이 없으면 죽고, 5턴을 채우면 보상을 주고 사라진다. 판정은 State가 한다.
        MoneyTreeResult outcome = State.MoneyTreeAfterWave();

        if (outcome == MoneyTreeResult.DiesAlone)
        {
            Die(DeathCause.Other);
            return;
        }

        if (outcome == MoneyTreeResult.Matured)
        {
            // 골드 지급
            if (GameManager.Instance != null && GameManager.Instance.economyManager != null)
            {
                GameManager.Instance.economyManager.AddGold(REWARD_GOLD);
                
                // 알림 표시
                PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = Loc.T("game.money_tree.title"),
                        message = Loc.T("game.money_tree.message", ("gold", REWARD_GOLD)),
                        duration = 3f
                    }
                );
            }

            // 자동 소멸
            Die(DeathCause.Other);
        }
    }

    /// <summary>
    /// 인접 4칸(상하좌우)에 완두콩류 식물(Pea 또는 Peanut)이 있는지 확인
    /// </summary>
    private bool CheckAdjacentPlants() => State.HasAdjacentBreedablePlant();

    public int GetSurvivedTurns() => State.SurvivedTurns;

    public void SetSurvivedTurns(int turns) => State.SurvivedTurns = turns;
}
