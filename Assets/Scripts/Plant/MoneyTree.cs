using UnityEngine;

public class MoneyTree : Plant
{
    private int survivedTurns = 0;
    private const int REQUIRED_TURNS = 5;
    private const int REWARD_GOLD = 5000;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "돈나무";
        base.Init(gridIndex, grid);
        plantID = 3; // 네펜데스가 2이므로 3으로 설정
        
        // 이벤트 구독 (중복 방지)
        if (this.grid != null)
        {
            this.grid.OnGridStateChanged -= OnGridStateChanged;
            this.grid.OnGridStateChanged += OnGridStateChanged;
        }

        // 다음 프레임에 인접 식물 체크 (초기화 완료 후 체크)
        StartCoroutine(CheckAdjacentPlantsDelayed());
    }

    private void OnDestroy()
    {
        if (grid != null)
        {
            grid.OnGridStateChanged -= OnGridStateChanged;
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
        // 웨이브를 버틸 때마다 턴 증가
        survivedTurns++;

        // 인접 식물이 있는지 확인
        if (!CheckAdjacentPlants())
        {
            // 인접 식물이 없으면 즉시 사망
            Die(DeathCause.Other);
            return;
        }

        // 5턴 생존 시 보상 지급 후 소멸
        if (survivedTurns >= REQUIRED_TURNS)
        {
            // 골드 지급
            if (GameManager.Instance != null && GameManager.Instance.economyManager != null)
            {
                GameManager.Instance.economyManager.AddGold(REWARD_GOLD);
                
                // 알림 표시
                PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = "돈나무 수확 완료!",
                        message = $"{REWARD_GOLD}골드를 획득했습니다.",
                        duration = 3f
                    }
                );
            }

            // 자동 소멸
            Die(DeathCause.Other);
        }
    }

    public override float GetResistanceValue(int order)
    {
        // 네펜데스처럼 모든 웨이브에 100% 저항
        return 1f;
    }

    public override int GetSellingPrice()
    {
        // 판매 불가 (네펜데스와 동일)
        return 0;
    }

    /// <summary>
    /// 인접 4칸(상하좌우)에 완두콩류 식물(Pea 또는 Peanut)이 있는지 확인
    /// </summary>
    private bool CheckAdjacentPlants()
    {
        if (grid == null)
            return false;

        int maxIndex = grid.GetMaxCol() * 4;
        // offset: -4(왼쪽), +4(오른쪽), -1(위), +1(아래)
        int[] adjacentOffsets = { -4, 4, -1, 1 };

        foreach (int offset in adjacentOffsets)
        {
            int adjacentIndex = gridIndex + offset;

            // 범위 체크
            if (adjacentIndex < 0 || adjacentIndex >= maxIndex)
                continue;

            // 좌우 이동 시 같은 행에 있는지 체크
            if (offset == -4 || offset == 4)
            {
                // 좌우 이동은 열이 바뀌므로 행이 같아야 함
                int currentRow = gridIndex % 4;
                int adjacentRow = adjacentIndex % 4;
                if (currentRow != adjacentRow)
                    continue;
            }

            // 인접 칸에 식물이 있는지 확인
            if (grid.plantGrid.TryGetValue(adjacentIndex, out Plant adjacentPlant))
            {
                // 완두콩류 식물인지 확인
                if (adjacentPlant is Pea || adjacentPlant is Peanut)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public int GetSurvivedTurns()
    {
        return survivedTurns;
    }

    public void SetSurvivedTurns(int turns)
    {
        survivedTurns = turns;
    }
}
