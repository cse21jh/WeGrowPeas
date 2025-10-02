using TMPro;
using UnityEngine;

public class MessageBoxController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentMessageText;
    [SerializeField] private int boxIndex = 0;

    [SerializeField] private GameObject messageBoxPrefab;
    [SerializeField] private Transform messageBoxParent;


    public TMP_Text GetTextBox()
    {
        return currentMessageText;
    }

    public void SetText(string message)
    {
        currentMessageText = Instantiate(messageBoxPrefab, messageBoxParent).GetComponentInChildren<TextMeshProUGUI>();
        currentMessageText.text = message;
        boxIndex++;
    }

}
