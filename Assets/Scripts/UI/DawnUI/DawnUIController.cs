using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 전 새벽 단계(승천)를 고르는 UI. AbilityUIController와 유사한 역할.
/// 해금된 단계만 선택 가능하고, 선택 시 누적 제약 + 유전자 배율을 보여준다.
/// 확인 시 DawnSystem에 선택을 저장하고 새 게임을 시작한다.
/// </summary>
public class DawnUIController : MonoBehaviour
{
    public struct UnlockItemInfo
    {
        public string displayName;
        public string description;
        public Sprite icon;
        public bool isSpecial;
    }
    [SerializeField] private GameObject dawnPanel;
    [SerializeField] private Transform stageListContent;     // 단계 버튼 부모 (ScrollRect Content)
    [SerializeField] private GameObject stageButtonPrefab;   // 단계 버튼 프리팹(Button + TMP 라벨 + 자물쇠)

    [SerializeField] private TextMeshProUGUI currentStageText; // 현재 선택 단계(화살표 선택기 / 헤더)
    [SerializeField] private TextMeshProUGUI constraintText;   // 누적 제약(병합, 변경분 색 강조)
    [SerializeField] private TextMeshProUGUI geneticsMultText; // 유전자 배율

    [Header("Unlock Item Icon Settings")]
    [SerializeField] private Transform unlockItemContainer;   // 해금 아이템 아이콘들을 담을 컨테이너
    [SerializeField] private GameObject unlockItemPrefab;     // 해금 아이템 아이콘 프리팹 (DawnUnlockItemSlot 컴포넌트 포함)
    [SerializeField] private RectTransform tooltipPanel;      // 툴팁 패널
    [SerializeField] private TextMeshProUGUI tooltipText;     // 툴팁 텍스트
    [SerializeField] private GameObject unlockItemArea;       // 해금 아이템 전체 영역 부모 오브젝트 (텍스트 + 스크롤 뷰)

    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;              // 닫기 버튼
    [SerializeField] private Button prevButton;               // 이전 버튼 (일반 특성 패널로 돌아가기)
    [SerializeField] private AbilityUIController abilityUIController;
    [SerializeField] private SaveSlotUI saveSlotUI;           // 게임 시작

    private int selectedStage = -1;
    private readonly List<DawnStageItemUI> stageItems = new List<DawnStageItemUI>();

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnClickClose);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnClickPrev);
        }
    }

    public void OpenDawnPanel()
    {
        if (dawnPanel != null) dawnPanel.SetActive(true);
        BuildStageList();
        SelectStage(Mathf.Max(0, DawnSystem.MaxUnlockedDawnStage)); // 해금된 최대 스테이지로 UI 띄워줌
    }

    public void CloseDawnPanel()
    {
        if (dawnPanel != null) dawnPanel.SetActive(false);
        selectedStage = -1;
        HideTooltip();
    }

    public void OnClickClose()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");
        CloseDawnPanel();
    }

    public void OnClickPrev()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");
        CloseDawnPanel();

        if (abilityUIController == null)
        {
            abilityUIController = FindObjectOfType<AbilityUIController>();
        }

        if (abilityUIController != null)
        {
            abilityUIController.OpenGeneralAbilityPanel();
        }
    }

    private void BuildStageList()
    {
        if (stageListContent == null || stageButtonPrefab == null) return;

        foreach (Transform c in stageListContent) Destroy(c.gameObject);
        stageItems.Clear();

        var stages = DawnSystem.AllStages();
        foreach (var data in stages)
        {
            if (data == null) continue;
            GameObject go = Instantiate(stageButtonPrefab, stageListContent);
            go.SetActive(true);

            DawnStageItemUI item = go.GetComponent<DawnStageItemUI>();
            if (item == null)
            {
                item = go.AddComponent<DawnStageItemUI>();
            }

            bool isUnlocked = DawnSystem.IsStageUnlocked(data.stage);
            int stageNum = data.stage;
            string constraintDesc = data.constraintDescription;

            item.Setup(
                stage: stageNum,
                constraintDescription: constraintDesc,
                isUnlocked: isUnlocked,
                isSelected: false,
                onClickUnlocked: (s) => SelectStage(s),
                onClickLocked: () => OnLockedStageClicked()
            );

            stageItems.Add(item);
        }
    }

    private void OnLockedStageClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEffect("Button");
        }

        if (UIManager.Instance != null && UIManager.Instance.Popup != null)
        {
            UIManager.Instance.Popup.ShowFloatingPopup("아직 해금되지 않은 단계입니다.", 2.0f);
        }
    }

    // 좌우 화살표: 다음/이전 단계 (해금 범위 내에서만)
    public void NextStage()
    {
        if (DawnSystem.IsStageUnlocked(selectedStage + 1)) SelectStage(selectedStage + 1);
    }

    public void PrevStage()
    {
        if (selectedStage - 1 >= 0) SelectStage(selectedStage - 1);
    }

    public void SelectStage(int stage)
    {
        if (!DawnSystem.IsStageUnlocked(stage)) return;
        selectedStage = stage;
        HideTooltip();

        if (currentStageText != null) currentStageText.text = $"{stage} 단계";
        if (constraintText != null)
            constraintText.text = DawnSystem.GetConstraintSummaryRich(stage); // 병합 + 변경분 색 강조
        if (geneticsMultText != null)
            geneticsMultText.text = $"유전자 배율 x{DawnSystem.GetGeneticsMultiplier(stage):0.##}";

        var stageData = DawnSystem.GetStage(stage);

        // 기존 생성된 아이콘 제거
        if (unlockItemContainer != null)
        {
            foreach (Transform child in unlockItemContainer)
            {
                if (child != null) Destroy(child.gameObject);
            }
        }

        // 아이템 정보 모으기
        List<UnlockItemInfo> itemsToShow = new List<UnlockItemInfo>();
        string plant = DawnSystem.CurrentPlant;

        // 특수 아이템 먼저 추가 (특수 아이템이 앞으로 정렬된다).
        // 현재 식물로 이 단계를 클리어하면 해금되는 식물별 특수 아이템을 자동으로 모은다.
        foreach (var spec in SpecialItemSystem.GetItemsUnlockedAtStage(stage, plant))
        {
            if (spec == null) continue;
            itemsToShow.Add(new UnlockItemInfo
            {
                displayName = spec.displayName,
                description = spec.description,
                icon = spec.icon,
                isSpecial = true
            });
        }

        // 일반 아이템: 현재 식물로 이 단계를 클리어하면 해금되는 상점 아이템을 자동으로 모은다.
        // (에셋의 unlockItems 수동 목록 대신, 각 ItemData의 metaRequiredDawnStage/Plant를 역으로 조회)
        foreach (var norm in UnlockRunTracker.GetItemsUnlockedAtStage(stage, plant))
        {
            if (norm == null) continue;
            itemsToShow.Add(new UnlockItemInfo
            {
                displayName = norm.DisplayName,
                description = norm.Description,
                icon = norm.Icon,
                isSpecial = false
            });
        }

        // 해금 아이템 리스트가 존재할 경우 아이콘으로 노출
        if (itemsToShow.Count > 0)
        {
            if (unlockItemArea != null) unlockItemArea.SetActive(true);
            if (unlockItemContainer != null)
            {
                var scrollRect = unlockItemContainer.GetComponentInParent<ScrollRect>();
                GameObject toggleTarget = scrollRect != null ? scrollRect.gameObject : unlockItemContainer.gameObject;
                toggleTarget.SetActive(true);

                if (unlockItemPrefab != null)
                {
                    foreach (var info in itemsToShow)
                    {
                        GameObject go = Instantiate(unlockItemPrefab, unlockItemContainer);
                        go.SetActive(true);
                        var slotScript = go.GetComponent<DawnUnlockItemSlot>();
                        if (slotScript == null) slotScript = go.AddComponent<DawnUnlockItemSlot>();
                        slotScript.Setup(info, this);
                    }
                }
            }
        }
        else
        {
            if (unlockItemArea != null)
            {
                unlockItemArea.SetActive(false);
            }
            else
            {
                if (unlockItemContainer != null)
                {
                    var scrollRect = unlockItemContainer.GetComponentInParent<ScrollRect>();
                    GameObject toggleTarget = scrollRect != null ? scrollRect.gameObject : unlockItemContainer.gameObject;
                    toggleTarget.SetActive(false);
                }
            }
        }

        // 선택 하이라이트 동기화
        var stages = DawnSystem.AllStages();
        for (int i = 0; i < stageItems.Count && i < stages.Count; i++)
        {
            if (stageItems[i] != null && stages[i] != null)
            {
                stageItems[i].SetSelected(stages[i].stage == selectedStage);
            }
        }

        if (confirmButton != null) confirmButton.interactable = true;
    }

    public void ShowTooltip(string nameLine, string itemDesc, Vector3 slotWorldPosition)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = $"{nameLine}<size=50%>\n\n</size>{itemDesc}";
        tooltipPanel.gameObject.SetActive(true);

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        RectTransform parentRect = tooltipPanel.parent as RectTransform;
        if (parentRect != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPosition),
                uiCamera,
                out localPoint
            );
            tooltipPanel.anchoredPosition = localPoint + new Vector2(0f, 50f);
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }
    }

    // 확인 버튼 → 선택 저장 후 게임 시작
    public void ConfirmDawn()
    {
        if (selectedStage < 0) return;
        DawnSystem.SetSelectedStage(selectedStage);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");
        if (saveSlotUI != null) saveSlotUI.OnClickNewGame(); // 게임 시작
    }
}
