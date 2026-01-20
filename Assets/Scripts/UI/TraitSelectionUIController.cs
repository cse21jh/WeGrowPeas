using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum SelectionMode
{
    Trait,  // 형질 선택 (다중 선택)
    Wave    // 웨이브 선택 (단일 선택)
}

public class TraitSelectionUIController : MonoBehaviour
{
    public static TraitSelectionUIController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform popupParent;
    [SerializeField] private Transform traitButtonParent;
    [SerializeField] private GameObject traitButtonPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private SelectionMode currentMode = SelectionMode.Trait;
    private List<GeneticTrait> selectedTraits = new List<GeneticTrait>();
    private WaveType? selectedWave = null;
    private Dictionary<TraitType, Button> traitButtons = new Dictionary<TraitType, Button>();
    private Dictionary<WaveType, Button> waveButtons = new Dictionary<WaveType, Button>();
    private System.Action<List<GeneticTrait>> onTraitConfirmCallback;
    private System.Action<WaveType> onWaveConfirmCallback;
    private System.Action onCancelCallback;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("TraitSelectionUIController: 중복 인스턴스가 감지되었습니다. 기존 인스턴스를 유지합니다.");
            Destroy(gameObject);
        }
    }

    public void ShowTraitSelection(
        System.Action<List<GeneticTrait>> onConfirm, 
        System.Action onCancel)
    {
        currentMode = SelectionMode.Trait;
        this.onTraitConfirmCallback = onConfirm;
        this.onCancelCallback = onCancel;
        selectedTraits.Clear();
        selectedWave = null;
        traitButtons.Clear();
        waveButtons.Clear();
        
        if (popupParent != null)
        {
            popupParent.gameObject.SetActive(true);
            popupParent.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
        
        CreateTraitButtons();
        
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => {
                PhoneManager.Instance.PhoneTouchEffect();
                OnTraitConfirm();
            });
            confirmButton.interactable = true; // 형질은 선택 전에도 확인 가능
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => {
                PhoneManager.Instance.PhoneTouchEffect();
                OnCancel();
            });
        }

        if (titleText != null)
            titleText.text = "형질을 선택하세요";
    }

    /// <summary>
    /// 단일 형질 선택 UI를 표시합니다 (완두콩 구매 시 사용).
    /// 형질을 하나만 선택할 수 있으며, 선택하지 않으면 확인이 불가능합니다.
    /// </summary>
    public void ShowSingleTraitSelection(
        System.Action<List<GeneticTrait>> onConfirm,
        System.Action onCancel)
    {
        currentMode = SelectionMode.Trait;
        this.onTraitConfirmCallback = onConfirm;
        this.onCancelCallback = onCancel;
        selectedTraits.Clear();
        selectedWave = null;
        traitButtons.Clear();
        waveButtons.Clear();
        
        if (popupParent != null)
        {
            popupParent.gameObject.SetActive(true);
            popupParent.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
        
        CreateSingleTraitButtons();
        
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => {
                PhoneManager.Instance.PhoneTouchEffect();
                OnTraitConfirm();
            });
            confirmButton.interactable = false; // 형질 선택 전에는 비활성화
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => {
                PhoneManager.Instance.PhoneTouchEffect();
                OnCancel();
            });
        }

        if (titleText != null)
            titleText.text = "형질을 하나 선택하세요";
    }

    public void ShowWaveSelection(
        System.Action<WaveType> onConfirm,
        System.Action onCancel,
        string title = "웨이브를 선택하세요")
    {
        currentMode = SelectionMode.Wave;
        this.onWaveConfirmCallback = onConfirm;
        this.onCancelCallback = onCancel;
        selectedTraits.Clear();
        selectedWave = null;
        traitButtons.Clear();
        waveButtons.Clear();
        
        if (popupParent != null)
        {
            popupParent.gameObject.SetActive(true);
            popupParent.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
        
        CreateWaveButtons();
        
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => {
                PhoneManager.Instance.PhoneTouchEffect();
                OnWaveConfirm();
            });
            confirmButton.interactable = false; // 웨이브는 선택 전에는 비활성화
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => {
                PhoneManager.Instance.PhoneTouchEffect();
                OnCancel();
            });
        }

        if (titleText != null)
            titleText.text = title;
    }

    private void CreateTraitButtons()
    {
        if (traitButtonParent == null || traitButtonPrefab == null) return;

        // 기존 버튼 제거
        foreach (Transform child in traitButtonParent)
            Destroy(child.gameObject);

        // 현재 스테이지 가져오기
        int currentStage = GameManager.Instance != null ? GameManager.Instance.stage : 0;

        // 모든 형질 타입에 대한 버튼 생성 (None 제외, 해금된 것만)
        foreach (TraitType traitType in System.Enum.GetValues(typeof(TraitType)))
        {
            if (traitType == TraitType.None) continue;
            
            // 해금 여부 확인
            if (!IsTraitUnlocked(traitType, currentStage)) continue;
            
            var btnObj = Instantiate(traitButtonPrefab, traitButtonParent);
            var btn = btnObj.GetComponent<Button>();
            var btnText = btnObj.GetComponentInChildren<TMP_Text>();
            
            if (btnText != null)
                btnText.text = GetTraitDisplayName(traitType);
            
            if (btn != null)
            {
                // 토글 버튼으로 동작
                TraitType currentTrait = traitType; // 클로저를 위한 로컬 변수
                bool isSelected = false;
                
                btn.onClick.AddListener(() => {
                    PhoneManager.Instance.PhoneTouchEffect();
                    isSelected = !isSelected;
                    ToggleTrait(currentTrait, isSelected);
                    UpdateButtonVisual(btn, isSelected);
                });
                
                traitButtons[currentTrait] = btn;
            }
        }
    }

    /// <summary>
    /// 단일 선택 형질 버튼들을 생성합니다.
    /// 일반 완두콩은 추위/더위, 폭우/가뭄을 쌍으로 표시합니다.
    /// </summary>
    private void CreateSingleTraitButtons()
    {
        if (traitButtonParent == null || traitButtonPrefab == null) return;

        // 기존 버튼 제거
        foreach (Transform child in traitButtonParent)
            Destroy(child.gameObject);

        // 현재 스테이지 가져오기
        int currentStage = GameManager.Instance != null ? GameManager.Instance.stage : 0;

        // 이미 추가된 쌍 형질을 추적 (중복 방지)
        HashSet<TraitType> processedPairs = new HashSet<TraitType>();

        // 모든 형질 타입에 대한 버튼 생성 (None 제외, 해금된 것만)
        foreach (TraitType traitType in System.Enum.GetValues(typeof(TraitType)))
        {
            if (traitType == TraitType.None) continue;
            
            // 해금 여부 확인
            if (!IsTraitUnlocked(traitType, currentStage)) continue;
            
            // 쌍 형질 처리 (추위/더위, 폭우/가뭄)
            if (traitType == TraitType.Cold || traitType == TraitType.Heat)
            {
                // 이미 처리했으면 스킵
                if (processedPairs.Contains(TraitType.Cold) || processedPairs.Contains(TraitType.Heat))
                    continue;
                
                processedPairs.Add(TraitType.Cold);
                processedPairs.Add(TraitType.Heat);
                
                var btnObj = Instantiate(traitButtonPrefab, traitButtonParent);
                var btn = btnObj.GetComponent<Button>();
                var btnText = btnObj.GetComponentInChildren<TMP_Text>();
                
                if (btnText != null)
                    btnText.text = "추위/더위";
                
                if (btn != null)
                {
                    btn.onClick.AddListener(() => {
                        PhoneManager.Instance.PhoneTouchEffect();
                        SelectPairedTrait(TraitType.Cold, TraitType.Heat);
                        UpdateAllTraitButtonVisuals();
                    });
                    
                    // Cold를 키로 사용 (시각적 업데이트용)
                    traitButtons[TraitType.Cold] = btn;
                }
                continue;
            }
            
            if (traitType == TraitType.HeavyRain || traitType == TraitType.Drought)
            {
                // 이미 처리했으면 스킵
                if (processedPairs.Contains(TraitType.HeavyRain) || processedPairs.Contains(TraitType.Drought))
                    continue;
                
                processedPairs.Add(TraitType.HeavyRain);
                processedPairs.Add(TraitType.Drought);
                
                var btnObj = Instantiate(traitButtonPrefab, traitButtonParent);
                var btn = btnObj.GetComponent<Button>();
                var btnText = btnObj.GetComponentInChildren<TMP_Text>();
                
                if (btnText != null)
                    btnText.text = "폭우/가뭄";
                
                if (btn != null)
                {
                    btn.onClick.AddListener(() => {
                        PhoneManager.Instance.PhoneTouchEffect();
                        SelectPairedTrait(TraitType.HeavyRain, TraitType.Drought);
                        UpdateAllTraitButtonVisuals();
                    });
                    
                    // HeavyRain을 키로 사용 (시각적 업데이트용)
                    traitButtons[TraitType.HeavyRain] = btn;
                }
                continue;
            }
            
            // 일반 형질 (NaturalDeath, Pest, Wind, Flood)
            var btnObj2 = Instantiate(traitButtonPrefab, traitButtonParent);
            var btn2 = btnObj2.GetComponent<Button>();
            var btnText2 = btnObj2.GetComponentInChildren<TMP_Text>();
            
            if (btnText2 != null)
                btnText2.text = GetTraitDisplayName(traitType);
            
            if (btn2 != null)
            {
                // 단일 선택 버튼으로 동작
                TraitType currentTrait = traitType; // 클로저를 위한 로컬 변수
                
                btn2.onClick.AddListener(() => {
                    PhoneManager.Instance.PhoneTouchEffect();
                    SelectSingleTrait(currentTrait);
                    UpdateAllTraitButtonVisuals();
                });
                
                traitButtons[currentTrait] = btn2;
            }
        }
    }

    private bool IsTraitUnlocked(TraitType traitType, int currentStage)
    {
        // stage + 2 >= UnlockStage 조건으로 해금 여부 확인
        // (EnemyController.InitBaseWeightsByStage 로직과 동일)
        
        switch (traitType)
        {
            case TraitType.NaturalDeath:
                // Aging은 항상 해금
                return true;
                
            case TraitType.Pest:
                return (currentStage + 2) >= PestWave.UnlockStage;
                
            case TraitType.Wind:
                return (currentStage + 2) >= WindWave.UnlockStage;
                
            case TraitType.Flood:
                return (currentStage + 2) >= FloodWave.UnlockStage;
                
            case TraitType.HeavyRain:
                return (currentStage + 2) >= HeavyRainWave.UnlockStage;
                
            case TraitType.Drought:
                return (currentStage + 2) >= DroughtWave.UnlockStage;
                
            case TraitType.Cold:
                return (currentStage + 2) >= ColdWave.UnlockStage;
                
            case TraitType.Heat:
                return (currentStage + 2) >= HeatWave.UnlockStage;
                
            default:
                return false;
        }
    }

    private void ToggleTrait(TraitType traitType, bool add)
    {
        if (add)
        {
            // 기본 저항력 0.5f, 유전자 0으로 추가
            selectedTraits.Add(new GeneticTrait(traitType, 0.5f, 0, 0.0f));
        }
        else
        {
            selectedTraits.RemoveAll(t => t.traitType == traitType);
        }
    }

    /// <summary>
    /// 단일 형질을 선택합니다 (기존 선택은 해제됨).
    /// </summary>
    private void SelectSingleTrait(TraitType traitType)
    {
        selectedTraits.Clear();
        // 기본 저항력 0.5f, 유전자 1로 추가 (완두콩 구매 시 형질 보장)
        selectedTraits.Add(new GeneticTrait(traitType, 0.5f, 1, 0.0f));
        
        if (confirmButton != null)
            confirmButton.interactable = true; // 선택 후 확인 버튼 활성화
    }

    /// <summary>
    /// 쌍 형질을 선택합니다 (추위/더위, 폭우/가뭄).
    /// </summary>
    private void SelectPairedTrait(TraitType traitA, TraitType traitB)
    {
        selectedTraits.Clear();
        // 기본 저항력 0.5f, 유전자 0으로 둘 다 추가
        selectedTraits.Add(new GeneticTrait(traitA, 0.5f, 0, 0.0f));
        selectedTraits.Add(new GeneticTrait(traitB, 0.5f, 0, 0.0f));
        
        if (confirmButton != null)
            confirmButton.interactable = true; // 선택 후 확인 버튼 활성화
    }

    /// <summary>
    /// 모든 형질 버튼의 시각적 상태를 업데이트합니다 (단일 선택용).
    /// </summary>
    private void UpdateAllTraitButtonVisuals()
    {
        if (selectedTraits.Count == 0) return;
        
        // 선택된 형질 타입들 추출
        HashSet<TraitType> selectedTypes = new HashSet<TraitType>();
        foreach (var trait in selectedTraits)
        {
            selectedTypes.Add(trait.traitType);
        }
        
        foreach (var kvp in traitButtons)
        {
            bool isSelected = false;
            
            // 쌍 형질 버튼 확인 (Cold 버튼은 Cold/Heat 둘 다 체크, HeavyRain 버튼은 HeavyRain/Drought 둘 다 체크)
            if (kvp.Key == TraitType.Cold)
            {
                isSelected = selectedTypes.Contains(TraitType.Cold) || selectedTypes.Contains(TraitType.Heat);
            }
            else if (kvp.Key == TraitType.HeavyRain)
            {
                isSelected = selectedTypes.Contains(TraitType.HeavyRain) || selectedTypes.Contains(TraitType.Drought);
            }
            else
            {
                // 일반 형질은 정확히 일치하는지 확인
                isSelected = selectedTypes.Contains(kvp.Key);
            }
            
            UpdateButtonVisual(kvp.Value, isSelected);
        }
    }

    private void UpdateButtonVisual(Button btn, bool isSelected)
    {
        if (btn == null) return;
        
        var colors = btn.colors;
        Color targetColor = isSelected ? Color.green : Color.white;
        colors.normalColor = targetColor;
        colors.highlightedColor = targetColor;
        colors.pressedColor = targetColor;
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor;
        btn.colors = colors;
    }

    private string GetTraitDisplayName(TraitType traitType)
    {
        // 형질 이름 반환 (필요시 로컬라이제이션 추가 가능)
        switch (traitType)
        {
            case TraitType.NaturalDeath: return "자연사";
            case TraitType.Pest: return "해충";
            case TraitType.Wind: return "바람";
            case TraitType.Flood: return "홍수";
            case TraitType.HeavyRain: return "폭우";
            case TraitType.Cold: return "추위";
            case TraitType.Drought: return "가뭄";
            case TraitType.Heat: return "더위";
            default: return traitType.ToString();
        }
    }

    private void OnTraitConfirm()
    {
        // 단일 선택 모드에서는 형질이 선택되지 않았으면 확인 불가
        // (확인 버튼이 비활성화되어 있으므로 일반적으로 호출되지 않지만, 안전장치로 확인)
        if (selectedTraits == null || selectedTraits.Count == 0)
        {
            Debug.LogWarning("형질이 선택되지 않았습니다.");
            return;
        }

        // 형질이 제대로 선택되었는지 확인
        Debug.Log($"[TraitSelectionUI] 형질 확인: {selectedTraits.Count}개 형질 선택됨");
        foreach (var trait in selectedTraits)
        {
            Debug.Log($"[TraitSelectionUI] - {trait.traitType}, genetics: {trait.genetics}, resistance: {trait.resistance}");
        }

        // selectedTraits의 복사본을 만들어서 전달 (참조 문제 방지)
        List<GeneticTrait> traitsCopy = new List<GeneticTrait>(selectedTraits);

        if (popupParent != null)
        {
            popupParent.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                popupParent.gameObject.SetActive(false);
                onTraitConfirmCallback?.Invoke(traitsCopy);
            });
        }
        else
        {
            onTraitConfirmCallback?.Invoke(traitsCopy);
        }
    }

    private void OnWaveConfirm()
    {
        if (!selectedWave.HasValue)
        {
            Debug.LogWarning("웨이브가 선택되지 않았습니다.");
            return;
        }

        if (popupParent != null)
        {
            popupParent.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                popupParent.gameObject.SetActive(false);
                onWaveConfirmCallback?.Invoke(selectedWave.Value);
            });
        }
        else
        {
            onWaveConfirmCallback?.Invoke(selectedWave.Value);
        }
    }

    private void OnCancel()
    {
        if (popupParent != null)
        {
            popupParent.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                popupParent.gameObject.SetActive(false);
                onCancelCallback?.Invoke();
            });
        }
        else
        {
            onCancelCallback?.Invoke();
        }
    }

    private void CreateWaveButtons()
    {
        if (traitButtonParent == null || traitButtonPrefab == null) return;

        // 기존 버튼 제거
        foreach (Transform child in traitButtonParent)
            Destroy(child.gameObject);

        // 현재 스테이지 가져오기
        int currentStage = GameManager.Instance != null ? GameManager.Instance.stage : 0;

        // 모든 웨이브 타입에 대한 버튼 생성 (None 제외, 해금된 것만)
        foreach (WaveType waveType in System.Enum.GetValues(typeof(WaveType)))
        {
            if (waveType == WaveType.None) continue;

            // 해금 여부 확인
            if (!IsWaveUnlocked(waveType, currentStage)) continue;

            var btnObj = Instantiate(traitButtonPrefab, traitButtonParent);
            var btn = btnObj.GetComponent<Button>();
            var btnText = btnObj.GetComponentInChildren<TMP_Text>();

            if (btnText != null)
                btnText.text = GetWaveDisplayName(waveType);

            if (btn != null)
            {
                // 단일 선택 버튼으로 동작
                WaveType currentWave = waveType; // 클로저를 위한 로컬 변수

                btn.onClick.AddListener(() => {
                    PhoneManager.Instance.PhoneTouchEffect();
                    SelectWave(currentWave);
                    UpdateAllWaveButtonVisuals();
                });

                waveButtons[currentWave] = btn;
            }
        }
    }

    private bool IsWaveUnlocked(WaveType waveType, int currentStage)
    {
        // EnemyController.InitBaseWeightsByStage 로직 참고
        // stage + 2 >= UnlockStage 조건으로 해금 여부 확인
        
        switch (waveType)
        {
            case WaveType.Aging:
                return true; // 항상 해금

            case WaveType.Pest:
                return (currentStage + 2) >= PestWave.UnlockStage;

            case WaveType.Wind:
                return (currentStage + 2) >= WindWave.UnlockStage;

            case WaveType.Flood:
                return (currentStage + 2) >= FloodWave.UnlockStage;

            case WaveType.HeavyRain:
                return (currentStage + 2) >= HeavyRainWave.UnlockStage;

            case WaveType.Drought:
                return (currentStage + 2) >= DroughtWave.UnlockStage;

            case WaveType.Cold:
                return (currentStage + 2) >= ColdWave.UnlockStage;

            case WaveType.Heat:
                return (currentStage + 2) >= HeatWave.UnlockStage;

            default:
                return false;
        }
    }

    private void SelectWave(WaveType waveType)
    {
        selectedWave = waveType;
        
        if (confirmButton != null)
            confirmButton.interactable = true; // 선택 후 확인 버튼 활성화
    }

    private void UpdateAllWaveButtonVisuals()
    {
        foreach (var kvp in waveButtons)
        {
            UpdateWaveButtonVisual(kvp.Value, kvp.Key == selectedWave);
        }
    }

    private void UpdateWaveButtonVisual(Button btn, bool isSelected)
    {
        if (btn == null) return;

        var colors = btn.colors;
        colors.normalColor = isSelected ? Color.green : Color.white;
        btn.colors = colors;
    }

    private string GetWaveDisplayName(WaveType waveType)
    {
        // 웨이브 이름 반환
        switch (waveType)
        {
            case WaveType.Aging: return "자연사";
            case WaveType.Pest: return "해충";
            case WaveType.Wind: return "바람";
            case WaveType.Flood: return "홍수";
            case WaveType.HeavyRain: return "폭우";
            case WaveType.Cold: return "추위";
            case WaveType.Drought: return "가뭄";
            case WaveType.Heat: return "더위";
            default: return waveType.ToString();
        }
    }

    public void Hide()
    {
        if (popupParent != null)
            popupParent.gameObject.SetActive(false);
    }
}
