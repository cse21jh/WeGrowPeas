using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using Unity.VisualScripting;

//using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{
    protected EnemyController enemyController;
    [SerializeField] protected EconomyManager economyManager;

    [SerializeField] protected GameObject peaPrefab;
    [SerializeField] protected GameObject peanutPrefab;
    [SerializeField] protected GameObject nepenthesPrefab;
    [SerializeField] protected GameObject chiliPepperPrefab;
    [SerializeField] protected GameObject moneyTreePrefab;
    [SerializeField] protected GameObject sprinklerPrefab;
    //[SerializeField] private GameObject soilPrefab;
    [SerializeField] protected GameObject[] disabledSoil; // 4개 이상의 열이 추가될 때 활성화되는 토양들
    [SerializeField] protected List<GameObject> bugPrefabs;
    [SerializeField] protected GameObject ladybugPrefabs;

    [SerializeField] protected Sprite[] gardenSprites; // 정원 배경 스프라이트들
    [SerializeField] protected SpriteRenderer gardenRenderer; // 정원 배경 스프라이트 렌더러

    [SerializeField] private GameObject petBottleMarkerPrefab;
    [SerializeField] private GameObject goldSoilMarkerPrefab;
    [SerializeField] private Material soilHighlightMaterial;

    private const float FertilizerResistBonus = 0.05f; // +5%p



    public bool isDraggingShovel = false;


    /// <summary>아이템과 능력이 쌓은 밭 전체 보정치. 저장과 불러오기는 <see cref="GridModifiers"/>가 맡는다.</summary>
    public GridModifiers Modifiers { get; } = new GridModifiers();

    /// <summary>저주가 밭에 주는 효과.</summary>
    public GridCurseActions CurseActions => curseActions ??= new GridCurseActions(this);
    private GridCurseActions curseActions;

    /// <summary>같은 오브젝트에 붙은 스카우터. 씬에서 연결하지 않아도 찾는다.</summary>
    public ScouterController Scouter => scouter != null ? scouter : (scouter = GetComponent<ScouterController>());
    private ScouterController scouter;

    public EnemyController Enemy => enemyController;

    /// <summary>벌레와 무당벌레 소환.</summary>
    public BugSpawner Bugs => bugs ??= new BugSpawner(Board, Modifiers, bugPrefabs, ladybugPrefabs);
    private BugSpawner bugs;

    /// <summary>비료, 황금 토양, 페트병처럼 땅에 놓이는 효과.</summary>
    public SoilEffects Soil => soil ??= new SoilEffects(this, petBottleMarkerPrefab, goldSoilMarkerPrefab);
    private SoilEffects soil;

    /// <summary>같은 오브젝트에 붙은 교배 담당. 튜토리얼은 이를 상속한 컴포넌트가 붙어 있다.</summary>
    public BreedingController Breeder => breeder != null ? breeder : (breeder = GetComponent<BreedingController>());
    private BreedingController breeder;

    public EconomyManager Economy => economyManager;

    // 교배 버튼의 OnClick이 Canvas 프리팹에 Grid 메서드로 저장돼 있어 이름을 남겨 둔다.
    public void ActivateBreed() => Breeder.ActivateBreed();
    public void SkipBreed() => Breeder.SkipBreed();

    /// <summary>식물 생성과 추가.</summary>
    public PlantSpawner Spawner => spawner ??= new PlantSpawner(Board, AddPlantToGrid, RefreshResistanceScouter, peaPrefab, peanutPrefab, nepenthesPrefab, chiliPepperPrefab, moneyTreePrefab, sprinklerPrefab);
    private PlantSpawner spawner;

    /// <summary>값을 올리면서 밭 위 식물과 가격 표시도 갱신하는 강화.</summary>
    public GridUpgrades Upgrades => upgrades ??= new GridUpgrades(this);
    private GridUpgrades upgrades;

    /// <summary>마우스가 올라간 칸의 강조 표시.</summary>
    public SoilHighlighter Highlighter => highlighter ??= new SoilHighlighter(GetSoilTransform, soilHighlightMaterial);
    private SoilHighlighter highlighter;

    /// <summary>칸과 식물 데이터. 다른 시스템은 Grid 대신 이 객체로 밭을 읽는다.</summary>
    public PlantBoard Board { get; } = new PlantBoard();

    //저장 필요
    public int killBugCount = 0;

    //일반 특성
    protected bool hasWeatherForecast = false;


    //일반 특성
    public bool HasWeatherForecast => hasWeatherForecast;


    // 그리드 상태 변경 이벤트 (식물 배치/제거/이동 등)


    // LoadGrid 중에는 Board.Changed 발생을 억제 (MoneyTree가 로드 순서에 따라 즉시 Die하는 버그 방지)
    private bool isLoadingGrid = false;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        enemyController = FindAnyObjectByType<EnemyController>();
        //InitGrid();
        InitSoils();
        Board.Plants.Clear();
        Breeder.Initialize();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        Soil.KeepPetMarkersVisible();

    }

    public void InitGrid()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = Spawner.InstantiatePlant(GameManager.Instance.currentPlant);
            Plant plant = obj.GetComponent<Plant>();
            List<GeneticTrait> basicTrait = new List<GeneticTrait> { new GeneticTrait(TraitType.NaturalDeath, Plant.GetResistanceBasedOnGenetics(TraitType.NaturalDeath, 1), 1, 0.0f), };
            //FenceUIManager.Instance.SetFenceElements(0, plant);
            plant.SetTrait(basicTrait);
            AddPlantToGrid(plant);
        }

    }

    // 직전 Breed()의 변종 결과. 생성된 식물에 이펙트를 재생하기 위해 호출부가 참조한다.
    // (0 = 변종 없음, 1 = 악성, 2 = 양성)

    private void RefreshResistanceScouter() => Scouter.UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);

    public void AddPlantToGrid(Plant plant, int grididx = -1) // 이미 오브젝트로 만들어진 식물 그리드에 추가. grididx에 숫자 삽입 시 해당 위치에 식물 심어줌
    {
        if (grididx != -1)
        {
            if (!Board.Plants.ContainsKey(grididx))
            {
                plant.Init(grididx, this);
                PlantPlant(plant);

                return;
            }
        }

        for (int idx = 0; idx < Board.MaxCol * 4; idx++)
        {
            if (!Board.Plants.ContainsKey(idx))
            {
                plant.Init(idx, this);
                //Debug.Log($"[Grid.AddPlant] Init called for {plant.name} at idx {idx}", plant);
                PlantPlant(plant);

                return;
            }
        }
        Destroy(plant.gameObject);
        return;
    }
    private void PlantPlant(Plant plant)
    {
        Board.Plants[plant.gridIndex] = plant;
        Transform soilT = GetSoilTransform(plant.gridIndex);
        plant.transform.position = soilT.position;

        // 저장 복원 중에는 기존 뿌리 상태를 아래에서 그대로 불러온다.
        // 새 식물은 실제 흙 위치로 옮긴 뒤 판정해야 가격 상승 팝업도 올바른 곳에서 시작한다.
        if (!isLoadingGrid && plant is MovablePlant movablePlant)
            movablePlant.TryRootAfterPlacement();

        Scouter.UpdateGoldScouterImageInGrid();

        // 그리드 상태 변경 알림 (로딩 중에는 억제 — 모든 식물 로드 후 일괄 발송)
        if (!isLoadingGrid)
            Board.NotifyChanged();
    }

    // 특수(땅부자): 세로줄별 고속 숙성 배수 보너스 (col → +mul, 중첩 가능)

    // 특수(땅부자) 세로줄 배수 저장/복원 (index = col)
    public Transform GetSoilTransform(int idx)
    {
        try
        {
            // InitSoils에서 index = col * 4 + row로 계산하므로
            // idx = col * 4 + row
            // col = idx / 4
            // row = idx % 4
            int col = idx / 4;
            int row = idx % 4;

            // 범위 체크
            if (col < 0 || col >= transform.childCount)
            {
                Debug.LogError($"[Grid] GetSoilTransform: Invalid col={col}, idx={idx}, childCount={transform.childCount}");
                return null;
            }

            Transform colT = transform.GetChild(col);
            if (colT == null)
            {
                Debug.LogError($"[Grid] GetSoilTransform: colT is null, col={col}");
                return null;
            }

            if (row < 0 || row >= colT.childCount)
            {
                Debug.LogError($"[Grid] GetSoilTransform: Invalid row={row}, col={col}, idx={idx}, colT.childCount={colT.childCount}");
                return null;
            }

            Transform rowT = colT.GetChild(row);
            return rowT;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Grid] GetSoilTransform exception: idx={idx}, error={e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    /*public void DestroyPlant(int gridNum)
    {
        Plant plant = Board.Plants[gridNum];
        plant.Die();
        Board.Plants.Remove(gridNum);
        return;
    }*/

    public void ClearGridIndex(int gridIndex)
    {
        if (Board.Plants.ContainsKey(gridIndex))
        {
            Board.Plants.Remove(gridIndex);
            Board.NotifyChanged();
        }

        if (CheckGameOver())
        {
            StartCoroutine(GameManager.Instance.GameOver());
        }
    }

    public virtual bool CheckGameOver()
    {
        Plant plant;
        for (int idx = 0; idx < Board.MaxCol * 4; idx++)
        {
            plant = null;
            Board.Plants.TryGetValue(idx, out plant);
            if (plant == null)
                continue;
            if (plant.GetType() == typeof(Pea) || plant.GetType() == typeof(Peanut))
                return false;
        }
        return true;
    }

    public void AddSoil()
    {
        Board.MaxCol += 1;
        //GameObject obj = Instantiate(soilPrefab, this.transform);
        //obj.transform.localPosition = new Vector3(1.7f * (Board.MaxCol-1), 0f, 0f);

        gardenRenderer.sprite = gardenSprites[Board.MaxCol - 4]; // 정원 배경 스프라이트 변경

        disabledSoil[Board.MaxCol - 5].SetActive(true);

        //for (int row = 0; row < 4; row++)
        //{
        //    Transform soilT = disabledSoil[Board.MaxCol - 5].transform.GetChild(row);
        //    Soil soil = soilT.GetComponent<Soil>();

        //    if (soil != null)
        //    {
        //        int index = row + (Board.MaxCol - 1) * 4;
        //        soil.Init(index);
        //    }
        //}
    }
    public void UpdateSoil()
    {
        if (Board.MaxCol <= 4)
            return;
        for (int i = 5; i <= Board.MaxCol; i++)
        {
            gardenRenderer.sprite = gardenSprites[i - 4]; // 정원 배경 스프라이트 변경
            disabledSoil[i - 5].SetActive(true);
        }
    }
    private void InitSoils()
    {
        for (int col = 0; col < 8; col++)
        {
            Transform soilColT = transform.GetChild(col);
            for (int row = 0; row < 4; row++)
            {
                Transform soilT = soilColT.GetChild(row);
                Soil soil = soilT.GetComponent<Soil>();
                if (soil != null)
                {
                    int index = col * 4 + row;
                    soil.Init(index);
                }
            }
        }
    }
    // ───── 저주 액션 (CurseInstance.Activate에서 호출) ─────

    // 마우스 포인터가 교배 버튼 영역 위에 있는지 검사.
    // 식물의 OnMouseUp(콜라이더 이벤트)은 UI를 존중하지 않으므로, 교배 버튼이
    // 선택된 식물 위에 겹쳐 있을 때 식물 클릭이 선택을 취소하는 것을 막기 위해 직접 검사한다.

    public bool TryPlacePlant(Plant plant, Vector3 screenPosition)
    {
        Highlighter.ClearHoverHighlight();
        int? targetIndex = GetGridIndexFromPosition(screenPosition);

        // 토양 감지 실패
        if (!targetIndex.HasValue || plant.isDying)
        {
            // 원래 위치로 되돌리기
            Transform originalSoil = GetSoilTransform(plant.gridIndex);
            plant.transform.position = originalSoil.position;
            return false;
        }

        int fromIndex = plant.gridIndex;
        int toIndex = targetIndex.Value;

        if (Board.Plants.ContainsKey(toIndex))
        {
            if (!Board.Plants[toIndex].CanMove()) // 옮기기 불가능한 식물의 경우
            {
                Transform originalSoil = GetSoilTransform(plant.gridIndex);
                plant.transform.position = originalSoil.position;
                return false;
            }
            // 대상 칸에 식물이 있는 경우: 서로 위치 교환
            Plant targetPlant = Board.Plants[toIndex];
            Dictionary<Plant, int> previousPrices = Upgrades.CapturePlantSellingPrices();

            // 서로 gridIndex 바꾸기
            plant.SetGridIndex(toIndex);
            targetPlant.SetGridIndex(fromIndex);

            // 위치 바꾸기
            Transform fromSoil = GetSoilTransform(fromIndex);
            Transform toSoil = GetSoilTransform(toIndex);
            plant.transform.position = toSoil.position;
            targetPlant.transform.position = fromSoil.position;

            // Board.Plants 업데이트
            Board.Plants[toIndex] = plant;
            Board.Plants[fromIndex] = targetPlant;

            //스카우터 확인 (고추, 비료 때문)
            if (plant is MovablePlant p1)
            {
                p1.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }
            if (targetPlant is MovablePlant p2)
            {
                p2.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }

            Board.NotifyChanged();
            Upgrades.RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.Moved);
            if (fromIndex != toIndex) GameEvents.RaisePlantMoved();
            return true;
        }
        else
        {
            Dictionary<Plant, int> previousPrices = Upgrades.CapturePlantSellingPrices();
            // 빈 칸이면 원래대로 심기
            Board.Plants.Remove(fromIndex); // 원래 위치에서 제거
            plant.SetGridIndex(toIndex);
            plant.transform.position = GetSoilTransform(toIndex).position;
            Board.Plants[toIndex] = plant;

            if (plant is MovablePlant p1)
            {
                p1.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }
            Board.NotifyChanged();
            Upgrades.RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.Moved);
            if (fromIndex != toIndex) GameEvents.RaisePlantMoved();
            return true;
        }
    }

    private RaycastHit2D[] raycastHits = new RaycastHit2D[10]; // GC 할당 방지용 캐싱 배열

    public int? GetGridIndexFromPosition(Vector3 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

        // RaycastAll 대신 NonAlloc을 사용하여 매 프레임 발생하는 배열 생성(Garbage) 방지
        int hitCount = Physics2D.RaycastNonAlloc(worldPos2D, Vector2.zero, raycastHits);

        for (int i = 0; i < hitCount; i++)
        {
            if (raycastHits[i].collider != null)
            {
                // 게임 로직적으로 Soil 자체에 있는 콜라이더를 정확히 감지하도록 원상복구
                Soil soil = raycastHits[i].collider.GetComponent<Soil>();
                if (soil != null)
                {
                    return soil.GridIndex;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 현재 밭 상태와 누적 보정치를 저장 데이터에 담는다. <see cref="LoadGrid"/>와 짝이므로
    /// 한쪽에 항목을 추가하면 반드시 다른 쪽도 같이 고칠 것.
    /// </summary>
    public void SaveGrid(GridSave save)
    {
        // 밭 / 교배
        save.plantList.Clear();
        foreach (var p in Board.Plants.Values)
        {
            save.plantList.Add(new PlantData
            {
                speciesname = p.speciesname,
                traits = p.GetGeneticTrait(),
                gridIndex = p.gridIndex,
                taste = p.GetTaste(),
                resistWaveCount = p.GetResistWaveCount(),
                survivedTurns = (p is MoneyTree mt) ? mt.GetSurvivedTurns() : 0,
                travelSellBonus = p.GetTravelSellBonus(),
                freeTimePassedCount = p.GetFreeTimePassedCount(),
                hasTriedBreed = p.HasTriedBreed,
                isRooted = (p is MovablePlant movable) && !movable.IsMovable
            });
        }
        Modifiers.Save(save);
        Breeder.Save(save);
        save.maxCol = Board.MaxCol;

        save.mostExpensivePlant = Upgrades.MostExpensivePlant;

        // 벌레 / 무당벌레 / 네펜데스
        save.killBugCount = killBugCount;
        Bugs.Save(save);

        // 유전자 보너스

        // 식물 일반 특성

        // 완두콩 특성

        // 땅콩 특성

        // 일반 특성 (정보 표시)
        save.hasResistanceScouter = Scouter.HasResistanceScouter;
        save.hasGoldScouter = Scouter.HasGoldScouter;
        save.hasWeatherForecast = hasWeatherForecast;

        // 페트병
        save.perBottleTiles = new List<int>(Soil.PetBottleTiles);
        save.petBottleBlockCountBonus = Soil.GetPetBottleBlockCountBonus();

        // 고추

        // 타일 / 비료
        save.goldSoilTiles = new List<int>(Soil.GoldSoilTiles);
        save.fertilizerColumns.Clear();
        save.fertilizerType.Clear();
        foreach (KeyValuePair<int, WaveType> fer in Soil.GetFertilizerColumns())
        {
            save.fertilizerColumns.Add(fer.Key);
            save.fertilizerType.Add(fer.Value);
        }
        save.absorbFertilizerTiles = new List<int>(Soil.AbsorbFertilizerTiles);

        // 신규 아이템 스탯

        save.columnGoldMulBonusList = Soil.GetColumnGoldMulBonusForSave();
    }

    public void LoadGrid(GridSave saveData)
    {
        // 로딩 중 Board.Changed 억제 (MoneyTree 등이 로드 순서에 따라 조기 사망하는 것 방지)
        isLoadingGrid = true;

        List<PlantData> plantList = saveData.plantList;
        foreach (var item in plantList)
        {
            GameObject obj = Spawner.InstantiatePlant(item.speciesname);

            Plant plant = obj.GetComponent<Plant>();
            plant.Init(item.gridIndex, this);
            plant.SetTrait(item.traits);
            plant.SetTaste(item.taste);
            plant.SetResistWaveCount(item.resistWaveCount);
            plant.SetTravelSellBonus(item.travelSellBonus); // 특수(세계여행) 복원
            plant.SetFreeTimePassedCount(item.freeTimePassedCount); // 완두커피 복원
            plant.SetHasTriedBreed(item.hasTriedBreed); // 활성형 껍질 복원

            // 뿌리 상태 복원. 로드 중에는 새 뿌리 판정을 하지 않고 저장값을 그대로 사용한다.
            if (plant is MovablePlant movablePlant)
                movablePlant.SetMovable(!item.isRooted);

            // MoneyTree 생존 턴 수 복원
            if (plant is MoneyTree moneyTree)
                moneyTree.SetSurvivedTurns(item.survivedTurns);

            PlantPlant(plant);
        }

        // 모든 식물 로드 완료 후 Board.Changed 한 번 발송
        isLoadingGrid = false;
        Board.NotifyChanged();
        Board.MaxCol = saveData.maxCol;
        UpdateSoil();
        Soil.LoadColumnGoldMulBonus(saveData.columnGoldMulBonusList); // 특수(땅부자) 세로줄 배수 복원
        killBugCount = saveData.killBugCount;

        Bugs.Load(saveData);

        Modifiers.Load(saveData);

        Scouter.SetResistanceScouter(saveData.hasResistanceScouter); // 일반 특성
        Scouter.SetGoldScouter(saveData.hasGoldScouter);
        SetWeatherForecast(saveData.hasWeatherForecast);

        // 신규 아이템 스탯 로드

        // 신규 아이템 스탯 로드

        Soil.AbsorbFertilizerTiles.Clear();
        if (saveData.absorbFertilizerTiles != null)
        {
            foreach (int idx in saveData.absorbFertilizerTiles)
            {
                Soil.AbsorbFertilizerTiles.Add(idx);
            }
        }

        // 네펜데스 페로몬 업데이트
        foreach (var plant in Board.Plants.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromone();
                nepenthes.UpdatePheromoneSize();
            }
        }

        Breeder.Load(saveData);

        // 페트병 관련 변수 로드
        if (saveData.petBottleBlockCountBonus > 0)
            Soil.SetPetBottleBlockCountBonus(saveData.petBottleBlockCountBonus);

        // 고추 관련 변수 로드

        // 황금 비료 로드
        foreach (var i in saveData.goldSoilTiles)
            Soil.TryPlaceGoldSoil(i);

        foreach (var i in saveData.perBottleTiles)
            Soil.PlacePetBottle(i);
        for (int i = 0; i < saveData.fertilizerColumns.Count; i++)
            Soil.TryPlaceFertilizer(saveData.fertilizerColumns[i] * 4, saveData.fertilizerType[i]);

        Upgrades.RefreshAllPlantSellingPriceDisplays();
        Upgrades.LoadMostExpensivePlant(saveData.mostExpensivePlant);
    }

    // 급속 냉각기 관련
    // === 저항력 흡수 비료 ===
    // ───── 신규 아이템 ─────

    public void SetWeatherForecast(bool val)
    {
        hasWeatherForecast = val;
    }

    public bool GetHasWeatherForecast()
    {
        return hasWeatherForecast;
    }

    //-----전용 비료------

    private TraitType MapWaveToTrait(WaveType w)
    {
        return w switch
        {
            WaveType.Aging => TraitType.NaturalDeath,
            WaveType.Wind => TraitType.Wind,
            WaveType.Flood => TraitType.Flood,
            WaveType.Pest => TraitType.Pest,
            WaveType.Cold => TraitType.Cold,
            WaveType.HeavyRain => TraitType.HeavyRain,
            _ => TraitType.NaturalDeath
        };
    }

}

