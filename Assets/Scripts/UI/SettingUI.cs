using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private GameObject SettingPanel;

    void Start()
    {
        if (SettingPanel != null) 
            SettingPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingPanel();
        }
    }

    public void ShowSettingPanel()
    {
        if (SettingPanel == null)
            return;
        SettingPanel.SetActive(true);
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
