using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private GameObject SettingPanel;
    [SerializeField] private Toggle showBreedPopupToggle;
    [SerializeField] private Toggle playAlarmForSeenMessagesToggle;

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
            SettingPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
            Debug.Log("1234");
            showBreedPopupToggle.isOn = UIManager.Instance.ShowBreedPopupSetting;
        }

        if (playAlarmForSeenMessagesToggle != null)
        {
            Debug.Log("5678");
            playAlarmForSeenMessagesToggle.isOn = MessengerSaveSystem.PlayAlarmForSeenMessages;
        }

        SoundManager.Instance.ConnectSlider(SettingPanel.transform.Find("BGMVolumeBar").GetComponent<Slider>(), SettingPanel.transform.Find("EffectVolumeBar").GetComponent<Slider>());
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
    public void PlayButtonClickSound()
    {
        SoundManager.Instance.PlayEffect("Button");
    }
}
