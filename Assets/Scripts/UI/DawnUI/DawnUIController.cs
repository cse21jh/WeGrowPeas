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
    [SerializeField] private GameObject dawnPanel;
    [SerializeField] private Transform stageListContent;     // 단계 버튼 부모
    [SerializeField] private GameObject stageButtonPrefab;   // 단계 버튼 프리팹(Button + TMP 라벨)

    [SerializeField] private TextMeshProUGUI currentStageText; // 현재 선택 단계(화살표 선택기)
    [SerializeField] private TextMeshProUGUI constraintText;   // 누적 제약(병합, 변경분 색 강조)
    [SerializeField] private TextMeshProUGUI geneticsMultText; // 유전자 배율

    [SerializeField] private Button confirmButton;
    [SerializeField] private SaveSlotUI saveSlotUI;           // 게임 시작

    private int selectedStage = -1;

    public void OpenDawnPanel()
    {
        if (dawnPanel != null) dawnPanel.SetActive(true);
        BuildStageList();
        SelectStage(Mathf.Max(1, 1)); // 기본 1단계
    }

    public void CloseDawnPanel()
    {
        if (dawnPanel != null) dawnPanel.SetActive(false);
        selectedStage = -1;
    }

    private void BuildStageList()
    {
        if (stageListContent == null || stageButtonPrefab == null) return;
        foreach (Transform c in stageListContent) Destroy(c.gameObject);

        int maxUnlocked = DawnSystem.MaxUnlockedDawnStage;
        foreach (var data in DawnSystem.AllStages())
        {
            if (data == null) continue;
            GameObject go = Instantiate(stageButtonPrefab, stageListContent);
            go.SetActive(true);

            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = data.stage.ToString();

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = data.stage <= maxUnlocked; // 해금된 단계만 선택 가능
                int s = data.stage;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectStage(s));
            }
        }
    }

    // 좌우 화살표: 다음/이전 단계 (해금 범위 내에서만)
    public void NextStage()
    {
        if (DawnSystem.IsStageUnlocked(selectedStage + 1)) SelectStage(selectedStage + 1);
    }

    public void PrevStage()
    {
        if (selectedStage - 1 >= 1) SelectStage(selectedStage - 1);
    }

    public void SelectStage(int stage)
    {
        if (!DawnSystem.IsStageUnlocked(stage)) return;
        selectedStage = stage;

        if (currentStageText != null) currentStageText.text = $"{stage} 단계";
        if (constraintText != null)
            constraintText.text = DawnSystem.GetConstraintSummaryRich(stage); // 병합 + 변경분 색 강조
        if (geneticsMultText != null)
            geneticsMultText.text = $"유전자 배율 x{DawnSystem.GetGeneticsMultiplier(stage):0.##}";
        if (confirmButton != null) confirmButton.interactable = true;
    }

    // 확인 버튼 → 선택 저장 후 게임 시작
    public void ConfirmDawn()
    {
        if (selectedStage < 1) return;
        DawnSystem.SetSelectedStage(selectedStage);
        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");
        if (saveSlotUI != null) saveSlotUI.OnClickNewGame(); // 게임 시작
    }
}
