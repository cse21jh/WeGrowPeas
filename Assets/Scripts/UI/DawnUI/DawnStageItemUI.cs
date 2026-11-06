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
                    ? Loc.T("game.dawn.no_constraint")
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
            SoundManager.Instance?.PlayEffect(SoundIds.Button);
        }
        else
        {
            onClickLocked?.Invoke();
            SoundManager.Instance?.PlayEffect(SoundIds.Button);
        }
    }
}
