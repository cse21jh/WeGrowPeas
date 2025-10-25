using UnityEngine;

public class PopupController : MonoBehaviour
{





    public void ClosePopup()
    {
        SoundManager.Instance.PlayEffect("Button");
        gameObject.SetActive(false);
    }
}
