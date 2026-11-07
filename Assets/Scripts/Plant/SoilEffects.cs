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
    private readonly List<int> goldSoilTiles = new List<int>(); // 황금 비료가 뿌려진 타일들
    private readonly Dictionary<int, GameObject> goldSoilMarkers = new Dictionary<int, GameObject>();
    private readonly List<int> petBottleTiles = new List<int>();
    private readonly Dictionary<int, int> petBottleBlockCount = new Dictionary<int, int>(); // 페트병 위치별 남은 보호 횟수. -1은 전체 보너스
    private readonly Dictionary<int, WaveType> fertilizerColumns = new();
    private readonly HashSet<int> absorbFertilizerTiles = new HashSet<int>(); // 저항력 흡수 비료가 뿌려진 타일들
    private readonly Dictionary<int, float> columnGoldMulBonus = new Dictionary<int, float>(); // 땅부자: col → +mul
    private bool hasCoolingShield = false;
    private bool isCoolingShieldActivated = false;

    public List<int> GoldSoilTiles => goldSoilTiles;
    public List<int> PetBottleTiles => petBottleTiles;
    public HashSet<int> AbsorbFertilizerTiles => absorbFertilizerTiles;
    public bool HasCoolingShield => hasCoolingShield;

    public SoilEffects(Grid grid, GameObject petBottleMarkerPrefab, GameObject goldSoilMarkerPrefab)
    {
        this.grid = grid;
        board = grid.Board;
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

    public bool HasPetBottle(int idx) => petBottleTiles.Contains(idx);
    public bool HasGoldSoil(int idx) => goldSoilTiles.Contains(idx);
    public bool HasFertilizerAt(int idx) => fertilizerColumns.ContainsKey(GetCol(idx));
    private int GetCol(int index) => index / 4;
    public int GetPetBottleBlockCountBonus() => petBottleBlockCount.ContainsKey(-1) ? petBottleBlockCount[-1] : 0;
    public float GetColumnGoldMulBonus(int idx)
        => columnGoldMulBonus.TryGetValue(idx / 4, out float v) ? v : 0f;

    public void AddAbsorbFertilizer(int index)
    {
        if (index < 0 || index >= board.MaxCol * 4) return;
        if (!absorbFertilizerTiles.Contains(index))
        {
            absorbFertilizerTiles.Add(index);
            board.NotifyChanged(); // 밭 정보 UI 갱신
            // 시각적 피드백 (예: 보라색 비료 파티클 등) - 필요 시 구현
            Debug.Log($"Absorb Fertilizer Added at {index}");
        }
    }
    public List<int> GetAbsorbFertilizerTiles()
    {
        return absorbFertilizerTiles.ToList();
    }
    public bool HasAbsorbFertilizer(int index)
    {
        return absorbFertilizerTiles.Contains(index);
    }
    public void ProcessResistanceAbsorption()
    {
        Debug.Log("[AbsorbFertilizer] Start Processing...");
        // 흡수 비료가 있는 타일 순회
        foreach (int centerIdx in absorbFertilizerTiles)
        {
            // 중심 타일에 식물이 있어야 함
            if (!board.Plants.TryGetValue(centerIdx, out Plant centerPlant)) continue;
            if (centerPlant.isDying) continue;

            List<GeneticTrait> centerTraits = centerPlant.GetGeneticTrait().ToList();

            // 상하좌우 인접 타일 검사
            int[] neighborIndices = GetNeighborIndices(centerIdx);

            foreach (int neighborIdx in neighborIndices)
            {
                if (!board.Plants.TryGetValue(neighborIdx, out Plant neighborPlant)) continue;
                if (neighborPlant.isDying) continue;
                if (centerPlant == neighborPlant) continue; // 혹시라도 자기 자신이면 패스

                List<GeneticTrait> neighborTraits = neighborPlant.GetGeneticTrait().ToList();

                // 겹치는 형질 찾기
                foreach (var cTrait in centerTraits)
                {
                    foreach (var nTrait in neighborTraits)
                    {
                        if (cTrait.traitType == nTrait.traitType)
                        {
                            // 겹치는 형질 발견! 흡수 진행
                            // 이웃 식물의 저항력을 3% 깎음 (하한선 10%)
                            float currentNeighborRes = neighborPlant.GetResistanceValue((int)nTrait.traitType);
                            // 기본 저항력(traits의 resistance)을 가져와야 하는데 GetResistanceValue는 보정치 포함임.
                            // 하지만 ChangeResistance는 delta를 받아서 traits의 resistance를 수정함.
                            // 따라서 "현재 저항력이 10% 이상일 때만" 깎아야 함.
                            // Plant.ChangeResistance 내부 로직상 "traits[i].resistance"를 직접 수정하고 0.1f 하한선을 둠.
                            // 하지만 여기서 하한선을 체크하려면 neighborPlant의 순수 resistance를 알아야 함.
                            // ChangeResistance가 0.1f 미만으로 안 떨어지게 내부 방어 로직이 있으므로,
                            // 그냥 -0.03f를 호출하면 됨. 
                            // 단, "뺏어옴"이므로, 상대가 잃은 만큼 내가 가져와야 함.
                            // 만약 상대가 이미 0.1f라서 못 잃으면? 나도 못 가져오는 게 맞음.

                            // -> ChangeResistance는 bool을 리턴하지 않고 void였음 (수정 전).
                            // 아까 public bool ChangeResistance로 바꿨음! 
                            // ChangeResistance 내부에서: var < 0.1f 면 var = 0.1f로 보정함.
                            // 근데 "실제로 깎였는지"를 알아야 내가 가져올 수 있음.

                            // 로직 개선:
                            // 1. 상대의 현재 raw 저항력을 안쪽에서 알기 어려우므로(public 접근자 부족),
                            //    ChangeResistance가 "실제 변동량"을 리턴하거나, 
                            //    외부에서 예측해야 함.

                            // 여기서는 간단하게 구현: "일단 깎아보고, 깎는데 성공했으면 나도 올린다"
                            // 근데 ChangeResistance는 "변동 후 값"을 리턴하거나 하지 않음.
                            // bool 리턴도 그냥 "amount != 0"이면 true였음 (수정 전 추측).

                            // Plant.ChangeResistance를 다시 보자.
                            // if (var < 0.1f) var = 0.1f;
                            // 이렇게 되어있음. 
                            // 즉, 이미 0.1이면 더 안 깎임.

                            // 정확한 흡수를 위해선 상대 줄이기 -> 성공 여부/량 확인 -> 나 늘리기 순서가 필요.
                            // 지금 Plant 구조상 private List<GeneticTrait> traits; 이고 GetGeneticTrait()는 리스트 반환.
                            // 리스트의 요소는 struct면 복사본.. class면 참조.
                            // GeneticTrait는 struct인가 class인가? -> 확인 필요.

                            // 보통 struct로 구현했을 가능성 높음. Plant.cs 보면 traits[i] = new GeneticTrait(...) 이렇게 덮어쓰고 있음. struct임.

                            // 따라서 ChangeResistance를 믿어야 함.
                            // ChangeResistance를 조금 더 똑똑하게 수정해서 "실제 적용된 수치"를 반환하게 하거나
                            // 아니면 "줄일 수 있는지 확인"하는 메서드가 필요함.

                            // 여기서는 "하한선 10%" 규칙이 있으므로,
                            // 상대방의 해당 형질 저항력이 0.13f 이상일 때만 흡수 가능하다고 가정.
                            // 근데 GetResistanceValue는 보정된 값(비료 등 포함)이라 raw resistance가 아님.

                            // -> 정밀한 구현을 위해 ChangeResistance를 활용하되,
                            // 이번 기획은 "3%p씩 뺏어옴"이므로, 
                            // 그냥 -0.03f 호출하고, +0.03f 호출하는 식으로 진행. (상대가 0.1f여도 나는 오르면 '창조 흡수'가 되지만, 게임적 허용범위일 수 있음)
                            // "하한선 10%"는 뺏기는 쪽에 적용되는 룰.
                            // "뺏어옴"이라는 워딩을 엄격하게 지키려면 상대가 잃어야만 얻어야 함.

                            // 로직:
                            // 1. nTrait의 resistance가 0.1f보다 큰지 확인 (traits 리스트 순회해서 직접 확인해야 함)
                            // 2. 크다면 0.03f 감소 시도. (0.1f 밑으로는 안 내려가게)
                            // 3. 감소된 실제 양만큼 내 저항력 증가.

                            float amountToAbsorb = 0.03f;

                            // 이웃 식물의 해당 형질 raw resistance 찾기
                            float neighborRawRes = 0f;
                            // GetGeneticTrait()로 리스트 가져와서 찾기
                            var nTargetTrait = neighborTraits.FirstOrDefault(t => t.traitType == cTrait.traitType);
                            // default(struct) 체크
                            // GeneticTrait가 struct라면 traitType 비교가 애매할 수 있으니 loop로 찾자. (어차피 위에서 찾음)
                            neighborRawRes = nTargetTrait.resistance;

                            // 흡수 가능량 계산
                            float actualAbsorb = 0f;
                            if (neighborRawRes > 0.1f)
                            {
                                if (neighborRawRes - amountToAbsorb < 0.1f)
                                {
                                    actualAbsorb = neighborRawRes - 0.1f; // 0.1f까지만 뺌
                                }
                                else
                                {
                                    actualAbsorb = amountToAbsorb;
                                }
                            }

                            if (actualAbsorb > 0.0001f)
                            {
                                neighborPlant.ChangeResistance((int)nTrait.traitType, -actualAbsorb);
                                centerPlant.ChangeResistance((int)cTrait.traitType, actualAbsorb);
                                Debug.Log($"[Absorb] {centerIdx} absorbed {actualAbsorb:F3} from {neighborIdx} ({nTrait.traitType})");
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("[AbsorbFertilizer] Process Complete.");
    }
    private int[] GetNeighborIndices(int centerIdx)
    {
        List<int> neighbors = new List<int>();
        int col = centerIdx / 4;
        int row = centerIdx % 4;

        // 상 (row + 1)
        if (row + 1 < 4) neighbors.Add(centerIdx + 1);
        // 하 (row - 1)
        if (row - 1 >= 0) neighbors.Add(centerIdx - 1);
        // 좌 (col - 1)
        if (col - 1 >= 0) neighbors.Add(centerIdx - 4);
        // 우 (col + 1)
        if (col + 1 < board.MaxCol) neighbors.Add(centerIdx + 4);

        return neighbors.ToArray();
    }
    public bool TryPlaceGoldSoil(int idx)
    {
        if (goldSoilTiles.Contains(idx))
        {
            return false; // 이미 황금 비료가 있는 칸
        }
        if (board.Plants.ContainsKey(idx))
        {
            return false; // 식물이 있는 칸에는 황금 비료를 뿌릴 수 없음
        }

        goldSoilTiles.Add(idx);
        board.NotifyChanged(); // 밭 정보 UI 갱신

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
            if (petBottleTiles.Contains(idx))
            {
                return;
            }

            // 인덱스 범위 체크
            if (idx < 0 || idx >= board.MaxCol * 4)
            {
                Debug.LogError($"[Grid] PlacePetBottle: Invalid idx={idx}, board.MaxCol={board.MaxCol}, maxIdx={board.MaxCol * 4}");
                return;
            }

            petBottleTiles.Add(idx);
            board.NotifyChanged(); // 밭 정보 UI 갱신
            // 기본 보호 횟수 1 (재질 강화 아이템으로 증가 가능)
            int baseBlockCount = 1 + (petBottleBlockCount.ContainsKey(-1) ? petBottleBlockCount[-1] : 0); // -1은 전체 보호 횟수 보너스
            petBottleBlockCount[idx] = baseBlockCount;

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
    public bool TryInterceptDeath(int idx, DeathCause cause, Bug killer = null)
    {
        if (petBottleTiles.Contains(idx) && (cause == DeathCause.Generic || cause == DeathCause.Shovel)) // 웨이브나 삽에 의해 제거될 때, 페트병이 있는 경우
        {
            // 보호 횟수 확인
            if (petBottleBlockCount.ContainsKey(idx) && petBottleBlockCount[idx] > 0)
            {
                petBottleBlockCount[idx]--;
                if (petBottleBlockCount[idx] <= 0)
                {
                    // 보호 횟수 소진 → 페트병 제거
                    petBottleTiles.Remove(idx);
                    petBottleBlockCount.Remove(idx);

                    if (petMarkers.TryGetValue(idx, out var marker) && marker != null)
                    {
                        Object.Destroy(marker);
                        petMarkers.Remove(idx);
                    }
                }
                return true; // 보호 성공
            }
            else
            {
                // 보호 횟수가 없으면 기존 로직대로 제거
                petBottleTiles.Remove(idx);
                petBottleBlockCount.Remove(idx);

                if (petMarkers.TryGetValue(idx, out var marker) && marker != null)
                {
                    Object.Destroy(marker);
                    petMarkers.Remove(idx);
                }
                return true;
            }
        }

        if (cause == DeathCause.Bug && killer != null && board.Ladybugs.Count != 0) // 벌레로 인한 죽음인데, 필드에 익충이 있는 경우
        {
            grid.StartCoroutine(board.Ladybugs[0].KillFuckingBug(killer));
            return true;
        }

        return false; // 죽음 방어 실패
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
        if (HasFertilizerAt(idx)) return false; // 이미 다른 전용 비료가 있으면 불가

        int col = GetCol(idx);

        fertilizerColumns.Add(col, wave);
        board.NotifyChanged(); // 밭 정보 UI 갱신

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
    public void AddPetBottleBlockCount(int value)
    {
        // 전체 페트병 보호 횟수 보너스 (새로 설치되는 페트병에 적용)
        if (!petBottleBlockCount.ContainsKey(-1))
            petBottleBlockCount[-1] = 0;
        petBottleBlockCount[-1] += value;

        // 기존 페트병에도 보호 횟수 추가
        foreach (var idx in petBottleTiles)
        {
            if (!petBottleBlockCount.ContainsKey(idx))
                petBottleBlockCount[idx] = 0;
            petBottleBlockCount[idx] += value;
        }
    }
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
    public bool TryGetFertilizerType(int gridIndex, out WaveType type)
    {
        int col = gridIndex / 4;
        return fertilizerColumns.TryGetValue(col, out type);
    }
    /// <summary>특수(땅부자): 무작위 세로줄에 고속 숙성 효과(+0.5 = 풀업과 동일) 추가.</summary>
    public void AddLandRichColumn()
    {
        Dictionary<Plant, int> previousPrices = grid.Upgrades.CapturePlantSellingPrices();
        int col = Random.Range(0, board.MaxCol);
        if (!columnGoldMulBonus.ContainsKey(col)) columnGoldMulBonus[col] = 0f;
        columnGoldMulBonus[col] += 0.5f;
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
    public int CountTileEffects(int idx)
    {
        int count = 0;
        if (HasGoldSoil(idx)) count++;
        if (TryGetFertilizerType(idx, out _)) count++;
        if (HasPetBottle(idx)) count++;
        if (IsAffectedByChiliPepper(idx)) count++;
        if (IsPlantFrozen(idx)) count++;
        if (IsAffectedBySprinkler(idx)) count++;
        if (HasAbsorbFertilizer(idx)) count++;
        return count;
    }
    /// <summary>특수(임시완두B): 주변 4칸의 서로 다른 식물 종 수.</summary>
    public int CountDistinctNeighborSpecies(int idx)
    {
        var species = new HashSet<string>();
        int row = idx % 4;
        int[] neighbors = { idx - 4, idx + 4, row > 0 ? idx - 1 : -1, row < 3 ? idx + 1 : -1 };
        foreach (int n in neighbors)
        {
            if (n < 0 || n >= board.MaxCol * 4) continue;
            if (board.Plants.TryGetValue(n, out Plant p) && p != null && !string.IsNullOrEmpty(p.speciesname))
                species.Add(p.speciesname);
        }
        return species.Count;
    }
    /// <summary>특수(순환): 판매한 식물 주변 4칸 식물의 모든 저항력을 5%p 회복 (형질별 최대 90%).</summary>
    public void HealNeighborsBySell(int idx)
    {
        if (!SpecialItemSystem.Has("circulation")) return;

        int row = idx % 4;
        int[] neighbors = { idx - 4, idx + 4, row > 0 ? idx - 1 : -1, row < 3 ? idx + 1 : -1 };
        foreach (int n in neighbors)
        {
            if (n < 0 || n >= board.MaxCol * 4) continue;
            if (!board.Plants.TryGetValue(n, out Plant p) || p == null) continue;

            var traits = p.GetGeneticTrait();
            for (int i = 0; i < traits.Count; i++)
            {
                float cur = traits[i].resistance;
                if (cur >= 0.9f) continue;
                p.ChangeResistance((int)traits[i].traitType, Mathf.Min(0.05f, 0.9f - cur));
            }
        }
    }
    public bool IsAffectedBySprinkler(int targetIdx)
    {
        int range = 1 + grid.Modifiers.SprinklerRangeBonus;

        foreach (var kvp in board.Plants)
        {
            if (kvp.Value is Sprinkler)
            {
                if (kvp.Key == targetIdx)
                    return false; // 본인 자체에는 효과 X

                int sIdx = kvp.Key;

                int sRow = sIdx % 4;
                int tRow = targetIdx % 4;
                int sCol = sIdx / 4;
                int tCol = targetIdx / 4;
                // 같은 열이므로 Col은 같음. Row 차이 확인
                if (sCol == tCol && Mathf.Abs(sRow - tRow) <= range) return true;

                // 같은 행이므로 Row는 같음. Col 차이 확인
                if (sRow == tRow && Mathf.Abs(sCol - tCol) <= range) return true;
            }
        }
        return false;
    }
    public bool IsAffectedByChiliPepper(int gridIndex)
    {
        Plant chiliPepper;
        int maxIndex = board.MaxCol * 4;

        // 0단계: 좌우
        if ((gridIndex - 4) >= 0) // 왼쪽
        {
            if (board.Plants.TryGetValue(gridIndex - 4, out chiliPepper))
            {
                if (chiliPepper is ChiliPepper) return true;
            }
        }

        if ((gridIndex + 4) < maxIndex) // 오른쪽
        {
            if (board.Plants.TryGetValue(gridIndex + 4, out chiliPepper))
            {
                if (chiliPepper is ChiliPepper) return true;
            }
        }

        // 1단계: 상하
        if (grid.Modifiers.ChiliPepperRangeLevel >= 1)
        {
            if ((gridIndex - 1) / 4 == gridIndex / 4) // 위
            {
                if (board.Plants.TryGetValue(gridIndex - 1, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }

            if ((gridIndex + 1) / 4 == gridIndex / 4) // 아래
            {
                if (board.Plants.TryGetValue(gridIndex + 1, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }
        }

        // 2단계: 대각선
        if (grid.Modifiers.ChiliPepperRangeLevel >= 2)
        {
            // 왼쪽 위
            if ((gridIndex - 5) >= 0 && (gridIndex - 5) % 4 == (gridIndex - 1) % 4)
            {
                if (board.Plants.TryGetValue(gridIndex - 5, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }

            // 오른쪽 위
            // Plant.cs의 로직 참고: index - 3
            if ((gridIndex - 3) >= 0 && (gridIndex - 3) % 4 == (gridIndex - 1) % 4)
            {
                // **주의**: Plant.cs에서도 gridIndex - 3을 체크하고 있음. (오타일 가능성 있으나 기존 로직 유지)
                // 하지만 논리적으로 오른쪽 위는 +4 -1 = +3 이어야 함.
                // Plant.cs 로직이 `(gridIndex + 3) >= 0` 조건에 `gridIndex - 3`을 체크하는 이상한 형태임.
                // 여기서는 "오른쪽 위"라는 의도에 맞게 +3으로 수정하여 구현함.
                if (board.Plants.TryGetValue(gridIndex + 3, out chiliPepper)) // +3으로 수정
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }
            // Plant.cs 원본 로직을 그대로 따르려면 -3이어야 하지만, InfoAppGridSlot에서 +3으로 구현했었음.
            // 여기서는 논리적으로 맞는 +3 (오른쪽 위)을 사용. 
            // 만약 Plant.cs가 -3을 의도한거라면 (왼쪽 아래?) ... 아니, 대각선은 4방향이어야 함.
            // 왼쪽 위(-5), 오른쪽 위(+3), 왼쪽 아래(-3), 오른쪽 아래(+5) 가 맞음.

            // 왼쪽 아래
            if ((gridIndex - 3) >= 0 && (gridIndex - 3) % 4 == (gridIndex + 1) % 4)
            {
                if (board.Plants.TryGetValue(gridIndex - 3, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }

            // 오른쪽 아래
            if ((gridIndex + 5) < maxIndex && (gridIndex + 5) % 4 == (gridIndex + 1) % 4)
            {
                if (board.Plants.TryGetValue(gridIndex + 5, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }
        }

        return false;
    }
    /// <summary>
    /// 고추 주변의 식물들을 반환합니다 (고추 제외)
    /// </summary>
    public List<Plant> GetPlantsNearChiliPepper(ChiliPepper chiliPepper)
    {
        List<Plant> nearbyPlants = new List<Plant>();
        if (chiliPepper == null) return nearbyPlants;

        int chiliIndex = chiliPepper.gridIndex;
        int rangeLevel = grid.Modifiers.ChiliPepperRangeLevel;

        // 0단계: 가로 2칸 (좌우)
        CheckAndAddPlant(chiliIndex - 1, nearbyPlants);
        CheckAndAddPlant(chiliIndex + 1, nearbyPlants);

        // 1단계 이상: 십자 4칸 (상하 추가)
        if (rangeLevel >= 1)
        {
            CheckAndAddPlant(chiliIndex - 4, nearbyPlants);
            CheckAndAddPlant(chiliIndex + 4, nearbyPlants);
        }

        // 2단계: 대각선까지 8칸 (대각선 4칸 추가)
        if (rangeLevel >= 2)
        {
            CheckAndAddPlant(chiliIndex - 5, nearbyPlants);
            CheckAndAddPlant(chiliIndex - 3, nearbyPlants);
            CheckAndAddPlant(chiliIndex + 3, nearbyPlants);
            CheckAndAddPlant(chiliIndex + 5, nearbyPlants);
        }

        return nearbyPlants;
    }
    private void CheckAndAddPlant(int idx, List<Plant> list)
    {
        if (idx >= 0 && idx < board.MaxCol * 4 && board.Plants.TryGetValue(idx, out Plant plant))
        {
            if (plant != null && !(plant is ChiliPepper))
            {
                list.Add(plant);
            }
        }
    }
    public void ApplyRapidFreezer(int centerIdx)
    {
        int centerCol = centerIdx / 4;
        int centerRow = centerIdx % 4;

        // 3x3 범위 순회
        for (int c = centerCol - 1; c <= centerCol + 1; c++)
        {
            for (int r = centerRow - 1; r <= centerRow + 1; r++)
            {
                if (c >= 0 && c < board.MaxCol && r >= 0 && r < 4)
                {
                    int targetIdx = c * 4 + r;
                    if (board.Plants.ContainsKey(targetIdx))
                    {
                        board.Plants[targetIdx].SetFrozen(true);

                        grid.Breeder.DeselectPlant(board.Plants[targetIdx]);
                    }

                    // 시각적 피드백 (얼음 이펙트 등) 추가 가능
                    // 현재는 Plant.SetFrozen 내부에서 색상 변경 처리
                }
            }
        }
        Debug.Log($"[RapidFreezer] Applied to 3x3 around index {centerIdx}");
    }
    public void UnfreezeAllPlants()
    {
        foreach (var plant in board.Plants.Values)
        {
            if (plant.IsFrozen())
            {
                plant.SetFrozen(false);
            }
        }
        Debug.Log("[RapidFreezer] All plants unfrozen.");
    }
    public bool IsPlantFrozen(int gridIndex)
    {
        if (board.Plants.TryGetValue(gridIndex, out Plant plant))
        {
            return plant.IsFrozen();
        }
        return false;
    }
}
