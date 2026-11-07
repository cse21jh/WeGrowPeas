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
    [SerializeField] protected GameObject moneyTreePrefab;
    [SerializeField] protected GameObject sprinklerPrefab;
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
    [SerializeField] private Material soilHighlightMaterial;


    private const float FertilizerResistBonus = 0.05f; // +5%p

    public bool showingAllPrice = false;

    public List<Ladybug> ladybugs = new List<Ladybug>();

    public bool isDraggingShovel = false;

    protected Plant breedButtonPlant = null;
    private int hoveredSoilIndex = -1;

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
    public BugSpawner Bugs => bugs ??= new BugSpawner(this, bugPrefabs, ladybugPrefabs);
    private BugSpawner bugs;

    /// <summary>비료, 황금 토양, 페트병처럼 땅에 놓이는 효과.</summary>
    public SoilEffects Soil => soil ??= new SoilEffects(this, petBottleMarkerPrefab, goldSoilMarkerPrefab);
    private SoilEffects soil;

    //저장 필요
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    [HideInInspector] public int maxCol = 4;
    public int killBugCount = 0;
    public int totalBreedCount = 0;
    public int totalPeaBreedcount = 0;
    public int totalPeanutBreedCount = 0;








    //식물 공통 특성



    //완두콩 특성

    //완두콩 특성

    // 땅콩 특성

    //일반 특성
    protected bool hasWeatherForecast = false;

    protected float maxBreedTimer = 50.0f;
    protected int maxBreedCount = 4;
    protected int breedCount = 0;
    private int? shownBreedCount;



    // 신용카드: 하루에 3슬롯 이상 구매 시 자유시간에 환급받는 소모 비용 비율
    // 쌍둥이: 교배 시 식물이 하나 더 생성될 확률(0~1)

    //완두콩 전용 아이템

    //땅콩 전용 아이템


    private int mostExpensivePlant = 0;



    //식물 공통 특성

    //완두콩 특성
    //땅콩 특성

    //일반 특성
    public bool HasWeatherForecast => hasWeatherForecast;

    public float MaxBreedTimer => maxBreedTimer;
    public int MaxBreedCount => maxBreedCount;

    public int BreedCount => breedCount; // 스테이지 단위 저장이라 아직 ㄱㅊ
    public int MostExpensivePlant => mostExpensivePlant;

    // 신규 아이템(신용카드·쌍둥이·완두커피·슈퍼 변종·활성형 껍질·왕위 계승·땅과 콩)


    // 그리드 상태 변경 이벤트 (식물 배치/제거/이동 등)
    public event System.Action OnGridStateChanged;

    public void NotifyGridStateChanged() => OnGridStateChanged?.Invoke();

    // LoadGrid 중에는 OnGridStateChanged 발생을 억제 (MoneyTree가 로드 순서에 따라 즉시 Die하는 버그 방지)
    private bool isLoadingGrid = false;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        enemyController = FindAnyObjectByType<EnemyController>();
        //InitGrid();
        InitSoils();
        plantGrid.Clear();
        breedButton.SetActive(false);
        breedButtonPlant = null;
        EconomyFeedbackController.BindBreedCountUi(breedCountUI);
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (breedButtonPlant != null)
        {
            MoveBreedButton(breedButtonPlant.transform.position);
        }

        Soil.KeepPetMarkersVisible();

    }

    public void InitGrid()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = InstantiatePlant(GameManager.Instance.currentPlant);
            Plant plant = obj.GetComponent<Plant>();
            List<GeneticTrait> basicTrait = new List<GeneticTrait> { new GeneticTrait(TraitType.NaturalDeath, Plant.GetResistanceBasedOnGenetics(TraitType.NaturalDeath, 1), 1, 0.0f), };
            //FenceUIManager.Instance.SetFenceElements(0, plant);
            plant.SetTrait(basicTrait);
            AddPlantToGrid(plant);
        }

    }

    private void ExecuteBreeding()
    {
        if (breedObj1 != null && breedObj2 != null)
        {
            Plant parent1 = breedObj1.GetComponent<Plant>();
            Plant parent2 = breedObj2.GetComponent<Plant>();
            //자식 완두콩 형질 계산 후 Instantiate

            // 교배 횟수 증가 아이템이 구매되었을 때 바로 적용되도록 매번 재계산
            int currentEffectiveMaxBreedCount = Mathf.Max(1, Mathf.FloorToInt(maxBreedCount * ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1)) + GetKingReturnBreedDelta());

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
                // 활성형 껍질: 교배를 시도한 식물은 이후 자가번식 확률 보너스를 받지 못함
                parent1.MarkBreedAttempted();
                parent2.MarkBreedAttempted();

                // 저주: 씨 없는 수박 — 확률적으로 교배 실패(시도는 소모)
                if (CurseState.SeedlessFailPercent > 0f && Random.Range(0f, 100f) < CurseState.SeedlessFailPercent)
                {
                    breedCount++;
                    Debug.Log("씨 없는 수박 저주로 교배 실패");
                    SoundManager.Instance.PlayEffect(SoundIds.WrongSelect); // TODO: 실패 전용 효과음/연기 이펙트
                    UpdateBreedCountUI(currentEffectiveMaxBreedCount - breedCount);
                    breedObj1.GetComponent<Plant>().MakeDefaultSprite();
                    breedObj2.GetComponent<Plant>().MakeDefaultSprite();
                    breedObj1 = null;
                    breedObj2 = null;
                    DeactivateBreed();
                    return;
                }

                // 저주: 광란 — 확률적으로 랜덤 교배
                bool madnessBreed = CurseState.BreedMadnessPercent > 0f && Random.Range(0f, 100f) < CurseState.BreedMadnessPercent;
                Plant plant = AddMovablePlant(Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait(), madnessBreed));
                PlayMutationEffectIfAny(plant); // 변종(돌연변이) 이펙트
                if (UIManager.Instance != null && UIManager.Instance.ShowBreedPopupSetting)
                {
                    UIManager.Instance.Popup.ShowBreedPopup(plant);
                }

                // 쌍둥이: 확률적으로 교배를 한 번 더 실행해 유전자가 다른 이란성 쌍둥이를 생성
                if (Modifiers.TwinBreedProbability > 0f && Random.Range(0f, 1f) < Modifiers.TwinBreedProbability && HasEmptyGrid())
                {
                    // 교배 팝업은 인스턴스를 하나만 재사용하므로 쌍둥이는 플로팅 팝업으로만 알림
                    PlayMutationEffectIfAny(
                        AddMovablePlant(Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait(), madnessBreed)));
                    UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.twins"), 1);
                    totalBreedCount++;
                    if (GameManager.Instance.currentPlant == PlantIds.Pea)
                        totalPeaBreedcount++;
                    else if (GameManager.Instance.currentPlant == PlantIds.Peanut)
                        totalPeanutBreedCount++;
                }

                // 특수(밑장빼기): 25% 확률로 교배 횟수 미소모
                if (!(SpecialItemSystem.Has("bottom_deal") && Random.Range(0f, 100f) < 25f))
                    breedCount++;
                GameEvents.RaisePeaBreeded();
                Debug.Log("자식 생성 성공. 남은 교배 횟수는 " + (currentEffectiveMaxBreedCount - breedCount) + "입니다");
                SoundManager.Instance.PlayEffect(SoundIds.Breed);
                totalBreedCount++;
                if (GameManager.Instance.currentPlant == PlantIds.Pea)
                    totalPeaBreedcount++;
                else if (GameManager.Instance.currentPlant == PlantIds.Peanut)
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
                UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.no_attempts"), 1);
                SoundManager.Instance.PlayEffect(SoundIds.WrongSelect);
                isBreedButtonPressed = false;
            }
            else if (isEqualPlant)
            {
                UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.species_mismatch"), 1);
                SoundManager.Instance.PlayEffect(SoundIds.WrongSelect);
                isBreedButtonPressed = false;
            }
            else
            {
                UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.no_space"), 1);
                SoundManager.Instance.PlayEffect(SoundIds.WrongSelect);
                isBreedButtonPressed = false;
            }
        }
        else
        {
            UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.select_two"), 1);
            isBreedButtonPressed = false;
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
        int effectiveMaxBreedCount = Mathf.Max(1, Mathf.FloorToInt(maxBreedCount * ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1)) + GetKingReturnBreedDelta());
        // 저주: 불면증 — 밤시간(폰 페이즈) 단축으로 변경되어 교배시간 영향 제거
        float effectiveMaxBreedTimer = MaxBreedTimer * ModManager.Instance.GetMul(StatId.BreedingPhaseDurationMul, -1);

        //임시 알람
        bool _warned15s = false;

        //Debug.Log(effectiveMaxBreedTimer + "초 시작. 최대 교배 횟수는 " + effectiveMaxBreedCount + "입니다");
        UpdateBreedCountUI(effectiveMaxBreedCount);
        breedTimer = effectiveMaxBreedTimer;
        breedTimerUI.StartBreedingTimer();

        // 저주(통신장애): 낮 시간(교배 페이즈)의 앞부분 동안 폰 차단 시작
        if (PhoneManager.Instance != null) PhoneManager.Instance.BeginEmpBlockIfActive(effectiveMaxBreedTimer);

        // 신용카드: 어제 상점에서 3슬롯 이상 구매했다면 소모 비용의 일부를 환급 (없어도 일일 집계는 초기화)
        if (ShopManager.Instance != null)
        {
            int refund = ShopManager.Instance.ConsumeDailyPurchaseRefund(Modifiers.CreditCardRefundPercent);
            if (refund > 0)
            {
                GameManager.Instance.economyManager.AddGold(refund);
                UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.card_refund", ("gold", refund)), 1);
            }
        }

        // 특수(세계여행): 낮 시작 시 각 식물의 위치 마킹 (웨이브 후 이동 거리 계산용)
        if (SpecialItemSystem.Has("world_travel"))
            foreach (var p in plantGrid.Values)
                if (p != null) p.MarkDayStartPosition();

        breedSkipButton.SetActive(true);
        enemyController.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;

        Bugs.BeginDay();

        while (breedTimer > 0 && !isBreedSkipButtonPressed)
        {
            if (GameManager.Instance.GetGameIsStopped())
            {
                yield return null;
                continue;
            }

            breedTimer -= Time.deltaTime;

            if (breedTimer < 15f && !_warned15s)
            {
                _warned15s = true;

                PhoneNotificationBus.OnShow?.Invoke(
                    new PhoneNotificationData
                    {
                        title = Loc.T("game.wave.soon_title"),
                        message = Loc.T("game.wave.soon_message"),
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

            Bugs.Tick(Time.deltaTime);

            if (isBreedButtonPressed || Input.GetKeyDown(KeyCode.Space))
            {
                ExecuteBreeding();
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

        int currentEffectiveMaxBreedCount = Mathf.Max(1, Mathf.FloorToInt(maxBreedCount * ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1)) + GetKingReturnBreedDelta());
        int remainingBreeds = Mathf.Max(0, currentEffectiveMaxBreedCount - breedCount);
        GameEvents.RaiseDayEndedWithRemainingBreeds(remainingBreeds);

        breedCount = 0;
        // Debug.Log("교배 페이즈 종료");
        breedButton.SetActive(false);
        breedButtonPlant = null;
        enemyController.HideWaveSkipButton();
        isBreeding = false;
        breedSkipButton.SetActive(false);

        // 저주(통신장애): 자유시간이 끝나면(스킵 포함) 노이즈도 바로 끈다
        if (PhoneManager.Instance != null) PhoneManager.Instance.EndEmpBlock();

        // 완두커피: 자유시간이 지난 식물은 조금 더 비싸짐
        if (Modifiers.PeaCoffeeMultiplier > 0f)
        {
            foreach (var p in plantGrid.Values)
                if (p != null) p.OnFreeTimePassed();
        }
        //GardenGrid 리로드

        yield return null;
    }

    // 직전 Breed()의 변종 결과. 생성된 식물에 이펙트를 재생하기 위해 호출부가 참조한다.
    // (0 = 변종 없음, 1 = 악성, 2 = 양성)
    private int lastMutationKind = 0;

    /// <summary>직전 교배에서 변종이 발생했는가.</summary>
    public bool LastBreedWasMutation => lastMutationKind != 0;
    /// <summary>직전 변종이 양성이었는가. (변종이 아니면 의미 없음)</summary>
    public bool LastBreedWasBenign => lastMutationKind == 2;

    protected List<GeneticTrait> Breed(List<GeneticTrait> parent1, List<GeneticTrait> parent2, bool randomBreed = false)
    {
        List<GeneticTrait> childTrait = new List<GeneticTrait>();

        // 변종 판정(식물 단위, 악성:양성 = 80:20)
        // - 악성: 부모 무관 유전자 무작위, 저항력은 해당 유전자의 일반 범위 롤
        // - 양성: 유전자는 부모 상속, 모든 저항력 90~100%
        bool malignant = false, benign = false;
        if (Random.Range(0f, 100f) < Plant.GetMutationChancePercent())
        {
            // 악성 비율: 기본 0.8, 슈퍼 변종이면 반전(0.2), 디버그 패널이 지정하면 그 값
            float malignantRatio = Modifiers.HasSuperMutation ? 0.2f : 0.8f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MutationDebug.HasRatioOverride) malignantRatio = MutationDebug.MalignantRatioOverride;
#endif

            if (SpecialItemSystem.Has("peanut_special_8")) malignant = true; // 특수(임시땅콩B): 교배 시 악성만
            else if (Random.value < malignantRatio) malignant = true;
            else benign = true;
            Debug.Log($"[변종] {(malignant ? "악성" : "양성")} 변종 발생!");
        }
        lastMutationKind = malignant ? 1 : (benign ? 2 : 0);

        foreach (TraitType trait in System.Enum.GetValues(typeof(TraitType)))
        {
            if (trait == TraitType.None || trait == TraitType.Drought || trait == TraitType.Heat)
                break; // 가뭄, 더위에 대한 형질은 SetTrait에서 삽입해줌.따로따로 교배 로직을 거치면 이상하게 나옴. 

            int p1Trait;
            int p2Trait;

            float p1Resistance = 0f;
            float p2Resistance = 0f;

            int childGenetic = 0;

            int traitNotInParent = 0;

            if (parent1.Any(t => t.traitType == trait))
            {
                p1Trait = parent1.First(t => t.traitType == trait).genetics;
                p1Resistance = parent1.First(t => t.traitType == trait).resistance;
            }
            else
            {
                p1Trait = 0;
                traitNotInParent += 1;
            }

            if (parent2.Any(t => t.traitType == trait))
            {
                p2Trait = parent2.First(t => t.traitType == trait).genetics;
                p2Resistance = parent2.First(t => t.traitType == trait).resistance;
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
                    int p1Bonus = (int)(Modifiers.GoldenGeneticsProbabilityBonus * 100); // 보너스를 퍼센트로 변환
                    childGenetic += (Random.Range(1, 101) <= (Modifiers.AdditionalInheritance + 50 + p1Bonus) ? 1 : 0);
                    break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1:
                    // 황금 유전자 확률 보너스 적용: genetics = 1에서 우수한 형질이 나올 확률 증가
                    int p2Bonus = (int)(Modifiers.GoldenGeneticsProbabilityBonus * 100); // 보너스를 퍼센트로 변환
                    childGenetic += (Random.Range(1, 101) <= (Modifiers.AdditionalInheritance + 50 + p2Bonus) ? 1 : 0);
                    break;
                default: break;
            }

            if (randomBreed || malignant) childGenetic = Random.Range(0, 3); // 저주: 광란 / 악성 변종(유전자 무작위)

            // 특수(임시완두C): 양성 변종의 유전자를 현재 계절에 유리한 쪽으로 변경
            if (benign && SpecialItemSystem.Has("pea_special_12"))
                childGenetic = GetSeasonFavoredGenetics(trait);

            float resistance = Plant.GetResistanceBasedOnGenetics(trait, childGenetic, p1Resistance, p2Resistance);

            if (benign) resistance = Random.Range(90, 101) / 100f; // 양성 변종: 모든 저항 90~100%


            if (trait == TraitType.Pest)
                childTrait.Add(new GeneticTrait(trait, resistance, childGenetic, Modifiers.AdditionalPestResistance));
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
        Transform soilT = GetSoilTransform(plant.gridIndex);
        plant.transform.position = soilT.position;

        // 저장 복원 중에는 기존 뿌리 상태를 아래에서 그대로 불러온다.
        // 새 식물은 실제 흙 위치로 옮긴 뒤 판정해야 가격 상승 팝업도 올바른 곳에서 시작한다.
        if (!isLoadingGrid && plant is MovablePlant movablePlant)
            movablePlant.TryRootAfterPlacement();

        Scouter.UpdateGoldScouterImageInGrid();

        // 그리드 상태 변경 알림 (로딩 중에는 억제 — 모든 식물 로드 후 일괄 발송)
        if (!isLoadingGrid)
            OnGridStateChanged?.Invoke();
    }

    public Plant AddMovablePlant(List<GeneticTrait> trait, int grididx = -1)
    {
        GameObject obj = InstantiatePlant(GameManager.Instance.currentPlant);
        Plant plant = obj.GetComponent<Plant>();
        plant.SetTrait(trait);
        AddPlantToGrid(plant, grididx);

        // 특수(임시땅콩A): 새 식물이 생겨날 때 가격의 50% 골드 (실제로 심어진 경우만)
        if (SpecialItemSystem.Has("peanut_special_4")
            && plantGrid.TryGetValue(plant.gridIndex, out Plant placed) && placed == plant)
            GameManager.Instance.economyManager.AddGold(Mathf.RoundToInt(plant.GetSellingPrice() * 0.5f));

        return plant;
    }

    /// <summary>직전 교배가 변종이었다면 해당 식물에 변종 이펙트를 재생한다.</summary>
    public void PlayMutationEffectIfAny(Plant plant)
    {
        if (plant == null || !LastBreedWasMutation) return;
        CurseEffectManager.PlayMutation(plant, LastBreedWasBenign);
    }

    /// <summary>
    /// 특수(임시완두C): 양성 변종의 계절 유리 유전자.
    /// 앞쪽 4개(자연사·해충·바람·홍수) = 금색 2개(genetics 2).
    /// 뒤쪽 2개(장마·추위) = 계절 따라: 겨울·가을 → 금색(2, 장마/추위 저항↑), 여름·봄 → 회색(0, 짝 형질 가뭄/더위 저항↑).
    /// // 계절→유불리 매핑은 기획 확정 시 조정
    /// </summary>
    private int GetSeasonFavoredGenetics(TraitType trait)
    {
        if (trait != TraitType.HeavyRain && trait != TraitType.Cold) return 2;
        Season season = enemyController.CurrentSeason;
        bool coldSide = season == Season.Winter || season == Season.Fall;
        return coldSide ? 2 : 0;
    }

    /// <summary>특수(왕의 귀환): 기본 교배 횟수 -2, 누적 획득 골드 5000당 +1.</summary>
    private int GetKingReturnBreedDelta()
    {
        if (!SpecialItemSystem.Has("king_return")) return 0;
        int total = GameManager.Instance != null ? GameManager.Instance.economyManager.TotalGold : 0;
        return -2 + total / 5000;
    }

    /// <summary>특수(알록달록): 해당 칸에 적용 중인 땅/식물 효과 개수 (정보 UI 기준과 동일).</summary>
    public int CountTileEffects(int idx)
    {
        int count = 0;
        if (Soil.HasGoldSoil(idx)) count++;
        if (Soil.TryGetFertilizerType(idx, out _)) count++;
        if (Soil.HasPetBottle(idx)) count++;
        if (IsAffectedByChiliPepper(idx)) count++;
        if (IsPlantFrozen(idx)) count++;
        if (IsAffectedBySprinkler(idx)) count++;
        if (Soil.HasAbsorbFertilizer(idx)) count++;
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
            if (n < 0 || n >= maxCol * 4) continue;
            if (plantGrid.TryGetValue(n, out Plant p) && p != null && !string.IsNullOrEmpty(p.speciesname))
                species.Add(p.speciesname);
        }
        return species.Count;
    }

    // 특수(땅부자): 세로줄별 고속 숙성 배수 보너스 (col → +mul, 중첩 가능)

    // 특수(땅부자) 세로줄 배수 저장/복원 (index = col)
    /// <summary>특수(순환): 판매한 식물 주변 4칸 식물의 모든 저항력을 5%p 회복 (형질별 최대 90%).</summary>
    public void HealNeighborsBySell(int idx)
    {
        if (!SpecialItemSystem.Has("circulation")) return;

        int row = idx % 4;
        int[] neighbors = { idx - 4, idx + 4, row > 0 ? idx - 1 : -1, row < 3 ? idx + 1 : -1 };
        foreach (int n in neighbors)
        {
            if (n < 0 || n >= maxCol * 4) continue;
            if (!plantGrid.TryGetValue(n, out Plant p) || p == null) continue;

            var traits = p.GetGeneticTrait();
            for (int i = 0; i < traits.Count; i++)
            {
                float cur = traits[i].resistance;
                if (cur >= 0.9f) continue;
                p.ChangeResistance((int)traits[i].traitType, Mathf.Min(0.05f, 0.9f - cur));
            }
        }
    }

    public GameObject InstantiatePlant(string plantName)
    {
        switch (plantName)
        {
            case PlantIds.Pea: return Instantiate(peaPrefab);
            case PlantIds.Peanut: return Instantiate(peanutPrefab);
            case PlantIds.Nepenthes: return Instantiate(nepenthesPrefab);
            case PlantIds.ChiliPepper: return Instantiate(chiliPepperPrefab);
            case PlantIds.Sprinkler: return Instantiate(sprinklerPrefab);
            case PlantIds.MoneyTree: return Instantiate(moneyTreePrefab);
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
        Scouter.UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
    }

    public void AddMoneyTree(int idx)
    {
        GameObject obj = Instantiate(moneyTreePrefab);
        MoneyTree moneyTree = obj.GetComponent<MoneyTree>();
        AddPlantToGrid(moneyTree, idx);
    }

    public void AddSprinkler(int idx)
    {
        GameObject obj = Instantiate(sprinklerPrefab);
        Sprinkler sprinkler = obj.GetComponent<Sprinkler>();
        AddPlantToGrid(sprinkler, idx);
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
        if (plantGrid.ContainsKey(gridIndex))
        {
            plantGrid.Remove(gridIndex);
            OnGridStateChanged?.Invoke();
        }

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
            UpdateBreedCountUI(GetEffectiveRemainingBreedCount());
        }
    }

    public int GetEffectiveRemainingBreedCount()
    {
        int effectiveMaxBreedCount = Mathf.Max(
            1,
            Mathf.FloorToInt(maxBreedCount * ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1))
                + GetKingReturnBreedDelta());
        return Mathf.Max(0, effectiveMaxBreedCount - breedCount);
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
        Modifiers.AdditionalPestResistance += value;
        if (Modifiers.AdditionalPestResistance > 0.15f)
            Modifiers.AdditionalPestResistance = 0.15f;
        AddAdditionalResistanceInGrid(TraitType.Pest, value);
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
        if (Modifiers.ChiliPepperRangeLevel >= 1)
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
        if (Modifiers.ChiliPepperRangeLevel >= 2)
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
                // 여기서는 "오른쪽 위"라는 의도에 맞게 +3으로 수정하여 구현함.
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

        // 저주(꽃가루 실종): 교배 불가 식물은 부모로 새로 선택 불가(이미 선택된 건 취소 허용)
        if (clickedObject != breedObj1 && clickedObject != breedObj2 && !clickedPea.IsBreedable)
        {
            SoundManager.Instance.PlayEffect(SoundIds.WrongSelect);
            return;
        }

        if (breedObj1 == clickedObject)
        {
            // 부모 1 선택 취소
            SoundManager.Instance.PlayEffect(SoundIds.SelectPlant);
            clickedPea.MakeDefaultSprite();
            breedObj1 = null;
        }
        else if (breedObj2 == clickedObject)
        {
            // 부모 2 선택 취소
            SoundManager.Instance.PlayEffect(SoundIds.SelectPlant);
            clickedPea.MakeDefaultSprite();
            breedObj2 = null;
        }
        else if (breedObj1 == null)
        {
            // 부모 1 선택
            SoundManager.Instance.PlayEffect(SoundIds.SelectPlant);
            breedObj1 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else if (breedObj2 == null)
        {
            // 부모 2 선택
            SoundManager.Instance.PlayEffect(SoundIds.SelectPlant);
            breedObj2 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else
        {
            // 이미 두 부모 선택됨
            SoundManager.Instance.PlayEffect(SoundIds.WrongSelect);
            Debug.Log("이미 두 부모가 모두 선택된 상태");
            return;
        }

        breedButton.SetActive(breedObj1 != null && breedObj2 != null);
        breedButtonPlant = (breedObj1 != null && breedObj2 != null) ? clickedPea : null;

        MoveBreedButton(clickedObject.transform.position);
    }

    // ───── 저주 액션 (CurseInstance.Activate에서 호출) ─────

    // 마우스 포인터가 교배 버튼 영역 위에 있는지 검사.
    // 식물의 OnMouseUp(콜라이더 이벤트)은 UI를 존중하지 않으므로, 교배 버튼이
    // 선택된 식물 위에 겹쳐 있을 때 식물 클릭이 선택을 취소하는 것을 막기 위해 직접 검사한다.
    public bool IsPointerOverBreedButton()
    {
        if (breedButton == null || !breedButton.activeInHierarchy)
            return false;

        RectTransform rt = breedButton.GetComponent<RectTransform>();
        Canvas canvas = breedButton.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam);
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
        if (breedObj1 == plant || breedObj2 == plant)
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

        // [시간은 금이다] 아이템 효과
        if (Modifiers.TimeIsGoldLevel > 0)
        {
            if (breedTimer > 0.1f)
            {
                int reward = Mathf.CeilToInt(breedTimer) * 10 * Modifiers.TimeIsGoldLevel;
                if (reward > 0)
                {
                    //GameManager.Instance.economyManager.AddGold(reward);
                    // AddGold에 로그가 있을 수 있으므로 여기서는 로그 생략하거나 필요시 추가
                    GameManager.Instance.economyManager.AddGold(reward);
                    Debug.Log($"[TimeIsGold] Success! Skipped. Remain: {breedTimer:F2}s, Level: {Modifiers.TimeIsGoldLevel}, Reward: {reward}G");
                }
            }
        }

        isBreedSkipButtonPressed = true;
    }

    protected void UpdateBreedCountUI(int count)
    {
        shownBreedCount = count;
        ApplyBreedCountText();
        GameEvents.RaiseBreedCountChanged(count);
    }

    private void ApplyBreedCountText()
    {
        if (shownBreedCount.HasValue)
            breedCountUI.text = Loc.T("game.common.times", ("count", shownBreedCount.Value));
    }

    private void OnEnable() => Loc.LanguageChanged += ApplyBreedCountText;

    private void OnDisable() => Loc.LanguageChanged -= ApplyBreedCountText;

    /// <summary>디버그 전용: 벌레(또는 확률에 따라 무당벌레) 1마리를 즉시 소환한다. 등장 스테이지 제한 무시.</summary>
    public void DebugSpawnBug()
    {
        Bugs.Spawn(ignoreStageGate: true);
    }

    /// <summary>디버그 전용: 현재 재배 중인 식물을 무작위 형질로 1개 추가한다.</summary>
    public Plant DebugSpawnRandomPlant()
    {
        if (!HasEmptyGrid())
        {
            Debug.LogWarning("[Debug] 빈 칸이 없어 식물을 추가할 수 없습니다.");
            return null;
        }

        // 대응 형질(가뭄·더위)은 SetTrait에서 자동으로 채워주므로 여기서는 제외한다.
        TraitType[] pickable =
        {
            TraitType.NaturalDeath, TraitType.Pest, TraitType.Wind,
            TraitType.Flood, TraitType.HeavyRain, TraitType.Cold
        };
        TraitType trait = pickable[Random.Range(0, pickable.Length)];
        int genetics = Random.Range(0, 3);

        var traits = new List<GeneticTrait>
        {
            new GeneticTrait(trait, Plant.GetResistanceBasedOnGenetics(trait, genetics), genetics, 0f)
        };

        Plant plant = AddMovablePlant(traits);
        Debug.Log($"[Debug] {GameManager.Instance.currentPlant} 추가: {trait} (유전자 {genetics})");
        return plant;
    }



    public bool GetIsBreeding()
    {
        return isBreeding;
    }

    public bool TryPlacePlant(Plant plant, Vector3 screenPosition)
    {
        ClearHoverHighlight();
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
            Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();

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
            if (plant is MovablePlant p1)
            {
                p1.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }
            if (targetPlant is MovablePlant p2)
            {
                p2.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }

            OnGridStateChanged?.Invoke();
            RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.Moved);
            if (fromIndex != toIndex) GameEvents.RaisePlantMoved();
            return true;
        }
        else
        {
            Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
            // 빈 칸이면 원래대로 심기
            plantGrid.Remove(fromIndex); // 원래 위치에서 제거
            plant.SetGridIndex(toIndex);
            plant.transform.position = GetSoilTransform(toIndex).position;
            plantGrid[toIndex] = plant;

            if (plant is MovablePlant p1)
            {
                p1.CheckResistanceScouterImage(enemyController.CurrentWave.WaveType);
            }
            OnGridStateChanged?.Invoke();
            RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.Moved);
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

    public void HighlightHoveredTile(int? targetIndex, Material customMaterial = null, UnityEngine.Color? customOutlineColor = null)
    {
        // 1. Un-highlight previous
        if (hoveredSoilIndex != -1 && (!targetIndex.HasValue || hoveredSoilIndex != targetIndex.Value))
        {
            Transform oldSoilT = GetSoilTransform(hoveredSoilIndex);
            if (oldSoilT != null)
            {
                Soil oldSoil = oldSoilT.GetComponent<Soil>();
                if (oldSoil != null) oldSoil.SetHighlight(false);
            }
            hoveredSoilIndex = -1;
        }

        // 2. Highlight new
        if (targetIndex.HasValue)
        {
            int idx = targetIndex.Value;
            if (hoveredSoilIndex != idx)
            {
                Transform newSoilT = GetSoilTransform(idx);
                if (newSoilT != null)
                {
                    Soil newSoil = newSoilT.GetComponent<Soil>();
                    if (newSoil != null)
                    {
                        Material matToUse = customMaterial != null ? customMaterial : soilHighlightMaterial;
                        newSoil.SetHighlight(true, matToUse, customOutlineColor);
                    }
                }
                hoveredSoilIndex = idx;
            }
        }
    }

    public void ClearHoverHighlight()
    {
        if (hoveredSoilIndex != -1)
        {
            Transform oldSoilT = GetSoilTransform(hoveredSoilIndex);
            if (oldSoilT != null)
            {
                Soil oldSoil = oldSoilT.GetComponent<Soil>();
                if (oldSoil != null) oldSoil.SetHighlight(false);
            }
            hoveredSoilIndex = -1;
        }
    }


    public void SetBreedTimerUI(TimerUI timerUI)
    {
        breedTimerUI = timerUI;
    }

    public TimerUI GetBreedTimerUI()
    {
        return breedTimerUI;
    }

    /// <summary>
    /// 현재 밭 상태와 누적 보정치를 저장 데이터에 담는다. <see cref="LoadGrid"/>와 짝이므로
    /// 한쪽에 항목을 추가하면 반드시 다른 쪽도 같이 고칠 것.
    /// </summary>
    public void SaveGrid(GridSave save)
    {
        // 밭 / 교배
        save.plantList.Clear();
        foreach (var p in plantGrid.Values)
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
        save.maxCol = maxCol;
        save.maxBreedTimer = maxBreedTimer;
        save.maxBreedCount = maxBreedCount;

        save.totalBreedCount = totalBreedCount;
        save.totalPeaBreedcount = totalPeaBreedcount;
        save.totalPeanutBreedCount = totalPeanutBreedCount;
        save.mostExpensivePlant = mostExpensivePlant;

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
        // 로딩 중 OnGridStateChanged 억제 (MoneyTree 등이 로드 순서에 따라 조기 사망하는 것 방지)
        isLoadingGrid = true;

        List<PlantData> plantList = saveData.plantList;
        foreach (var item in plantList)
        {
            GameObject obj = InstantiatePlant(item.speciesname);

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

        // 모든 식물 로드 완료 후 OnGridStateChanged 한 번 발송
        isLoadingGrid = false;
        OnGridStateChanged?.Invoke();
        maxCol = saveData.maxCol;
        UpdateSoil();
        Soil.LoadColumnGoldMulBonus(saveData.columnGoldMulBonusList); // 특수(땅부자) 세로줄 배수 복원
        killBugCount = saveData.killBugCount;
        totalBreedCount = saveData.totalBreedCount;
        totalPeaBreedcount = saveData.totalPeaBreedcount;
        totalPeanutBreedCount = saveData.totalPeanutBreedCount;

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
        foreach (var plant in plantGrid.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromone();
                nepenthes.UpdatePheromoneSize();
            }
        }



        maxBreedTimer = saveData.maxBreedTimer;
        maxBreedCount = saveData.maxBreedCount;

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

        RefreshAllPlantSellingPriceDisplays();
        mostExpensivePlant = saveData.mostExpensivePlant;
    }

    public void SetNepenthesPheromoneEnabled(bool enabled)
    {
        Modifiers.HasNepenthesPheromone = enabled;
        // 기존 네펜데스들의 페로몬 활성화/비활성화
        foreach (var plant in plantGrid.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromone();
            }
        }
    }


    public void AddAdditionalNepenthesPheromoneSizeMultiplier(float value)
    {
        Modifiers.AdditionalNepenthesPheromoneSizeMultiplier += value;
        // 기존 네펜데스들의 페로몬 크기 업데이트
        foreach (var plant in plantGrid.Values)
        {
            if (plant is Nepenthes nepenthes)
            {
                nepenthes.UpdatePheromoneSize();
            }
        }
    }

    public void AddResistanceBonus(float value)
    {
        Modifiers.ResistanceBonus += value;
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
        Modifiers.WeakGeneticsResistanceBonus += value;
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
        Modifiers.StrongGeneticsResistanceBonus += value;
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

    public void AddSprinklerFertilizerSynergyBonus(float val)
    {
        Modifiers.SprinklerFertilizerSynergyBonus += val;
        Scouter.UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
    }

    public bool IsAffectedBySprinkler(int targetIdx)
    {
        int range = 1 + Modifiers.SprinklerRangeBonus;

        foreach (var kvp in plantGrid)
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

    // 급속 냉각기 관련
    public void ApplyRapidFreezer(int centerIdx)
    {
        int centerCol = centerIdx / 4;
        int centerRow = centerIdx % 4;

        // 3x3 범위 순회
        for (int c = centerCol - 1; c <= centerCol + 1; c++)
        {
            for (int r = centerRow - 1; r <= centerRow + 1; r++)
            {
                if (c >= 0 && c < GetMaxCol() && r >= 0 && r < 4)
                {
                    int targetIdx = c * 4 + r;
                    if (plantGrid.ContainsKey(targetIdx))
                    {
                        plantGrid[targetIdx].SetFrozen(true);

                        if (plantGrid[targetIdx].gameObject == breedObj1)
                        {
                            plantGrid[targetIdx].MakeDefaultSprite();
                            breedObj1 = null;
                            breedButton.SetActive(false);
                            breedButtonPlant = null;
                        }
                        if (plantGrid[targetIdx].gameObject == breedObj2)
                        {
                            plantGrid[targetIdx].MakeDefaultSprite();
                            breedObj2 = null;
                            breedButton.SetActive(false);
                            breedButtonPlant = null;
                        }
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
        foreach (var plant in plantGrid.Values)
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
        if (plantGrid.TryGetValue(gridIndex, out Plant plant))
        {
            return plant.IsFrozen();
        }
        return false;
    }

    // === 저항력 흡수 비료 ===
    public void AddChiliPepperRangeLevel(int value)
    {
        Modifiers.ChiliPepperRangeLevel = Mathf.Min(2, Modifiers.ChiliPepperRangeLevel + value); // 최대 2단계
        Scouter.UpdateResistanceScouterImageInGrid(enemyController.CurrentWave.WaveType);
    }

    /// <summary>
    /// 고추 주변의 식물들을 반환합니다 (고추 제외)
    /// </summary>
    public List<Plant> GetPlantsNearChiliPepper(ChiliPepper chiliPepper)
    {
        List<Plant> nearbyPlants = new List<Plant>();
        if (chiliPepper == null) return nearbyPlants;

        int chiliIndex = chiliPepper.gridIndex;
        int rangeLevel = Modifiers.ChiliPepperRangeLevel;

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

    public Dictionary<Plant, int> CapturePlantSellingPrices()
    {
        var previousPrices = new Dictionary<Plant, int>();
        foreach (Plant plant in plantGrid.Values)
        {
            if (plant == null || plant.isDying)
                continue;

            previousPrices[plant] = plant.GetSellingPrice();
        }

        return previousPrices;
    }

    public void RefreshPlantSellingPricesAndNotify(
        Dictionary<Plant, int> previousPrices,
        PlantValueChangeReason reason)
    {
        if (previousPrices == null)
            return;

        foreach (KeyValuePair<Plant, int> entry in previousPrices)
        {
            Plant plant = entry.Key;
            if (plant == null || plant.isDying)
                continue;

            plant.RefreshSellingPriceAndNotifyExternal(entry.Value, reason);
        }
    }

    public void RefreshAllPlantSellingPriceDisplays()
    {
        foreach (Plant plant in plantGrid.Values)
        {
            if (plant != null && !plant.isDying)
                plant.RefreshSellingPriceDisplay();
        }
    }

    public void AddAdditionalPlantGold(int value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        Modifiers.AdditionalPlantGold += value;
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }

    public void AddAdditionalPlantGoldMultiplier(float value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        Modifiers.AdditionalPlantGoldMultiplier += value;
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }

    // ───── 신규 아이템 ─────

    /// <summary>완두커피: 자유시간이 지날 때마다 붙는 판매 골드 배수 증가.</summary>
    public void AddPeaCoffeeMultiplier(float value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        Modifiers.PeaCoffeeMultiplier += value;
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }

    /// <summary>땅과 콩: 웨이브 후 뿌리 확률 +10%p/구매 + 뿌리내린 식물 가격 +10%p/구매.</summary>
    public void AddLandAndBeanLevel(int value)
    {
        Dictionary<Plant, int> previousPrices = CapturePlantSellingPrices();
        Modifiers.LandAndBeanLevel += value;
        RefreshPlantSellingPricesAndNotify(previousPrices, PlantValueChangeReason.UpgradeBonus);
    }

    /// <summary>
    /// 땅과 콩: 웨이브가 지나간 후, 아직 뿌리내리지 않은 식물이 확률적으로 뿌리를 내려 이동 불가가 된다.
    /// (GameManager.BreedEndRoutine에서 웨이브 정산 시 호출)
    /// </summary>
    public void ProcessLandAndBeanRooting()
    {
        float chance = Modifiers.GetLandAndBeanRootChance();
        if (chance <= 0f) return;

        foreach (var p in plantGrid.Values)
        {
            if (p is MovablePlant mp && mp.IsMovable && !mp.isDying)
            {
                if (Random.value < chance)
                    mp.SetMovable(false, PlantValueChangeReason.Rooted); // TODO: 뿌리 시각효과
            }
        }
    }

    public void SetWeatherForecast(bool val)
    {
        hasWeatherForecast = val;
    }

    public bool GetHasWeatherForecast()
    {
        return hasWeatherForecast;
    }





    //-----전용 비료------

    public bool HasBreedablePlantAt(int idx)
    {
        if (!plantGrid.ContainsKey(idx)) return false;

        if (plantGrid[idx].GetType() == typeof(Pea) || plantGrid[idx].GetType() == typeof(Peanut))
            return true;
        return false;
    }


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
        foreach (var p in plantGrid)
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

    public void TryUpdateMostExpensivePlant(int price)
    {
        if (mostExpensivePlant < price)
        {
            mostExpensivePlant = price;
        }
        return;
    }

}


