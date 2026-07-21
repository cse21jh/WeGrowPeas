using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새벽 모드 단계 선택 목록의 단일 항목 UI.
/// - Stage number text / Constraint description text / Lock icon / Selection highlight
/// </summary>
public class DawnStageItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private TextMeshProUGUI constraintDescriptionText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private Button button;

    private int stage;
    private bool isUnlocked;
    private Action<int> onClickUnlocked;
    private Action onClickLocked;

    private void Awake()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        if (stageNumberText == null)
        {
            var stageTF = transform.Find("StageText") ?? transform.Find("Text_Stage") ?? transform.Find("StageNumber");
            if (stageTF != null) stageNumberText = stageTF.GetComponent<TextMeshProUGUI>();
            if (stageNumberText == null) stageNumberText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (constraintDescriptionText == null)
        {
            var descTF = transform.Find("ConstraintText") ?? transform.Find("Text_Constraint") ?? transform.Find("Description");
            if (descTF != null) constraintDescriptionText = descTF.GetComponent<TextMeshProUGUI>();
        }

        if (lockIcon == null)
        {
            var lockTF = transform.Find("LockIcon") ?? transform.Find("Lock") ?? transform.Find("Icon_Lock") ?? transform.Find("LockOverlay");
            if (lockTF != null) lockIcon = lockTF.gameObject;
        }

        if (selectionHighlight == null)
        {
            var selTF = transform.Find("SelectionHighlight") ?? transform.Find("Highlight") ?? transform.Find("Selected") ?? transform.Find("Outline");
            if (selTF != null) selectionHighlight = selTF.gameObject;
        }
    }

    public void Setup(int stage, string constraintDescription, bool isUnlocked, bool isSelected, Action<int> onClickUnlocked, Action onClickLocked)
    {
        EnsureReferences();

        this.stage = stage;
        this.isUnlocked = isUnlocked;
        this.onClickUnlocked = onClickUnlocked;
        this.onClickLocked = onClickLocked;

        if (stageNumberText != null)
        {
            stageNumberText.text = stage.ToString();
        }

        if (lockIcon != null)
        {
            lockIcon.SetActive(!isUnlocked);
        }

        if (constraintDescriptionText != null)
        {
            if (isUnlocked)
            {
                constraintDescriptionText.text = string.IsNullOrWhiteSpace(constraintDescription)
                    ? "추가 제약 없음"
                    : constraintDescription;
            }
            else
            {
                constraintDescriptionText.text = "???";
            }
        }

        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }
    }

    private void OnClick()
    {
        if (isUnlocked)
        {
            onClickUnlocked?.Invoke(stage);
            SoundManager.Instance?.PlayEffect("Button");
        }
        else
        {
            onClickLocked?.Invoke();
            SoundManager.Instance?.PlayEffect("Button");
        }
    }
}
