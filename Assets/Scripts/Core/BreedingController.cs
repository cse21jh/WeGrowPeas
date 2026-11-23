using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    internal RectTransform BreedButtonRect => breedButton.transform as RectTransform;
    internal RectTransform BreedSkipRect => breedSkipButton.transform as RectTransform;
    internal RectTransform BreedCountRect => breedCountUI.rectTransform;
    internal event System.Action OnParentSelected;

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

    /// <summary>그리드를 준비할 때 첫 교배가 시작되기 전에 호출한다.</summary>
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
            int maxAttempts = GetEffectiveMaxBreedCount();
            BreedCheck check = BreedingRules.Check(grid.Field, parent1.State, parent2.State, breedCount, maxAttempts);

            if (check == BreedCheck.Ok)
            {
                // 무엇이 태어나는지는 BreedingRules가 정하고, 여기서는 심기와 연출을 한다
                BreedOutcome outcome = BreedingRules.Breed(grid.Field, parent1.State, parent2.State,
                    GameManager.Instance.currentPlant, grid.Enemy.CurrentSeason, SpawnChild);

                if (outcome.ConsumesAttempt) breedCount++;

                if (outcome.Failed)
                {
                    Debug.Log("씨 없는 수박 저주로 교배 실패");
                    SoundManager.Instance.PlayEffect(SoundIds.WrongSelect); // TODO: 실패 전용 효과음/연기 이펙트
                }
                else
                {
                    GameEvents.RaisePeaBreeded();
                    Debug.Log("자식 생성 성공. 남은 교배 횟수는 " + (maxAttempts - breedCount) + "입니다");
                    SoundManager.Instance.PlayEffect(SoundIds.Breed);
                    CountBreed();
                }

                UpdateBreedCountUI(maxAttempts - breedCount);
                parent1.MakeDefaultSprite();
                parent2.MakeDefaultSprite();
                breedObj1 = null;
                breedObj2 = null;
                DeactivateBreed();
            }
            else if (check == BreedCheck.NoAttemptsLeft)
            {
                UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.no_attempts"), 1);
                SoundManager.Instance.PlayEffect(SoundIds.WrongSelect);
                isBreedButtonPressed = false;
            }
            else if (check == BreedCheck.NoSpace) // TODO: 아래 두 안내 문구가 서로 바뀌어 있다 (기존 동작 유지)
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
        TutorialChapter tutorial = GameManager.Instance.Tutorial;
        bool untimedPractice = tutorial != null && tutorial.IsUntimedDay;
        //breedTimer 만큼 동안 아래 과정 반복 진행 가능
        if (!untimedPractice) grid.Enemy.SetCurrentWaveTimer();
        //교배할 부모 완두콩 두 개 선택
        isBreeding = true;
        breedObj1 = null;
        breedObj2 = null;

        //int breedCount = 0;
        int effectiveMaxBreedCount = GetEffectiveMaxBreedCount();
        // 저주: 불면증 — 밤시간(폰 페이즈) 단축으로 변경되어 교배시간 영향 제거
        float effectiveMaxBreedTimer = tutorial != null ? TutorialChapter.DaySeconds
            : MaxBreedTimer * ModManager.Instance.GetMul(StatId.BreedingPhaseDurationMul, -1);

        //임시 알람
        bool _warned15s = false;

        //Debug.Log(effectiveMaxBreedTimer + "초 시작. 최대 교배 횟수는 " + effectiveMaxBreedCount + "입니다");
        UpdateBreedCountUI(effectiveMaxBreedCount);
        breedTimer = effectiveMaxBreedTimer;
        if (!untimedPractice) breedTimerUI.StartBreedingTimer();

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
            foreach (var p in grid.Board.Plants.Values)
                if (p != null) p.MarkDayStartPosition();

        Button practiceSkipButton = untimedPractice ? breedSkipButton.GetComponent<Button>() : null;
        if (practiceSkipButton != null)
            practiceSkipButton.interactable = tutorial.CanSkipDay && !GameManager.Instance.GetGameIsStopped();
        breedSkipButton.SetActive(true);
        if (tutorial == null) grid.Enemy.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;

        if (tutorial?.CanSpawnBugs != false) grid.Bugs.BeginDay();

        while ((untimedPractice || breedTimer > 0) && !isBreedSkipButtonPressed)
        {
            bool gameIsStopped = GameManager.Instance.GetGameIsStopped();
            if (practiceSkipButton != null) practiceSkipButton.interactable = tutorial.CanSkipDay && !gameIsStopped;
            if (gameIsStopped && tutorial?.CanBreedDuringOpeningMessage != true)
            {
                yield return null;
                continue;
            }

            if (!untimedPractice) breedTimer -= Time.deltaTime;

            if (tutorial == null && breedTimer < 15f && !_warned15s)
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

            if (GameInput.GetDown(GameAction.Skip))
            {
                SkipBreed();
            }

            if (tutorial == null && GameInput.GetDown(GameAction.WaveSkip))
            {
                grid.Enemy.WaveSkip();
            }

            if (tutorial?.CanSpawnBugs != false) grid.Bugs.Tick(Time.deltaTime);

            if (isBreedButtonPressed || GameInput.GetDown(GameAction.Breed))
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

        if (!untimedPractice) breedTimerUI.StopTimer();

        int currentEffectiveMaxBreedCount = GetEffectiveMaxBreedCount();
        int remainingBreeds = Mathf.Max(0, currentEffectiveMaxBreedCount - breedCount);
        GameEvents.RaiseDayEndedWithRemainingBreeds(remainingBreeds);

        breedCount = 0;
        // Debug.Log("교배 페이즈 종료");
        breedButton.SetActive(false);
        breedButtonPlant = null;
        grid.Enemy.HideWaveSkipButton();
        isBreeding = false;
        breedSkipButton.SetActive(false);
        if (practiceSkipButton != null) practiceSkipButton.interactable = true;

        // 저주(통신장애): 자유시간이 끝나면(스킵 포함) 노이즈도 바로 끈다
        if (PhoneManager.Instance != null) PhoneManager.Instance.EndEmpBlock();

        // 완두커피: 자유시간이 지난 식물은 조금 더 비싸짐
        if (grid.Modifiers.PeaCoffeeMultiplier > 0f)
        {
            foreach (var p in grid.Board.Plants.Values)
                if (p != null) p.OnFreeTimePassed();
        }
        //GardenGrid 리로드

        yield return null;
    }
    private void SpawnChild(List<GeneticTrait> traits, MutationKind mutation, bool isTwin)
    {
        Plant plant = grid.Spawner.AddMovablePlant(traits);

        if (mutation != MutationKind.None)
        {
            Debug.Log($"[변종] {(mutation == MutationKind.Malignant ? "악성" : "양성")} 변종 발생!");
            if (plant != null) CurseEffectManager.PlayMutation(plant, mutation == MutationKind.Benign);
        }

        if (isTwin)
        {
            // 교배 팝업은 인스턴스를 하나만 재사용하므로 쌍둥이는 플로팅 팝업으로만 알림
            UIManager.Instance.Popup.ShowFloatingPopup(Loc.T("game.breed.twins"), 1);
            CountBreed();
        }
        else if (UIManager.Instance != null && UIManager.Instance.ShowBreedPopupSetting &&
                 GameManager.Instance.Tutorial?.CanBreedDuringOpeningMessage != true)
        {
            UIManager.Instance.Popup.ShowBreedPopup(plant);
        }
    }

    private void CountBreed()
    {
        totalBreedCount++;
        if (GameManager.Instance.currentPlant == PlantIds.Pea)
            totalPeaBreedcount++;
        else if (GameManager.Instance.currentPlant == PlantIds.Peanut)
            totalPeanutBreedCount++;
    }

    private int GetEffectiveMaxBreedCount()
        => BreedingRules.GetMaxAttempts(maxBreedCount,
            ModManager.Instance.GetMul(StatId.BreedingAttemptsMul, -1),
            GameManager.Instance != null ? GameManager.Instance.grid.Economy.TotalGold : 0);

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
        if (GameManager.Instance?.Tutorial != null) return TutorialChapter.DaySeconds;
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
        return Mathf.Max(0, GetEffectiveMaxBreedCount() - breedCount);
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
            OnParentSelected?.Invoke();
        }
        else if (breedObj2 == null)
        {
            // 부모 2 선택
            SoundManager.Instance.PlayEffect(SoundIds.SelectPlant);
            breedObj2 = clickedObject;
            clickedPea.MakeSelectedSprite();
            OnParentSelected?.Invoke();
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
        TutorialChapter tutorial = GameManager.Instance.Tutorial;
        if (tutorial != null && (!isBreeding || !tutorial.CanSkipDay)) return;

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
