using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>땅에 놓이는 효과를 맡는다. 전용 비료, 흡수 비료, 황금 토양, 페트병, 냉각 방패, 땅부자 보너스.</summary>
public class SoilEffects
{
    private readonly Grid grid;
    private readonly PlantBoard board;
    private readonly GameObject petBottleMarkerPrefab;
    private readonly GameObject goldSoilMarkerPrefab;

    private readonly Dictionary<int, GameObject> petMarkers = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, GameObject> goldSoilMarkers = new Dictionary<int, GameObject>();

    /// <summary>어느 칸에 무엇이 있는지. 규칙 계산은 이쪽을 보고, 이 클래스는 마커와 연출을 더한다.</summary>
    public SoilState State { get; }

    // 아래는 State의 값을 예전 이름으로 가리킨다.
    private List<int> goldSoilTiles => State.GoldSoilTiles;
    private List<int> petBottleTiles => State.PetBottleTiles;
    private Dictionary<int, int> petBottleBlockCount => State.PetBottleBlockCount;
    private Dictionary<int, WaveType> fertilizerColumns => State.FertilizerColumns;
    private HashSet<int> absorbFertilizerTiles => State.AbsorbFertilizerTiles;
    private Dictionary<int, float> columnGoldMulBonus => State.ColumnGoldMulBonus;
    private bool hasCoolingShield { get => State.HasCoolingShield; set => State.HasCoolingShield = value; }
    private bool isCoolingShieldActivated { get => State.IsCoolingShieldActivated; set => State.IsCoolingShieldActivated = value; }

    public List<int> GoldSoilTiles => goldSoilTiles;
    public List<int> PetBottleTiles => petBottleTiles;
    public HashSet<int> AbsorbFertilizerTiles => absorbFertilizerTiles;
    public bool HasCoolingShield => hasCoolingShield;

    public SoilEffects(Grid grid, GameObject petBottleMarkerPrefab, GameObject goldSoilMarkerPrefab)
    {
        this.grid = grid;
        board = grid.Board;
        State = new SoilState(grid.Board, grid.Modifiers);
        this.petBottleMarkerPrefab = petBottleMarkerPrefab;
        this.goldSoilMarkerPrefab = goldSoilMarkerPrefab;
    }

    /// <summary>페트병 마커는 부모 없이 만들어져서 UI 전환 때 꺼질 수 있어 매 프레임 되살린다.</summary>
    public void KeepPetMarkersVisible()
    {
        // petMarker가 항상 표시되도록 보장 (부모 비활성화 방지 + UI 전환 시 Culling 방지)
        foreach (var kvp in petMarkers)
        {
            if (kvp.Value != null)
            {
                // 비활성화되었으면 다시 활성화
                if (!kvp.Value.activeSelf)
                {
                    kvp.Value.SetActive(true);
                }

                // SpriteRenderer가 있으면 강제로 렌더링 활성화
                var sr = kvp.Value.GetComponent<SpriteRenderer>();
                if (sr != null && !sr.enabled)
                {
                    sr.enabled = true;
                }
            }
        }
    }

    public void SetPetBottleBlockCountBonus(int value)
    {
        petBottleBlockCount[-1] = value;
    }

    public bool HasPetBottle(int idx) => State.HasPetBottle(idx);
    public bool HasGoldSoil(int idx) => State.HasGoldSoil(idx);
    public bool HasFertilizerAt(int idx) => State.HasFertilizerAt(idx);
    private int GetCol(int index) => index / 4;
    public int GetPetBottleBlockCountBonus() => petBottleBlockCount.ContainsKey(-1) ? petBottleBlockCount[-1] : 0;
    public float GetColumnGoldMulBonus(int idx) => State.GetColumnGoldMulBonus(idx);

    public void AddAbsorbFertilizer(int index)
    {
        if (State.AddAbsorbFertilizer(index))
            Debug.Log($"Absorb Fertilizer Added at {index}"); // 시각적 피드백 (예: 보라색 비료 파티클 등) - 필요 시 구현
    }
    public List<int> GetAbsorbFertilizerTiles()
    {
        return absorbFertilizerTiles.ToList();
    }
    public bool HasAbsorbFertilizer(int index) => State.HasAbsorbFertilizer(index);
    public bool TryPlaceGoldSoil(int idx)
    {
        if (!State.PlaceGoldSoil(idx)) return false;

        // 시각화 (황금색 마커)
        if (goldSoilMarkerPrefab != null)
        {
            var soilT = grid.GetSoilTransform(idx);
            var marker = Object.Instantiate(goldSoilMarkerPrefab, soilT.position, Quaternion.identity, soilT);
            goldSoilMarkers[idx] = marker;
        }

        Debug.Log($"[Grid] 황금 비료 설치: idx={idx}");
        return true;
    }
    public void RemoveGoldSoil(int idx)
    {
        if (goldSoilTiles.Contains(idx))
        {
            goldSoilTiles.Remove(idx);

            if (goldSoilMarkers.TryGetValue(idx, out var marker) && marker != null)
            {
                Object.Destroy(marker);
                goldSoilMarkers.Remove(idx);
            }
        }
    }
    public void PlacePetBottle(int idx)
    {
        try
        {
            if (!State.PlacePetBottle(idx)) return;

            // 시각화(선택)
            if (petBottleMarkerPrefab != null)
            {
                var soilT = grid.GetSoilTransform(idx);
                if (soilT != null)
                {
                    // petMarker를 부모 없이 생성하여 부모 비활성화 영향 방지 (고추 ghost prefab과 동일한 방식)
                    var marker = Object.Instantiate(petBottleMarkerPrefab, soilT.position + new Vector3(-0.1f, 0f, 0f), Quaternion.identity);
                    petMarkers[idx] = marker;
                    // 항상 표시되도록 보장
                    if (marker != null)
                    {
                        marker.SetActive(true);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Grid] PlacePetBottle exception: idx={idx}, error={e.Message}\n{e.StackTrace}");
        }
    }
    /// <summary>보호 횟수를 다 쓴 페트병의 마커를 치운다. 판정은 <see cref="SoilState.TryBlockDeathWithPetBottle"/>가 한다.</summary>
    public void RemovePetBottleMarker(int idx)
    {
        if (petMarkers.TryGetValue(idx, out var marker) && marker != null)
        {
            Object.Destroy(marker);
            petMarkers.Remove(idx);
        }
    }
    public Dictionary<int, WaveType> GetFertilizerColumns()
    {
        return fertilizerColumns;
    }
    public WaveType GetFertilizerType(int idx)
    {
        return fertilizerColumns[GetCol(idx)];
    }
    public bool TryPlaceFertilizer(int idx, WaveType wave)
    {
        if (!State.PlaceFertilizer(idx, wave)) return false;
        int col = GetCol(idx);

        Transform soilColT = grid.transform.GetChild(col);
        var marker = soilColT.GetComponentInChildren<FertilizerMarker>(true);

        if (marker == null)
        {
            Debug.Log($"[Grid] No marker: col={col}, wave={wave}");
            return false;
        }
        marker.SetFertilizer(wave);

        Debug.Log($"[Grid] Fertilizer placed: col={col}, wave={wave}");
        grid.Scouter.UpdateResistanceScouterImageInGrid(grid.Enemy.CurrentWave.WaveType);

        // 전용 비료가 4줄 이상 존재하면 저항력 흡수 비료 해금
        if (fertilizerColumns.Count >= 4)
            UnlockGrants.GrantEventUnlocks(UnlockManager.Ids.FertilizerFourColumns);

        return true;
    }
    public void RemoveFertilizerAt(int col)
    {
        // SoilCol Transform 가져오기
        Transform soilColT = grid.transform.GetChild(col);
        var marker = soilColT.GetComponentInChildren<FertilizerMarker>(true);

        if (marker != null && marker.IsOn)
        {
            if (fertilizerColumns.ContainsKey(col))
            {
                fertilizerColumns.Remove(col);
            }

            marker.RemoveFertilizer();
            grid.Scouter.UpdateResistanceScouterImageInGrid(grid.Enemy.CurrentWave.WaveType);
            Debug.Log($"[Grid] Fertilizer removed at col={soilColT.GetSiblingIndex()}");
        }
    }
    public bool HasEmptyFetrilizerGrid()
    {
        for (int idx = 0; idx < board.MaxCol; idx++)
        {
            if (!fertilizerColumns.ContainsKey(idx))
            {
                return true;
            }
        }
        return false;
    }
    public void AddPetBottleBlockCount(int value) => State.AddPetBottleBlockCount(value);
    /// <summary>
    /// 냉각 방패 활성화 (구매 시 호출)
    /// </summary>
    public void ActivateCoolingShield()
    {
        hasCoolingShield = true;
    }
    /// <summary>
    /// 냉각 방패가 활성화되었는지 확인 (1회 사용됨 여부)
    /// </summary>
    public bool IsCoolingShieldActivated()
    {
        return isCoolingShieldActivated;
    }
    /// <summary>
    /// 냉각 방패 사용 처리 (1회 사용)
    /// </summary>
    public void SetCoolingShieldActivated()
    {
        isCoolingShieldActivated = true;
        hasCoolingShield = false; // 사용 후 소멸
    }
    public bool TryGetFertilizerType(int gridIndex, out WaveType type) => State.TryGetFertilizerType(gridIndex, out type);
    /// <summary>특수(땅부자): 무작위 세로줄에 고속 숙성 효과(+0.5 = 풀업과 동일) 추가.</summary>
    public void AddLandRichColumn()
    {
        Dictionary<Plant, int> previousPrices = grid.Upgrades.CapturePlantSellingPrices();
        int col = State.AddLandRichColumn();
        grid.Upgrades.RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
        Debug.Log($"[SpecialItem] 땅부자: {col + 1}번째 세로줄에 고속 숙성 효과 (+0.5, 누적 {columnGoldMulBonus[col]})");
    }
    public List<float> GetColumnGoldMulBonusForSave()
    {
        var list = new List<float>();
        for (int col = 0; col < board.MaxCol; col++)
            list.Add(columnGoldMulBonus.TryGetValue(col, out float v) ? v : 0f);
        return list;
    }
    public void LoadColumnGoldMulBonus(List<float> list)
    {
        columnGoldMulBonus.Clear();
        if (list == null) return;
        for (int col = 0; col < list.Count; col++)
            if (list[col] > 0f) columnGoldMulBonus[col] = list[col];
    }

    /// <summary>특수(알록달록): 해당 칸에 적용 중인 땅/식물 효과 개수 (정보 UI 기준과 동일).</summary>
    public int CountTileEffects(int idx) => State.CountTileEffects(idx);
    /// <summary>특수(임시완두B): 주변 4칸의 서로 다른 식물 종 수.</summary>
    public int CountDistinctNeighborSpecies(int idx) => State.CountDistinctNeighborSpecies(idx);
    /// <summary>특수(순환): 판매한 식물 주변 4칸 식물의 모든 저항력을 5%p 회복 (형질별 최대 90%).</summary>
    public void HealNeighborsBySell(int idx) => State.HealNeighborsBySell(idx);
    public bool IsAffectedBySprinkler(int targetIdx) => State.IsAffectedBySprinkler(targetIdx);
    public bool IsAffectedByChiliPepper(int gridIndex) => State.IsAffectedByChiliPepper(gridIndex);
    public void ApplyRapidFreezer(int centerIdx)
    {
        foreach (PlantState target in State.GetFreezeTargets(centerIdx))
        {
            Plant plant = board.Plants[target.GridIndex];
            plant.SetFrozen(true);
            grid.Breeder.DeselectPlant(plant);
        }
        Debug.Log($"[RapidFreezer] Applied to 3x3 around index {centerIdx}");
    }
    public bool IsPlantFrozen(int gridIndex) => State.IsPlantFrozen(gridIndex);
}
