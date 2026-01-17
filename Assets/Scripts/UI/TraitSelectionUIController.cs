using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TraitSelectionUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform popupParent;
    [SerializeField] private Transform traitButtonParent;
    [SerializeField] private GameObject traitButtonPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private List<GeneticTrait> selectedTraits = new List<GeneticTrait>();
    private Dictionary<TraitType, Button> traitButtons = new Dictionary<TraitType, Button>();
    private System.Action<List<GeneticTrait>> onConfirmCallback;
    private System.Action onCancelCallback;

    public void ShowTraitSelection(
        System.Action<List<GeneticTrait>> onConfirm, 
        System.Action onCancel)
    {
        this.onConfirmCallback = onConfirm;
        this.onCancelCallback = onCancel;
        selectedTraits.Clear();
        traitButtons.Clear();
        
        if (popupParent != null)
        {
            popupParent.gameObject.SetActive(true);
            popupParent.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
        
        CreateTraitButtons();
        
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancel);
        }

        if (titleText != null)
            titleText.text = "형질을 선택하세요";
    }

    private void CreateTraitButtons()
    {
        if (traitButtonParent == null || traitButtonPrefab == null) return;

        // 기존 버튼 제거
        foreach (Transform child in traitButtonParent)
            Destroy(child.gameObject);

        // 모든 형질 타입에 대한 버튼 생성 (None 제외)
        foreach (TraitType traitType in System.Enum.GetValues(typeof(TraitType)))
        {
            if (traitType == TraitType.None) continue;
            
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
                    isSelected = !isSelected;
                    ToggleTrait(currentTrait, isSelected);
                    UpdateButtonVisual(btn, isSelected);
                });
                
                traitButtons[currentTrait] = btn;
            }
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

    private void UpdateButtonVisual(Button btn, bool isSelected)
    {
        if (btn == null) return;
        
        var colors = btn.colors;
        colors.normalColor = isSelected ? Color.green : Color.white;
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

    private void OnConfirm()
    {
        if (popupParent != null)
        {
            popupParent.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                popupParent.gameObject.SetActive(false);
                onConfirmCallback?.Invoke(selectedTraits);
            });
        }
        else
        {
            onConfirmCallback?.Invoke(selectedTraits);
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

    public void Hide()
    {
        if (popupParent != null)
            popupParent.gameObject.SetActive(false);
    }
}
