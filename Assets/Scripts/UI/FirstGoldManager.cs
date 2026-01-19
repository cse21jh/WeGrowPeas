using UnityEngine;

public class FirstGoldManager : MonoBehaviour
{
    [SerializeField] private GameObject popup;
    [SerializeField] private PopupHideController popupHideController;

    public void SetFirstGold()
    {
        if (GameManager.Instance.seenFirstGold)
            return;
        PhoneManager.Instance.messengerApp.ActivateTrigger("GoldPlant");
        GameManager.Instance.seenFirstGold = true;
        /*
        Time.timeScale = 0f;
        popup.SetActive(true);
        popupHideController.MaximizePanel();
        */
    }

    public void TimeContinues()
    {
        Time.timeScale = 1f;
    }

    public void TimeStops()
    {
        Time.timeScale = 0f;
    }
}
