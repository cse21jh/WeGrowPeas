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

    public void ShowSettingPanel()
    {
        if (SettingPanel == null)
            return;
        SettingPanel.SetActive(true);
        Time.timeScale = 0;
        return;
    }

    public void HideSettingPanel()
    {
        if (SettingPanel == null)
            return;
        SettingPanel.SetActive(false);
        Time.timeScale = 1;
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

}
