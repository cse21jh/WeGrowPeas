using TMPro;
using UnityEngine;

public class MessageBoxBtnController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentMessageText;

    public void OnClicked()
    {
        FindAnyObjectByType<TutorialManager>().OnMessageBoxClicked();
    }

    public void SetText(string message)
    {
        currentMessageText.text = message;
    }
}
