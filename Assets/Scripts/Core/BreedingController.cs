using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>낮 교배 진행. 부모 선택, 버튼, 남은 횟수, 자식 형질 계산을 맡는다.</summary>
public class BreedingController : MonoBehaviour
{
    protected bool isBreeding = false;
    protected GameObject breedObj1 = null;
    protected GameObject breedObj2 = null;
    protected bool isBreedButtonPressed = false;
    protected bool isBreedSkipButtonPressed = false;
    protected float breedTimer;
    [SerializeField] protected TimerUI breedTimerUI;
    [SerializeField] protected GameObject breedButton;
    [SerializeField] protected GameObject breedSkipButton;
    [SerializeField] protected TextMeshProUGUI breedCountUI;
    protected Plant breedButtonPlant = null;
    public int totalBreedCount = 0;
    public int totalPeaBreedcount = 0;
    public int totalPeanutBreedCount = 0;
    protected float maxBreedTimer = 50.0f;
    protected int maxBreedCount = 4;
    protected int breedCount = 0;
    private int? shownBreedCount;
    public float MaxBreedTimer => maxBreedTimer;
    public int MaxBreedCount => maxBreedCount;
    public int BreedCount => breedCount; // 스테이지 단위 저장이라 아직 ㄱㅊ
    private int lastMutationKind = 0;

    /// <summary>직전 교배에서 변종이 발생했는가.</summary>
    public bool LastBreedWasMutation => lastMutationKind != 0;
    /// <summary>직전 변종이 양성이었는가. (변종이 아니면 의미 없음)</summary>
    public bool LastBreedWasBenign => lastMutationKind == 2;

    private Grid cachedGrid;
    protected Grid grid => cachedGrid != null ? cachedGrid : (cachedGrid = GetComponent<Grid>());

    /// <summary>얼어서 고를 수 없게 된 식물이 부모로 선택돼 있으면 선택을 푼다.</summary>
    public void DeselectPlant(Plant plant)
    {
        if (plant.gameObject == breedObj1)
        {
            plant.MakeDefaultSprite();
            breedObj1 = null;
            breedButton.SetActive(false);
            breedButtonPlant = null;
        }
        if (plant.gameObject == breedObj2)
        {
            plant.MakeDefaultSprite();
            breedObj2 = null;
            breedButton.SetActive(false);
            breedButtonPlant = null;
        }
    }

    /// <summary>Grid.Start에서 호출한다. 초기화 순서를 유지하려고 자체 Start를 쓰지 않는다.</summary>
    public virtual void Initialize()
    {
        breedButton.SetActive(false);
        breedButtonPlant = null;
        EconomyFeedbackController.BindBreedCountUi(breedCountUI);
    }

    private void Update()
    {
        if (breedButtonPlant != null)
        {
            MoveBreedButton(breedButtonPlant.transform.position);
        }
    }

    private void OnEnable() => Loc.LanguageChanged += ApplyBreedCountText;

    private void OnDisable() => Loc.LanguageChanged -= ApplyBreedCountText;

    public void Save(GridSave save)
    {
        save.maxBreedTimer = maxBreedTimer;
        save.maxBreedCount = maxBreedCount;
        save.totalBreedCount = totalBreedCount;
        save.totalPeaBreedcount = totalPeaBreedcount;
        save.totalPeanutBreedCount = totalPeanutBreedCount;
    }

    public void Load(GridSave saveData)
    {
        maxBreedTimer = saveData.maxBreedTimer;
        maxBreedCount = saveData.maxBreedCount;
        totalBreedCount = saveData.totalBreedCount;
        totalPeaBreedcount = saveData.totalPeaBreedcount;
        totalPeanutBreedCount = saveData.totalPeanutBreedCount;
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
            for (int idx = 0; idx < grid.maxCol * 4; idx++) // 빈 칸이 있는가
            {
                if (!grid.plantGrid.ContainsKey(idx))
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
                Plant plant = grid.AddMovablePlant(Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait(), madnessBreed));
                PlayMutationEffectIfAny(plant); // 변종(돌연변이) 이펙트
                if (UIManager.Instance != null && UIManager.Instance.ShowBreedPopupSetting)
                {
                    UIManager.Instance.Popup.ShowBreedPopup(plant);
                }

                // 쌍둥이: 확률적으로 교배를 한 번 더 실행해 유전자가 다른 이란성 쌍둥이를 생성
                if (grid.Modifiers.TwinBreedProbability > 0f && Random.Range(0f, 1f) < grid.Modifiers.TwinBreedProbability && grid.HasEmptyGrid())
                {
                    // 교배 팝업은 인스턴스를 하나만 재사용하므로 쌍둥이는 플로팅 팝업으로만 알림
                    PlayMutationEffectIfAny(
                        grid.AddMovablePlant(Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait(), madnessBreed)));
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
        grid.Enemy.SetCurrentWaveTimer();
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
            int refund = ShopManager.Instance.ConsumeDailyPurchaseRefund(grid.Modifiers.CreditCardRefundPercent);
            if (refund > 0)
            {
                GameManager.Instance.grid.Economy.AddGold(refund);
                UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.card_refund", ("gold", refund)), 1);
            }
        }

        // 특수(세계여행): 낮 시작 시 각 식물의 위치 마킹 (웨이브 후 이동 거리 계산용)
        if (SpecialItemSystem.Has("world_travel"))
            foreach (var p in grid.plantGrid.Values)
                if (p != null) p.MarkDayStartPosition();

        breedSkipButton.SetActive(true);
        grid.Enemy.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;

        grid.Bugs.BeginDay();

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
                grid.Enemy.WaveSkip();
            }

            grid.Bugs.Tick(Time.deltaTime);

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
        grid.Enemy.HideWaveSkipButton();
        isBreeding = false;
        breedSkipButton.SetActive(false);

        // 저주(통신장애): 자유시간이 끝나면(스킵 포함) 노이즈도 바로 끈다
        if (PhoneManager.Instance != null) PhoneManager.Instance.EndEmpBlock();

        // 완두커피: 자유시간이 지난 식물은 조금 더 비싸짐
        if (grid.Modifiers.PeaCoffeeMultiplier > 0f)
        {
            foreach (var p in grid.plantGrid.Values)
                if (p != null) p.OnFreeTimePassed();
        }
        //GardenGrid 리로드

        yield return null;
    }
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
            float malignantRatio = grid.Modifiers.HasSuperMutation ? 0.2f : 0.8f;
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
                    int p1Bonus = (int)(grid.Modifiers.GoldenGeneticsProbabilityBonus * 100); // 보너스를 퍼센트로 변환
                    childGenetic += (Random.Range(1, 101) <= (grid.Modifiers.AdditionalInheritance + 50 + p1Bonus) ? 1 : 0);
                    break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1:
                    // 황금 유전자 확률 보너스 적용: genetics = 1에서 우수한 형질이 나올 확률 증가
                    int p2Bonus = (int)(grid.Modifiers.GoldenGeneticsProbabilityBonus * 100); // 보너스를 퍼센트로 변환
                    childGenetic += (Random.Range(1, 101) <= (grid.Modifiers.AdditionalInheritance + 50 + p2Bonus) ? 1 : 0);
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
                childTrait.Add(new GeneticTrait(trait, resistance, childGenetic, grid.Modifiers.AdditionalPestResistance));
            else
                childTrait.Add(new GeneticTrait(trait, resistance, childGenetic, 0.0f));
        }
        return childTrait;
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
        Season season = grid.Enemy.CurrentSeason;
        bool coldSide = season == Season.Winter || season == Season.Fall;
        return coldSide ? 2 : 0;
    }
    /// <summary>특수(왕의 귀환): 기본 교배 횟수 -2, 누적 획득 골드 5000당 +1.</summary>
    private int GetKingReturnBreedDelta()
    {
        if (!SpecialItemSystem.Has("king_return")) return 0;
        int total = GameManager.Instance != null ? GameManager.Instance.grid.Economy.TotalGold : 0;
        return -2 + total / 5000;
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
        if (grid.Modifiers.TimeIsGoldLevel > 0)
        {
            if (breedTimer > 0.1f)
            {
                int reward = Mathf.CeilToInt(breedTimer) * 10 * grid.Modifiers.TimeIsGoldLevel;
                if (reward > 0)
                {
                    //GameManager.Instance.grid.Economy.AddGold(reward);
                    // AddGold에 로그가 있을 수 있으므로 여기서는 로그 생략하거나 필요시 추가
                    GameManager.Instance.grid.Economy.AddGold(reward);
                    Debug.Log($"[TimeIsGold] Success! Skipped. Remain: {breedTimer:F2}s, Level: {grid.Modifiers.TimeIsGoldLevel}, Reward: {reward}G");
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
    public bool GetIsBreeding()
    {
        return isBreeding;
    }
    public void SetBreedTimerUI(TimerUI timerUI)
    {
        breedTimerUI = timerUI;
    }
    public TimerUI GetBreedTimerUI()
    {
        return breedTimerUI;
    }
}
