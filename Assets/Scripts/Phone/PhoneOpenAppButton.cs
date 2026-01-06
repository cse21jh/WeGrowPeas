using UnityEngine;

public class PhoneOpenAppButton : MonoBehaviour
{
    [SerializeField] private AppKey key;

    public void Open()
    {
        if (PhoneManager.Instance != null)
            PhoneManager.Instance.OpenApp(key);
    }
}