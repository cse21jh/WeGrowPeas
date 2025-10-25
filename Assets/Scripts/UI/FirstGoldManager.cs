using UnityEngine;

public class FirstGoldManager : MonoBehaviour
{
    [SerializeField] private bool isFirstGold = false;
    [SerializeField] private GameObject popup;
    [SerializeField] private PopupHideController popupHideController;

    public void SetFirstGold()
    {
        if (isFirstGold)
            return;

        isFirstGold = true;
        popup.SetActive(true);
        popupHideController.MaximizePanel();
    }


}
