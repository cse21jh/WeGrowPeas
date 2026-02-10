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

    protected bool isBreeding = false;

    protected GameObject breedObj1 = null;
    protected GameObject breedObj2 = null;
    protected bool isBreedButtonPressed = false;

    protected bool isBreedSkipButtonPressed = false;


    protected float breedTimer;

    [SerializeField] protected GameObject peaPrefab;
    [SerializeField] protected GameObject peanutPrefab;
    [SerializeField] protected GameObject nepenthesPrefab;
    [SerializeField] protected GameObject chiliPepperPrefab;
    //[SerializeField] private GameObject soilPrefab;
    [SerializeField] protected GameObject[] disabledSoil; // 4개 이상의 열이 추가될 때 활성화되는 토양들
    [SerializeField] protected List<GameObject> bugPrefabs;
    [SerializeField] protected GameObject ladybugPrefabs;

    [SerializeField] protected TimerUI breedTimerUI;
    [SerializeField] protected GameObject breedButton;
    [SerializeField] protected GameObject breedSkipButton;
    [SerializeField] protected TextMeshProUGUI breedCountUI;

    [SerializeField] protected Sprite[] gardenSprites; // 정원 배경 스프라이트들
    [SerializeField] protected SpriteRenderer gardenRenderer; // 정원 배경 스프라이트 렌더러

    [SerializeField] private GameObject petBottleMarkerPrefab;
    [SerializeField] private GameObject goldSoilMarkerPrefab;
    
    private Dictionary<int, GameObject> petMarkers = new Dictionary<int, GameObject>();
    protected List<int> goldSoilTiles = new List<int>(); // 황금 비료가 뿌려진 타일들
    private Dictionary<int, GameObject> goldSoilMarkers = new Dictionary<int, GameObject>(); // 황금 비료 마커들

    private const float FertilizerResistBonus = 0.05f; // +5%p

    public bool showingAllPrice = false;

    public List<Ladybug> ladybugs = new List<Ladybug>();

    public bool isDraggingShovel = false;

    private Plant breedButtonPlant = null;

    //저장 필요
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    [HideInInspector] public int maxCol = 4;
    public int killBugCount = 0;
    public int totalBreedCount = 0;
    public int totalPeaBreedcount = 0;
    public int totalPeanutBreedCount = 0;

    protected float bugSpawnTimeInterval = 15.0f;
    protected float lastBugSpawnTimeInterval = 0f;

    protected float bugSpeedDecreasement = 0f;
    protected float bugSpawnIntervalIncreasement = 0f;
    protected float ladybugSpawnProbability = 0f;
    protected int maxLadybugCount = 0; // 최대 무당벌레 수 (0 = 무제한)
    protected int additionalLadybugGoldPerUnit = 0; // 무당벌레당 골드 (웨이브 종료 시)
    protected float additionalLadybugResistancePerUnit = 0f; // 무당벌레당 저항력 증가
    protected int additionalBugGold = 0;

    protected int additionalNepenthesGold = 0; // 네펜데스가 벌레 먹을 때 추가 골드
    protected bool hasNepenthesPheromone = false; // 네펜데스 페로몬 활성화 여부
    protected float additionalNepenthesPheromoneSizeMultiplier = 0f; // 네펜데스 페로몬 범위 증가 배수 (합적용, 0 = 기본값, 0.2 = +20%)
    protected float nepenthesSpawnProbability = 0f; // 네펜데스 등장 확률 (rotation weight 증가로 구현)
    
    protected float weakGeneticsResistanceBonus = 0f; // 약한 유전자(열성이 아닌 형질, 최초 저항력 80%가 아닌 형질) 저항력 증가 보너스
    protected float strongGeneticsResistanceBonus = 0f; // 강한 유전자(우성, 최초 저항력 80%인 형질) 저항력 증가 보너스
    protected float goldenGeneticsProbabilityBonus = 0f; // 황금 유전자 확률 보너스 (genetics = 1에서 우수한 형질이 나올 확률 증가)

    
    protected float additionalPlantGoldMultiplier = 0.2f; // 기본값 0.2f (웨이브 저항 횟수당 골드 배수)

    protected float additionalPestResistance = 0f;

    //식물 공통 특성
    protected float resistanceBonus = 0f; // 기본 저항력 보너스
    protected int additionalPlantGold = 0; // 기본 식물 가격 보너스

    //완두콩 특성
    protected float resistanceDecayReduction = 0f; // 웨이브를 버텼을 때, 감소하는 저항력의 감소량
    protected float resistanceAdaptation = 0f; // 웨이브를 버텼을 때, 해당 웨이브의 저항력 증가량

    // 땅콩 특성
    protected float additionalPeanutCopyProbability = 0f; // 땅콩 복사 확률 증가
    protected float bonusRatioWhenDie = 0f; // 죽었을 때, 추가 골드

    //일반 특성
    protected bool hasResistanceScouter = false;
    protected bool hasGoldScouter = false;
    protected bool hasWeatherForecast = false;

    protected int additionalInheritance = 0;
    protected float maxBreedTimer = 40.0f;
    protected int maxBreedCount = 4;
    protected int breedCount = 0;

    protected bool hasCoolingShield = false; // 냉각 방패 보유 여부
    protected bool isCoolingShieldActivated = false; // 냉각 방패 활성화 여부 (1회 사용)
    protected List<int> petBottleTiles = new List<int>();
    protected Dictionary<int, int> petBottleBlockCount = new Dictionary<int, int>(); // 페트병 위치별 보호 횟수 (기본값 1, 재질 강화 시 증가)
    protected int petBottleInitialStockBonus = 0; // 페트병 일일 최대 구매 횟수 보너스
    protected int petBottlePriceReduction = 0; // 페트병 가격 감소량
    protected float petBottleSpawnProbability = 0f; // 페트병 등장 확률 (rotation weight 증가로 구현)

    protected int chiliPepperRangeLevel = 0; // 고추 영향 범위 레벨 (0: 가로 2칸, 1: 십자 4칸, 2: 대각선까지 8칸)
    protected float chiliPepperSpawnProbability = 0f; // 고추 등장 확률 (rotation weight 증가로 구현)
    protected float chiliPepperHealPercent = 0f; // 치료형 캡사이신 회복 퍼센트 (구매 횟수 * 3%)

    private readonly Dictionary<int, WaveType> fertilizerColumns = new();

    public List<int> GoldSoilTiles => goldSoilTiles;

    public float BugSpawnTimeInterval => bugSpawnTimeInterval;
    public float LastBugSpawnTimeInterval => lastBugSpawnTimeInterval;
    public float BugSpeedDecreasement => bugSpeedDecreasement;
    public float BugSpawnIntervalIncreasement => bugSpawnIntervalIncreasement;
    public float LadybugSpawnProbability => ladybugSpawnProbability;
    public int MaxLadybugCount => maxLadybugCount;
    public int AdditionalLadybugGoldPerUnit => additionalLadybugGoldPerUnit;
    public float AdditionalLadybugResistancePerUnit => additionalLadybugResistancePerUnit;
    public int AdditionalBugGold => additionalBugGold;
    public int AdditionalNepenthesGold => additionalNepenthesGold;
    public bool HasNepenthesPheromone => hasNepenthesPheromone;
    public float AdditionalNepenthesPheromoneSizeMultiplier => additionalNepenthesPheromoneSizeMultiplier;
    public float NepenthesSpawnProbability => nepenthesSpawnProbability;
    public float WeakGeneticsResistanceBonus => weakGeneticsResistanceBonus;
    public float StrongGeneticsResistanceBonus => strongGeneticsResistanceBonus;
    public float GoldenGeneticsProbabilityBonus => goldenGeneticsProbabilityBonus;    

    //식물 공통 특성
    public float ResistanceBonus => resistanceBonus;
    public int AdditionalPlantGold => additionalPlantGold;

    //완두콩 특성
    public float ResistanceDecayReduction => resistanceDecayReduction;
    public float ResistanceAdaptation => resistanceAdaptation;
    //땅콩 특성
    public float AdditionalPeanutCopyProbability => additionalPeanutCopyProbability;
    public float BonusRatioWhenDie =>  bonusRatioWhenDie;

    //일반 특성
    public bool HasResistanceScouter => hasResistanceScouter;
    public bool HasGoldScouter => hasGoldScouter;
    public bool HasWeatherForecast => hasWeatherForecast;

    public float AdditionalPlantGoldMultiplier => additionalPlantGoldMultiplier;
    public float AdditionalPestResistance => additionalPestResistance;
    public int AdditionalInheritance => additionalInheritance;
    public float MaxBreedTimer => maxBreedTimer;
    public int MaxBreedCount => maxBreedCount;
    public int BreedCount => breedCount; // 스테이지 단위 저장이라 아직 ㄱㅊ
    public bool HasCoolingShield => hasCoolingShield;
    public List<int> PetBottleTiles => petBottleTiles;
    public int PetBottleInitialStockBonus => petBottleInitialStockBonus;
    public int PetBottlePriceReduction => petBottlePriceReduction;
    public float PetBottleSpawnProbability => petBottleSpawnProbability;
    public int GetPetBottleBlockCountBonus() => petBottleBlockCount.ContainsKey(-1) ? petBottleBlockCount[-1] : 0;
    public int ChiliPepperRangeLevel => chiliPepperRangeLevel;
    public float ChiliPepperSpawnProbability => chiliPepperSpawnProbability;
    public float ChiliPepperHealPercent => chiliPepperHealPercent;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        enemyController = GameObject.Find("EnemyController").GetComponent<EnemyController>();
        //InitGrid();
        InitSoils();
        plantGrid.Clear();
        breedButton.SetActive(false);
        breedButtonPlant = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (breedButtonPlant != null)
        {
            MoveBreedButton(breedButtonPlant.transform.position);
        }

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

    public void InitGrid()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = InstantiatePlant(GameManager.Instance.currentPlant);
            Plant plant = obj.GetComponent<Plant>();
            List<GeneticTrait> basicTrait = new List<GeneticTrait>
        {
            new GeneticTrait(TraitType.NaturalDeath, Plant.GetResistanceBasedOnGenetics(TraitType.NaturalDeath, 1), 1, 0.0f),
        };
            //FenceUIManager.Instance.SetFenceElements(0, plant);
            plant.SetTrait(basicTrait);
            AddPlantToGrid(plant);
        }
    }

    public IEnumerator Breeding()
    {
        //breedTimer 만큼 동안 아래 과정 반복 진행 가능
        enemyController.SetCurrentWaveTimer();
        //교배할 부모 완두콩 두 개 선택
        isBreeding = true;
        breedObj1 = null;
        breedObj2 = null;

        //int breedCount = 0;
        int effectiveMaxBreedCount = Mathf.Max(1, Mathf.FloorToInt(maxBreedCount * ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1)));
        float effectiveBugSpawnTimeInterval = BugSpawnTimeInterval * ModManager.Instance.GetMul(StatId.BugSpawnIntervalMul, -1);
        float effectiveMaxBreedTimer = MaxBreedTimer * ModManager.Instance.GetMul(StatId.BreedingPhaseDurationMul, -1);

        //임시 알람
        bool _warned15s = false;

        //Debug.Log(effectiveMaxBreedTimer + "초 시작. 최대 교배 횟수는 " + effectiveMaxBreedCount + "입니다");
        UpdateBreedCountUI(effectiveMaxBreedCount);
        breedTimer = effectiveMaxBreedTimer;
        breedTimerUI.StartBreedingTimer();

        breedSkipButton.SetActive(true);
        enemyController.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;

        if (GameManager.Instance.stage == 6)
            lastBugSpawnTimeInterval = 0f;

        while (breedTimer > 0 && !isBreedSkipButtonPressed)
        {
            if(GameManager.Instance.GetGameIsStopped())
            {
                yield return null;
                continue;
            }

            lastBugSpawnTimeInterval += Time.deltaTime;
            breedTimer -= Time.deltaTime;

            if (breedTimer < 15f && !_warned15s)
            {
                _warned15s = true;

                PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = "웨이브가 얼마 남지 않았습니다",
                        message = "조심하세요.",
                        duration = 5f
                    }
                );
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                SkipBreed();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                enemyController.WaveSkip();
            }

            if (lastBugSpawnTimeInterval > effectiveBugSpawnTimeInterval * (1f + bugSpawnIntervalIncreasement))
            {
                List<int> targetIdx = new List<int>(plantGrid.Keys);
                if (targetIdx.Count > 0)
                {
                    SpawnRandomBug();
                    lastBugSpawnTimeInterval = 0f;
                }
            }

            if (isBreedButtonPressed || Input.GetKeyDown(KeyCode.Space))
            {
                if (breedObj1 != null && breedObj2 != null) // 교배 버튼 등으로 추후 수정
                {
                    Plant parent1 = breedObj1.GetComponent<Plant>();
                    Plant parent2 = breedObj2.GetComponent<Plant>();
                    //자식 완두콩 형질 계산 후 Instantiate

                    // 교배 횟수 증가 아이템이 구매되었을 때 바로 적용되도록 매번 재계산
                    int currentEffectiveMaxBreedCount = Mathf.Max(1, Mathf.FloorToInt(maxBreedCount * ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1)));

                    bool canBreed = false;
                    for (int idx = 0; idx < maxCol * 4; idx++) // 빈 칸이 있는가
                    {
                        if (!plantGrid.ContainsKey(idx))
                        {
                            canBreed = true;
                            break;
                        }
                    }

                    bool isEqualPlant = false;
                    if ((parent1.GetType() == parent2.GetType())) // 추후 아종 교배가 생긴다면 이곳과 교배 로직 수정을...
                    {
                        isEqualPlant = true;
                    }


                    if (canBreed && breedCount < currentEffectiveMaxBreedCount && isEqualPlant)
                    {                        
                        AddMovablePlant(Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait()));
                        breedCount++;
                        GameEvents.RaisePeaBreeded();
                        Debug.Log("자식 생성 성공. 남은 교배 횟수는 " + (currentEffectiveMaxBreedCount - breedCount) + "입니다");
                        SoundManager.Instance.PlayEffect("Breed");
                        totalBreedCount++;
                        if (GameManager.Instance.currentPlant == "완두콩")
                            totalPeaBreedcount++;
                        else if (GameManager.Instance.currentPlant == "땅콩")
                            totalPeanutBreedCount++;
                        UpdateBreedCountUI(currentEffectiveMaxBreedCount - breedCount);
                        Plant p1 = breedObj1.GetComponent<Plant>();
                        Plant p2 = breedObj2.GetComponent<Plant>();
                        p1.MakeDefaultSprite();
                        p2.MakeDefaultSprite();
                        breedObj1 = null;
                        breedObj2 = null;
                        DeactivateBreed();
                    }
                    else if (breedCount >= currentEffectiveMaxBreedCount)
                    {
                        Debug.Log("최대 교배 횟수 초과");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }
                    else if (isEqualPlant)
                    {
                        Debug.Log("두 종이 일치하지 않습니다");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }
                    else
                    {
                        Debug.Log("키울 공간이 부족합니다");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }


                }
                else
                {
                    Debug.Log("아직 두 콩을 모두 선택하지 않았습니다");
                    isBreedButtonPressed = false;
                }
            }
            else
            {
                isBreedButtonPressed = false; // 버그로 인해 Breed 버튼이 활성화된 상태에서 버튼 먼저 누르면 교배가 바로 되던 현상 수정
            }

            yield return null;
        }

        if (breedObj1 != null) breedObj1.GetComponent<Plant>().MakeDefaultSprite();
        if (breedObj2 != null) breedObj2.GetComponent<Plant>().MakeDefaultSprite();

        breedTimerUI.StopTimer();
        breedCount = 0;
        // Debug.Log("교배 페이즈 종료");
        breedButton.SetActive(false);
        breedButtonPlant = null;
        enemyController.HideWaveSkipButton();
        isBreeding = false;
        breedSkipButton.SetActive(false);
        //GardenGrid 리로드

        yield return null;
    }

    protected List<GeneticTrait> Breed(List<GeneticTrait> parent1, List<GeneticTrait> parent2)
    {
        List<GeneticTrait> childTrait = new List<GeneticTrait>();

        foreach (TraitType trait in System.Enum.GetValues(typeof(TraitType)))
        {
            if (trait == TraitType.None || trait == TraitType.Drought || trait == TraitType.Heat)
                break; // 가뭄, 더위에 대한 형질은 SetTrait에서 삽입해줌.따로따로 교배 로직을 거치면 이상하게 나옴. 

            int p1Trait;
            int p2Trait;

            int childGenetic = 0;

            int traitNotInParent = 0;

            if (parent1.Any(t => t.traitType == trait))
            {
                p1Trait = parent1.First(t => t.traitType == trait).genetics;
            }
            else
            {
                p1Trait = 0;
                traitNotInParent += 1;
            }

            if (parent2.Any(t => t.traitType == trait))
            {
                p2Trait = parent2.First(t => t.traitType == trait).genetics;
            }
            else
            {
                p2Trait = 0;
                traitNotInParent += 1;
            }

            if (traitNotInParent == 2)
                continue;

            switch (p1Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: 
                    // 황금 유전자 확률 보너스 적용: genetics = 1에서 우수한 형질이 나올 확률 증가
                    int p1Bonus = (int)(goldenGeneticsProbabilityBonus * 100); // 보너스를 퍼센트로 변환
                    childGenetic += (additionalInheritance + 50 + p1Bonus <= Random.Range(1, 101) ? 0 : 1); 
                    break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: 
                    // 황금 유전자 확률 보너스 적용: genetics = 1에서 우수한 형질이 나올 확률 증가
                    int p2Bonus = (int)(goldenGeneticsProbabilityBonus * 100); // 보너스를 퍼센트로 변환
                    childGenetic += (additionalInheritance + 50 + p2Bonus <= Random.Range(1, 101) ? 0 : 1); 
                    break;
                default: break;
            }

            float resistance = Plant.GetResistanceBasedOnGenetics(trait, childGenetic);


            if (trait == TraitType.Pest)
                childTrait.Add(new GeneticTrait(trait, resistance, childGenetic, GetAdditionalPestResistance()));
            else
                childTrait.Add(new GeneticTrait(trait, resistance, childGenetic, 0.0f));
        }
        return childTrait;
    }

    protected void AddPlantToGrid(Plant plant, int grididx = -1) // 이미 오브젝트로 만들어진 식물 그리드에 추가. grididx에 숫자 삽입 시 해당 위치에 식물 심어줌
    {
        if (grididx != -1)
        {
            if (!plantGrid.ContainsKey(grididx))
            {
                plant.Init(grididx, this);
                PlantPlant(plant);

                return;
            }
        }

        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            if (!plantGrid.ContainsKey(idx))
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
        plantGrid[plant.gridIndex] = plant;
        UpdateGoldScouterImageInGrid();
        enemyController.UpdateCurrentWaveAlarm();
        Transform soilT = GetSoilTransform(plant.gridIndex);
        plant.transform.position = soilT.position;
    }

    public void AddMovablePlant(List<GeneticTrait> trait, int grididx = -1)
    {
        GameObject obj = InstantiatePlant(GameManager.Instance.currentPlant);
        Plant plant = obj.GetComponent<Plant>();
        plant.SetTrait(trait);        
        AddPlantToGrid(plant, grididx);
    }

    public GameObject InstantiatePlant(string plantName)
    {
        switch (plantName)
        {
            case "완두콩": return Instantiate(peaPrefab);
            case "땅콩": return Instantiate(peanutPrefab);
            case "네펜데스": return Instantiate(nepenthesPrefab);
            case "고추": return Instantiate(chiliPepperPrefab);
            default: return Instantiate(peaPrefab);
        }
    }

    public void AddNepenthes(int idx)
    {
        GameObject obj = Instantiate(nepenthesPrefab);
        Nepenthes nepenthes = obj.GetComponent<Nepenthes>();
        AddPlantToGrid(nepenthes, idx);
    }

    public void AddChiliPepper(int idx)
    {
        GameObject obj = Instantiate(chiliPepperPrefab);
        ChiliPepper chiliPepper = obj.GetComponent<ChiliPepper>();
        AddPlantToGrid(chiliPepper, idx);
        UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
    }

    public float GetResistanceBonus()
    {
        return resistanceBonus;
    }
    public float GetWeakGenericsResistanceBonus()
    {
        return weakGeneticsResistanceBonus;
    }

    public float GetStrongGenericsResistanceBonus()
    {
        return strongGeneticsResistanceBonus;
    }

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
        Plant plant = plantGrid[gridNum];
        plant.Die();
        plantGrid.Remove(gridNum);
        return;
    }*/

    public void ClearGridIndex(int gridIndex)
    {
        if (plantGrid.ContainsKey(gridIndex)) plantGrid.Remove(gridIndex);

        if (CheckGameOver())
        {
            StartCoroutine(GameManager.Instance.GameOver());
        }
    }

    public virtual bool CheckGameOver()
    {
        Plant plant;
        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            plant = null;
            plantGrid.TryGetValue(idx, out plant);
            if (plant == null)
                continue;
            if (plant.GetType() == typeof(Pea) || plant.GetType() == typeof(Peanut))
                return false;
        }
        return true;
    }

    public void AddMaxBreedTimer(int time)
    {
        maxBreedTimer += time;
        //breedTimerUI.UpdateMaxTimerCount();
        
        // 교배 중이면 현재 타이머도 증가
        if (isBreeding && breedTimer > 0)
        {
            breedTimer += time;
            // 타이머가 최대값을 넘지 않도록 제한
            float effectiveMaxBreedTimer = GetMaxBreedTimer();
            if (breedTimer > effectiveMaxBreedTimer)
            {
                breedTimer = effectiveMaxBreedTimer;
            }
        }
    }

    public float GetMaxBreedTimer()
    {
        float effectiveMaxBreedTimer = MaxBreedTimer * ModManager.Instance.GetMul(StatId.BreedingPhaseDurationMul, -1);
        return effectiveMaxBreedTimer;
    }

    public float GetBreedTimer()
    {
        return breedTimer;
    }

    public void AddMaxBreedCount(int count)
    {
        maxBreedCount += count;
        
        // 교배 중이면 현재 교배 횟수도 증가 (남은 횟수 증가)
        if (isBreeding)
        {
            // breedCount는 이미 사용한 횟수이므로 변경하지 않음
            // maxBreedCount가 증가하면 자동으로 남은 횟수가 증가함
            // UI 업데이트
            int effectiveMaxBreedCount = Mathf.Max(1, Mathf.FloorToInt(maxBreedCount * ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1)));
            UpdateBreedCountUI(effectiveMaxBreedCount - breedCount);
        }
    }

    public int GetMaxCol()
    {
        return maxCol;
    }

    public void AddAdditionalResistanceInGrid(TraitType traitType, float value, bool byUpgrade = false)
    {
        for (int idx = 0; idx < GetMaxCol() * 4; idx++) // grid에 있는 식물들 저항력 증가
        {
            if (plantGrid.ContainsKey(idx))
            {
                Plant plant = plantGrid[idx];
                plant.AddAdditionalResistance(traitType, value, byUpgrade);
            }
        }
        return;
    }

    public void AddAdditionalPestResistance(float value)
    {
        additionalPestResistance += value;
        if (additionalPestResistance > 0.15f)
            additionalPestResistance = 0.15f;
        AddAdditionalResistanceInGrid(TraitType.Pest, value);
    }

    public float GetAdditionalPestResistance()
    {
        return additionalPestResistance;
    }

    public void AddAdditionalInheritance(int value)
    {
        additionalInheritance += value;
        return;
    }

    public void AddSoil()
    {
        maxCol += 1;
        //GameObject obj = Instantiate(soilPrefab, this.transform);
        //obj.transform.localPosition = new Vector3(1.7f * (maxCol-1), 0f, 0f);

        gardenRenderer.sprite = gardenSprites[maxCol - 4]; // 정원 배경 스프라이트 변경

        disabledSoil[maxCol - 5].SetActive(true);

        //for (int row = 0; row < 4; row++)
        //{
        //    Transform soilT = disabledSoil[maxCol - 5].transform.GetChild(row);
        //    Soil soil = soilT.GetComponent<Soil>();

        //    if (soil != null)
        //    {
        //        int index = row + (maxCol - 1) * 4;
        //        soil.Init(index);
        //    }
        //}
    }
    public void UpdateSoil()
    {
        if (maxCol <= 4)
            return;
        for (int i = 5; i <= maxCol; i++)
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
    public bool IsAffectedByChiliPepper(int gridIndex)
    {
        Plant chiliPepper;
        int maxIndex = GetMaxCol() * 4;

        // 0단계: 좌우
        if ((gridIndex - 4) >= 0) // 왼쪽
        {
            if (plantGrid.TryGetValue(gridIndex - 4, out chiliPepper))
            {
                if (chiliPepper is ChiliPepper) return true;
            }
        }

        if ((gridIndex + 4) < maxIndex) // 오른쪽
        {
            if (plantGrid.TryGetValue(gridIndex + 4, out chiliPepper))
            {
                if (chiliPepper is ChiliPepper) return true;
            }
        }

        // 1단계: 상하
        if (chiliPepperRangeLevel >= 1)
        {
            if ((gridIndex - 1) / 4 == gridIndex / 4) // 위
            {
                if (plantGrid.TryGetValue(gridIndex - 1, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }

            if ((gridIndex + 1) / 4 == gridIndex / 4) // 아래
            {
                if (plantGrid.TryGetValue(gridIndex + 1, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }
        }

        // 2단계: 대각선
        if (chiliPepperRangeLevel >= 2)
        {
            // 왼쪽 위
            if ((gridIndex - 5) >= 0 && (gridIndex - 5) % 4 == (gridIndex - 1) % 4)
            {
                if (plantGrid.TryGetValue(gridIndex - 5, out chiliPepper))
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
                // 여기서는 "오른쪽 위"라는 의도에 맞게 +3으로 수정하여 구현함. (기존 InfoAppGridSlot 구현과 동일하게)
                 if (plantGrid.TryGetValue(gridIndex + 3, out chiliPepper)) // +3으로 수정
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
                if (plantGrid.TryGetValue(gridIndex - 3, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }

            // 오른쪽 아래
            if ((gridIndex + 5) < maxIndex && (gridIndex + 5) % 4 == (gridIndex + 1) % 4)
            {
                if (plantGrid.TryGetValue(gridIndex + 5, out chiliPepper))
                {
                    if (chiliPepper is ChiliPepper) return true;
                }
            }
        }

        return false;
    }


    public virtual void RequestBreedSelect(GameObject clickedObject)
    {
        if (!isBreeding)
            return;

        Plant clickedPea = clickedObject.GetComponent<Plant>();
        if (clickedPea == null) return;


        if (breedObj1 == clickedObject)
        {
            // 부모 1 선택 취소
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj1 = null;
        }
        else if (breedObj2 == clickedObject)
        {
            // 부모 2 선택 취소
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj2 = null;
        }
        else if (breedObj1 == null)
        {
            // 부모 1 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj1 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else if (breedObj2 == null)
        {
            // 부모 2 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj2 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else
        {
            // 이미 두 부모 선택됨
            SoundManager.Instance.PlayEffect("WrongSelect");
            Debug.Log("이미 두 부모가 모두 선택된 상태");
            return;
        }

        breedButton.SetActive(breedObj1 != null && breedObj2 != null);
        breedButtonPlant = (breedObj1 != null && breedObj2 != null) ? clickedPea : null;
        
        MoveBreedButton(clickedObject.transform.position);        
    }

    public void MoveBreedButton(Vector3 pos)
    {
        RectTransform canvasRect = breedButton.GetComponentInParent<Canvas>().transform as RectTransform;
        Vector3 targetWorldPos = new Vector3(1, 0, 0) + pos;
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(targetWorldPos);

        // 캔버스에 설정된 카메라(renderCamera)를 사용해야 함
        Camera uiCamera = breedButton.GetComponentInParent<Canvas>().worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint);
        breedButton.GetComponent<RectTransform>().anchoredPosition = localPoint;
    }

    public void CheckBreedButtonBeforeDie(GameObject plant)
    {
        if(breedObj1 == plant || breedObj2 == plant)
        {
            breedButton.SetActive(false);
            breedButtonPlant = null;
        }
    }

    public void ActivateBreed()
    {
        isBreedButtonPressed = true;
    }

    protected void DeactivateBreed()
    {
        breedButton.SetActive(false);
        breedButtonPlant = null;
        isBreedButtonPressed = false;
    }

    public void SkipBreed()
    {
        if (GameManager.Instance.GetGameIsStopped())
            return;
        isBreedSkipButtonPressed = true;
    }

    protected void UpdateBreedCountUI(int count)
    {
        breedCountUI.text = $"{count}회";
    }

    private void SpawnRandomBug()
    {
        int stage = GameManager.Instance.stage;
        if (stage < 6) // 해충 웨이브 해금 전엔 등장 X
            return;
        if (Random.Range(0, 100) < (ladybugSpawnProbability * 100))
        {
            // 최대 무당벌레 수 제한 확인
            if (maxLadybugCount > 0 && ladybugs.Count >= maxLadybugCount)
            {
                // 최대 수에 도달했으므로 일반 벌레 스폰
                int bugIndex = Random.Range(0, Mathf.Min(((((stage - 1) / 5) * 2) - 1),bugPrefabs.Count));
                Instantiate(bugPrefabs[bugIndex]);
                return;
            }
            Instantiate(ladybugPrefabs);
            return;
        }
        int i = Random.Range(0, Mathf.Min(((((stage - 1) / 5) * 2) - 1),bugPrefabs.Count)); //벌레 해금 시기와 일치하도록 설정
        Instantiate(bugPrefabs[i]);
        return;
    }
    


    public bool GetIsBreeding()
    {
        return isBreeding;
    }

    public bool TryPlacePlant(Plant plant, Vector3 screenPosition)
    {
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

        if (plantGrid.ContainsKey(toIndex))
        {
            if (!plantGrid[toIndex].CanMove()) // 옮기기 불가능한 식물의 경우
            {
                Transform originalSoil = GetSoilTransform(plant.gridIndex);
                plant.transform.position = originalSoil.position;
                return false;
            }
            // 대상 칸에 식물이 있는 경우: 서로 위치 교환
            Plant targetPlant = plantGrid[toIndex];


            // 서로 gridIndex 바꾸기
            plant.SetGridIndex(toIndex);
            targetPlant.SetGridIndex(fromIndex);            

            // 위치 바꾸기
            Transform fromSoil = GetSoilTransform(fromIndex);
            Transform toSoil = GetSoilTransform(toIndex);
            plant.transform.position = toSoil.position;
            targetPlant.transform.position = fromSoil.position;

            // plantGrid 업데이트
            plantGrid[toIndex] = plant;
            plantGrid[fromIndex] = targetPlant;

            //스카우터 확인 (고추, 비료 때문)
            if(plant is MovablePlant p1)
            {
                p1.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }
            if (targetPlant is MovablePlant p2)
            {
                p2.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }

            return true;
        }
        else
        {

            // 빈 칸이면 원래대로 심기
            plantGrid.Remove(fromIndex); // 원래 위치에서 제거
            plant.SetGridIndex(toIndex);
            plant.transform.position = GetSoilTransform(toIndex).position;
            plantGrid[toIndex] = plant;

            if (plant is MovablePlant p1)
            {
                p1.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }
            return true;
        }
    }

    public int? GetGridIndexFromPosition(Vector3 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);
        //Debug.Log(hit.transform.name + hit.transform.position);

        if (hit.collider != null)
        {
            Soil soil = hit.collider.GetComponent<Soil>();
            if (soil != null)
            {
                return soil.GridIndex;
            }
        }
        return null;
    }   
    public void SetBreedTimerUI(TimerUI timerUI)
    {
        breedTimerUI = timerUI;
    }

    public TimerUI GetBreedTimerUI()
    {
        return breedTimerUI;
    }

    public void LoadGrid(SaveData saveData)
    {
        List<PlantData> plantList = saveData.plantList;
        foreach (var item in plantList)
        {
            GameObject obj = InstantiatePlant(item.speciesname);

            Plant plant = obj.GetComponent<Plant>();
            plant.Init(item.gridIndex, this);
            plant.SetTrait(item.traits);
            plant.SetTaste(item.taste);
            plant.SetResistWaveCount(item.resistWaveCount);
            PlantPlant(plant);
        }
        maxCol = saveData.maxCol;
        UpdateSoil();
        killBugCount = saveData.killBugCount;
        totalBreedCount = saveData.totalBreedCount;
        totalPeaBreedcount = saveData.totalPeaBreedcount;
        totalPeanutBreedCount = saveData.totalPeanutBreedCount;

        bugSpawnTimeInterval = saveData.bugSpawnTimeInterval;
        lastBugSpawnTimeInterval = saveData.lastBugSpawnTimeInterval;

        bugSpeedDecreasement = saveData.bugSpeedDecreasement;
        bugSpawnIntervalIncreasement = saveData.bugSpawnIntervalIncreasement;
        ladybugSpawnProbability = saveData.ladybugSpawnProbability;
        maxLadybugCount = saveData.maxLadybugCount;
        additionalLadybugGoldPerUnit = saveData.additionalLadybugGoldPerUnit;
        additionalLadybugResistancePerUnit = saveData.additionalLadybugResistancePerUnit;
        additionalBugGold = saveData.additionalBugGold;
        additionalNepenthesGold = saveData.additionalNepenthesGold;
        hasNepenthesPheromone = saveData.hasNepenthesPheromone;
        additionalNepenthesPheromoneSizeMultiplier = saveData.additionalNepenthesPheromoneSizeMultiplier;
        nepenthesSpawnProbability = saveData.nepenthesSpawnProbability;

        resistanceBonus = saveData.resistanceBonus; // 식물 공통 특성
        additionalPlantGold = saveData.additionalPlantGold;

        resistanceDecayReduction = saveData.resistanceDecayReduction; // 완두콩 특성
        resistanceAdaptation = saveData.resistanceAdaptation;
        
        additionalPeanutCopyProbability = saveData.additionalPeanutCopyProbability; // 땅콩 특성
        bonusRatioWhenDie = saveData.bonusRatioWhenDie;

        SetResistanceScouter(saveData.hasResistanceScouter); // 일반 특성
        SetGoldScouter(saveData.hasGoldScouter);
        SetWeatherForecast(saveData.hasWeatherForecast);

        weakGeneticsResistanceBonus = saveData.weakGeneticsResistanceBonus;
        strongGeneticsResistanceBonus = saveData.strongGeneticsResistanceBonus;
        goldenGeneticsProbabilityBonus = saveData.goldenGeneticsProbabilityBonus;
        
        // 네펜데스 페로몬 업데이트
        foreach (var plant in plantGrid.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromone();
                nepenthes.UpdatePheromoneSize();
            }
        }
                
        additionalPlantGoldMultiplier = saveData.additionalPlantGoldMultiplier;

        additionalPestResistance = saveData.additionalPestResistance;

        additionalInheritance = saveData.additionalInheritance;
        maxBreedTimer = saveData.maxBreedTimer;
        maxBreedCount = saveData.maxBreedCount;
        
        // 페트병 관련 변수 로드
        petBottleInitialStockBonus = saveData.petBottleInitialStockBonus;
        petBottlePriceReduction = saveData.petBottlePriceReduction;
        petBottleSpawnProbability = saveData.petBottleSpawnProbability;
        if (saveData.petBottleBlockCountBonus > 0)
        {
            if (!petBottleBlockCount.ContainsKey(-1))
                petBottleBlockCount[-1] = 0;
            petBottleBlockCount[-1] = saveData.petBottleBlockCountBonus;
        }
        
        // 고추 관련 변수 로드
        chiliPepperRangeLevel = saveData.chiliPepperRangeLevel;
        chiliPepperSpawnProbability = saveData.chiliPepperSpawnProbability;
        chiliPepperHealPercent = saveData.chiliPepperHealPercent;
        
        // 황금 비료 로드
        foreach(var i in saveData.goldSoilTiles)
            TryPlaceGoldSoil(i);
        
        foreach(var i in saveData.perBottleTiles)
            PlacePetBottle(i);
        for(int i = 0;i<saveData.fertilizerColumns.Count; i++)
            TryPlaceFertilizer(saveData.fertilizerColumns[i] * 4, saveData.fertilizerType[i]);
    }

    public void AddBugSpeedDcreasement(float value)
    {
        bugSpeedDecreasement += value;
    }

    public float GetBugSpeedDecreasement()
    {
        return bugSpeedDecreasement;
    }

    public void AddBugSpawnIntervalIncreasement(float value)
    {
        bugSpawnIntervalIncreasement += value;
    }

    public void AddLadybugSpawnProbability(float value)
    {
        ladybugSpawnProbability += value;
    }

    public void AddMaxLadybugCount(int value)
    {
        maxLadybugCount += value;
    }

    public void AddAdditionalLadybugGoldPerUnit(int value)
    {
        additionalLadybugGoldPerUnit += value;
    }

    public void AddAdditionalLadybugResistancePerUnit(float value)
    {
        additionalLadybugResistancePerUnit += value;
    }

    public void AddAdditionalBugGold(int value)
    {
        additionalBugGold += value;
    }

    public void AddAdditionalNepenthesGold(int value)
    {
        additionalNepenthesGold += value;
    }

    public void SetNepenthesPheromoneEnabled(bool enabled)
    {
        hasNepenthesPheromone = enabled;
        // 기존 네펜데스들의 페로몬 활성화/비활성화
        foreach (var plant in plantGrid.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromone();
            }
        }
    }

    public void UpdateResistanceScouterImageInGrid(WaveType wave)
    {
        if (!hasResistanceScouter)
            return;
        foreach (var plant in plantGrid.Values)
        {
            if (plant is MovablePlant p)
            {
                p.CheckResistanceScouterImage(wave);
            }
        }
    }

    public void UpdateGoldScouterImageInGrid()
    {
        if (!hasGoldScouter)
            return;
        List<MovablePlant> sortedPlants = plantGrid.Values 
                                                   .OfType<MovablePlant>() 
                                                   .OrderByDescending(p => p.GetSellingPrice()) // GetSellingPrice() 결과가 높은 순으로 정렬
                                                   .ToList(); // 정렬된 결과를 리스트로 변환

        for (int i = 0; i < sortedPlants.Count; i++)
        {
            MovablePlant currentPlant = sortedPlants[i];

            if (i < 3)
            {
                currentPlant.ShowGoldScouterImage();
            }
            else 
            {
                currentPlant.HideGoldScouterImage();
            }
        }
    }

    public void AddAdditionalNepenthesPheromoneSizeMultiplier(float value)
    {
        additionalNepenthesPheromoneSizeMultiplier += value;
        // 기존 네펜데스들의 페로몬 크기 업데이트
        foreach (var plant in plantGrid.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromoneSize();
            }
        }
    }

    public float GetEffectiveNepenthesPheromoneSizeMultiplier()
    {
        return 1f + additionalNepenthesPheromoneSizeMultiplier; // 0.2 = +20% = 1.2배
    }

    public void AddResistanceBonus(float value)
    {
        resistanceBonus += value;
        // 기존 식물들에도 적용
        ApplyResistanceBonusToExistingPlants(value);
    }

    private void ApplyResistanceBonusToExistingPlants(float bonus)
    {
        for (int idx = 0; idx < GetMaxCol() * 4; idx++)
        {
            if (plantGrid.ContainsKey(idx))
            {
                Plant plant = plantGrid[idx];
                if (plant is MovablePlant)
                {
                    plant.IncreaseResistance(bonus);
                }
            }
        }
    }

    public void AddWeakGeneticsResistanceBonus(float value)
    {
        weakGeneticsResistanceBonus += value;
        // 기존 식물들에도 적용
        ApplyWeakGeneticsResistanceBonusToExistingPlants(value);
    }

    private void ApplyWeakGeneticsResistanceBonusToExistingPlants(float bonus)
    {
        for (int idx = 0; idx < GetMaxCol() * 4; idx++)
        {
            if (plantGrid.ContainsKey(idx))
            {
                Plant plant = plantGrid[idx];
                if (plant is MovablePlant)
                {
                    plant.IncreaseWeakGeneticsResistance(bonus);
                }
            }
        }
    }

    public void AddStrongGeneticsResistanceBonus(float value)
    {
        strongGeneticsResistanceBonus += value;
        // 기존 식물들에도 적용
        ApplyStrongGeneticsResistanceBonusToExistingPlants(value);
    }

    private void ApplyStrongGeneticsResistanceBonusToExistingPlants(float bonus)
    {
        for (int idx = 0; idx < GetMaxCol() * 4; idx++)
        {
            if (plantGrid.ContainsKey(idx))
            {
                Plant plant = plantGrid[idx];
                if (plant is MovablePlant)
                {
                    plant.IncreaseStrongGeneticsResistance(bonus);
                }
            }
        }
    }

    public void AddGoldenGeneticsProbabilityBonus(float value)
    {
        goldenGeneticsProbabilityBonus += value;
    }

    public void AddNepenthesSpawnProbability(float value)
    {
        nepenthesSpawnProbability += value;
    }

    public void AddPetBottleInitialStockBonus(int value)
    {
        petBottleInitialStockBonus += value;
    }

    public void AddPetBottlePriceReduction(int value)
    {
        petBottlePriceReduction += value;
    }

    public void AddPetBottleSpawnProbability(float value)
    {
        petBottleSpawnProbability += value;
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

    public void AddChiliPepperRangeLevel(int value)
    {
        chiliPepperRangeLevel = Mathf.Min(2, chiliPepperRangeLevel + value); // 최대 2단계
        UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
    }

    public void AddChiliPepperSpawnProbability(float value)
    {
        chiliPepperSpawnProbability += value;
    }

    public void AddChiliPepperHealPercent(float value)
    {
        chiliPepperHealPercent += value;
    }

    /// <summary>
    /// 고추 주변의 식물들을 반환합니다 (고추 제외)
    /// </summary>
    public List<Plant> GetPlantsNearChiliPepper(ChiliPepper chiliPepper)
    {
        List<Plant> nearbyPlants = new List<Plant>();
        if (chiliPepper == null) return nearbyPlants;

        int chiliIndex = chiliPepper.gridIndex;
        int rangeLevel = chiliPepperRangeLevel;

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
        if (idx >= 0 && idx < maxCol * 4 && plantGrid.TryGetValue(idx, out Plant plant))
        {
            if (plant != null && !(plant is ChiliPepper))
            {
                list.Add(plant);
            }
        }
    }

    public int GetAdditionalBugGold()
    {
        return additionalBugGold;
    }

    public void AddAdditionalPlantGold(int value)
    {
        additionalPlantGold += value;
    }

    public int GetAdditionalPlantGold()
    {
        return additionalPlantGold;
    }

    public void AddAdditionalPlantGoldMultiplier(float value)
    {
        additionalPlantGoldMultiplier += value;
    }

    public float GetAdditionalPlantGoldMultiplier()
    {
        return additionalPlantGoldMultiplier;
    }

    public void AddAdditionalPeanutCopyProbability(float value)
    {
        additionalPeanutCopyProbability += value;
    }

    public float GetAdditionalPeanutCopyProbability()
    {
        return additionalPeanutCopyProbability;
    }

    public void AddResistanceDecayReduction(float value)
    {
        resistanceDecayReduction += value;
    }

    public float GetResistanceDecayReduction()
    {
        return resistanceDecayReduction;
    }

    public void AddResistanceAdaptation(float value)
    {
        resistanceAdaptation += value;
    }

    public float GetResistanceAdaptation()
    {
        return resistanceAdaptation;
    }
    public void AddBonusRatioWhenDie(float value)
    {
        bonusRatioWhenDie += value;
    }

    public float GetBonusRatioWhenDie()
    {
        return bonusRatioWhenDie;
    }

    public void SetResistanceScouter(bool val)
    {
        if(hasResistanceScouter = val)
        {
            UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
        }
    }

    public bool GetHasReistanceScouter()
    {
        return hasResistanceScouter;
    }

    public void SetGoldScouter(bool val)
    {
        if(hasGoldScouter = val)
        {
            UpdateGoldScouterImageInGrid();
        }
    }

    public bool GetHasGoldScouter()
    {
        return hasGoldScouter;
    }
    public void SetWeatherForecast(bool val)
    {
        if(hasWeatherForecast = val)
        {

        }
    }

    public bool GetHasWeatherForecast()
    {
        return hasWeatherForecast;
    }



    public bool HasPetBottle(int idx) => petBottleTiles.Contains(idx);

    public bool HasGoldSoil(int idx) => goldSoilTiles.Contains(idx);

    public bool TryPlaceGoldSoil(int idx)
    {
        if (goldSoilTiles.Contains(idx))
        {
            return false; // 이미 황금 비료가 있는 칸
        }
        if (plantGrid.ContainsKey(idx))
        {
            return false; // 식물이 있는 칸에는 황금 비료를 뿌릴 수 없음
        }

        goldSoilTiles.Add(idx);

        // 시각화 (황금색 마커)
        if (goldSoilMarkerPrefab != null)
        {
            var soilT = GetSoilTransform(idx);
            var marker = Instantiate(goldSoilMarkerPrefab, soilT.position, Quaternion.identity, soilT);
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
                Destroy(marker);
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
            if (idx < 0 || idx >= maxCol * 4)
            {
                Debug.LogError($"[Grid] PlacePetBottle: Invalid idx={idx}, maxCol={maxCol}, maxIdx={maxCol * 4}");
                return;
            }

            petBottleTiles.Add(idx);
            // 기본 보호 횟수 1 (재질 강화 아이템으로 증가 가능)
            int baseBlockCount = 1 + (petBottleBlockCount.ContainsKey(-1) ? petBottleBlockCount[-1] : 0); // -1은 전체 보호 횟수 보너스
            petBottleBlockCount[idx] = baseBlockCount;

            // 시각화(선택)
            if (petBottleMarkerPrefab != null)
            {
                var soilT = GetSoilTransform(idx);
                if (soilT != null)
                {
                    // petMarker를 부모 없이 생성하여 부모 비활성화 영향 방지 (고추 ghost prefab과 동일한 방식)
                    var marker = Instantiate(petBottleMarkerPrefab, soilT.position + new Vector3(-0.1f, 0f, 0f), Quaternion.identity);
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
                        Destroy(marker);
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
                    Destroy(marker);
                    petMarkers.Remove(idx);
                }
                return true;
            }
        }
        
        if (cause == DeathCause.Bug && killer != null && ladybugs.Count != 0) // 벌레로 인한 죽음인데, 필드에 익충이 있는 경우
        {
            StartCoroutine(ladybugs[0].KillFuckingBug(killer));
            return true;
        }

        return false; // 죽음 방어 실패
    }

    //-----전용 비료------
    public bool HasFertilizerAt(int idx) => fertilizerColumns.ContainsKey(GetCol(idx));

    public bool HasBreedablePlantAt(int idx)
    {
        if (!plantGrid.ContainsKey(idx)) return false;

        if (plantGrid[idx].GetType() == typeof(Pea) || plantGrid[idx].GetType() == typeof(Peanut))
            return true;
        return false;
    }
    
    public Dictionary<int, WaveType> GetFertilizerColumns()
    {
        return fertilizerColumns;
    }

    public WaveType GetFertilizerType (int idx)
    {
        return fertilizerColumns[GetCol(idx)];
    }

    public bool TryPlaceFertilizer(int idx, WaveType wave)
    {
        if (HasFertilizerAt(idx)) return false; // 이미 다른 전용 비료가 있으면 불가

        int col = GetCol(idx);

        fertilizerColumns.Add(col, wave);

        Transform soilColT = transform.GetChild(col);
        var marker = soilColT.GetComponentInChildren<FertilizerMarker>(true);

        if (marker == null)
        {
            Debug.Log($"[Grid] No marker: col={col}, wave={wave}");
            return false;
        }
        marker.SetFertilizer(wave);

        Debug.Log($"[Grid] Fertilizer placed: col={col}, wave={wave}");
        UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
        return true;
    }

    public void RemoveFertilizerAt(int col)
    {
        // SoilCol Transform 가져오기
        Transform soilColT = transform.GetChild(col);
        var marker = soilColT.GetComponentInChildren<FertilizerMarker>(true);

        if (marker != null && marker.IsOn)
        {
            if (fertilizerColumns.ContainsKey(col))
            {
                fertilizerColumns.Remove(col);
            }

            marker.RemoveFertilizer();
            UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
            Debug.Log($"[Grid] Fertilizer removed at col={soilColT.GetSiblingIndex()}");
        }
    }

    int GetCol(int index) => index / 4;

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

    public int GetLivingPlantCount()
    {
        int count = 0;
        Plant plant;
        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            plant = null;
            plantGrid.TryGetValue(idx, out plant);
            if (plant == null)
                continue;
            if (plant.GetType() == typeof(Pea) || plant.GetType() == typeof(Peanut))
                count++;
        }
        return count;
    }

    public bool HasEmptyGrid()
    {
        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            if (!plantGrid.ContainsKey(idx))
            {
                return true;
            }
        }
        return false;
    }

    public bool HasEmptyFetrilizerGrid()
    {
        for (int idx = 0; idx < maxCol; idx++)
        {
            if (!fertilizerColumns.ContainsKey(idx))
            {
                return true;
            }
        }
        return false;
    }

    public void AddSuperPea()
    {
        List<GeneticTrait> peaTrait = new List<GeneticTrait>
        {
            new GeneticTrait(TraitType.NaturalDeath, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Pest, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Wind, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Flood, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.HeavyRain, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Cold, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Drought, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Heat, 1f , 2, 0.0f)
        };
        AddMovablePlant(peaTrait);        
    }

    public void ShowAllPriceSign()
    {
        showingAllPrice = true;
        foreach(var p in plantGrid)
        {
            p.Value.ShowPriceSign();
        }
    }
    public void HideAllPriceSign()
    {
        showingAllPrice = false;
        foreach (var p in plantGrid)
        {
            p.Value.HidePriceSign();
        }
    }

    public int CountNoTraitPlant(WaveType wave)
    {
        int count = 0;
        for (int idx = 0; idx < GetMaxCol() * 4; idx++)
        {
            if (plantGrid.ContainsKey(idx))
            {
                Plant plant = plantGrid[idx];

                if (!plant.HasTrait(wave))
                {
                    count++;
                }
            }
        }
        return count;
    }

}


