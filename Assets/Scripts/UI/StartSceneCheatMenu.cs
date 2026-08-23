using UnityEngine;


public sealed class StartSceneCheatMenu : MonoBehaviour
{
    public void DebugUnlockAllElements()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[StartSceneCheatMenu] SaveManager를 찾을 수 없습니다.");
            return;
        }

        SaveManager.Instance.DebugUnlockAllElements();
    }

    public void DebugResetAllData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[StartSceneCheatMenu] SaveManager를 찾을 수 없습니다.");
            return;
        }

        SaveManager.Instance.DebugResetAllData();
    }
}
