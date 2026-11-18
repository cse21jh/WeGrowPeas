using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private GameObject SettingPanel;
    [SerializeField] private Toggle showBreedPopupToggle;
    [SerializeField] private Toggle playAlarmForSeenMessagesToggle;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider effectVolumeSlider;

    void Start()
    {
        if (showBreedPopupToggle != null)
        {
            showBreedPopupToggle.onValueChanged.AddListener((isOn) =>
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetBreedPopupSetting(isOn);
                }
            });
        }

        if (playAlarmForSeenMessagesToggle != null)
        {
            playAlarmForSeenMessagesToggle.onValueChanged.AddListener((isOn) =>
            {
                MessengerSaveSystem.PlayAlarmForSeenMessages = isOn;
            });
        }

        if (SettingPanel != null)
        {
            LanguageSelector.AddTo(SettingPanel.transform, (RectTransform)bgmVolumeSlider.transform);
            KeyBindingPanel.AddTo(SettingPanel.transform, (RectTransform)bgmVolumeSlider.transform);
            // 단축키 버튼까지 만든 뒤에 넣어야 버튼들이 함께 한 줄 내려간다.
            // 튜토리얼 설정창에는 토글이 없으므로 있는 행 중 맨 아래 행 밑에 붙인다.
            Component lastRow = playAlarmForSeenMessagesToggle != null ? playAlarmForSeenMessagesToggle
                : showBreedPopupToggle != null ? showBreedPopupToggle
                : effectVolumeSlider;
            RectTransform graphicsRow = lastRow != null
                ? GraphicsQualitySelector.AddTo(SettingPanel.transform,
                    (RectTransform)lastRow.transform,
                    (RectTransform)bgmVolumeSlider.transform)
                : null;
            if (graphicsRow != null)
                SettingStepperRow.AddTo(SettingPanel.transform, graphicsRow,
                    (RectTransform)bgmVolumeSlider.transform, "EndlessModeRow",
                    () => Loc.T("settings.endless"),
                    () => Loc.T(EndlessMode.Enabled ? "settings.endless.on" : "settings.endless.off"),
                    direction => EndlessMode.Enabled = direction > 0);
            SettingPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 단축키 화면이 ESC를 처리했으면 설정창은 그대로 둔다.
            if (KeyBindingPanel.BlocksEscape) return;

            // 도감이 설정창 위에 떠 있으면 ESC는 도감이 처리한다.
            // 도감을 닫으면 설정창이 그대로 남아 있어야 하므로 여기선 아무것도 하지 않는다.
            var codex = CodexUIController.Instance;
            if (codex != null && (codex.IsOpen || codex.ConsumedEscapeThisFrame)) return;

            // 프로필 선택 창도 ESC를 먼저 처리한다(시작 화면).
            if (ProfileSelectUI.BlocksEscape) return;

            // 설정창이 열려 있으면 닫기 (기존 동작)
            if (SettingPanel != null && SettingPanel.activeSelf)
            {
                HideSettingPanel();
            }
            // 폰이 열려 있으면 설정창 대신 폰을 닫기
            else if (PhoneManager.Instance != null && PhoneManager.Instance.IsOpen)
            {
                PhoneManager.Instance.SetOpen(false);
            }
            // 아무것도 안 열려 있으면 설정창 열기 (기존 동작)
            else
            {
                ShowSettingPanel();
            }
        }
    }

    public void ShowSettingPanel()
    {
        if (SettingPanel == null)
            return;

        SettingPanel.SetActive(true);

        if (showBreedPopupToggle != null && UIManager.Instance != null)
        {
            showBreedPopupToggle.isOn = UIManager.Instance.ShowBreedPopupSetting;
        }

        if (playAlarmForSeenMessagesToggle != null)
        {
            playAlarmForSeenMessagesToggle.isOn = MessengerSaveSystem.PlayAlarmForSeenMessages;
        }

        SoundManager.Instance.ConnectSlider(bgmVolumeSlider, effectVolumeSlider);
        Time.timeScale = 0;
        ClickRouter.Instance.IsBlockedByUI = true;
        return;
    }

    public void HideSettingPanel()
    {
        if (SettingPanel == null)
            return;
        SettingPanel.SetActive(false);
        Time.timeScale = 1;
        ClickRouter.Instance.IsBlockedByUI = false;

        // 볼륨·토글 변경을 바로 기록한다(종료 시에만 저장하면 크래시 때 날아간다).
        DeviceSettings.Save();
        return;
    }

    public void ToggleSettingPanel()
    {
        if (SettingPanel == null)
            return;
        if (SettingPanel.activeSelf)
            HideSettingPanel();
        else
            ShowSettingPanel();
        return;
    }

    /// <summary>
    /// 설정창의 도감 버튼. 도감은 DontDestroyCanvas에 있어 씬을 넘어 유지되므로
    /// 게임 씬에서도 같은 인스턴스를 연다.
    ///
    /// 설정창은 닫지 않고 그 위에 띄운다. 설정창이 열려 있는 동안은 timeScale이 0이라
    /// 도감을 보는 동안 게임이 진행되지 않는다.
    /// </summary>
    public void OpenCodex()
    {
        var codex = CodexUIController.Instance;
        if (codex == null)
        {
            // StartScene을 거치지 않고 게임 씬에서 바로 플레이하면 도감이 없다.
            Debug.LogWarning("[SettingUI] 도감(CodexUIController)을 찾지 못했습니다. " +
                             "StartScene에서 시작했는지 확인하세요.");
            return;
        }

        codex.OpenCodex();
    }

    public void PlayButtonClickSound()
    {
        SoundManager.Instance.PlayEffect(SoundIds.Button);
    }
}
