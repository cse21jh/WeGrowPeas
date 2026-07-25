using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MsgBtnController : MonoBehaviour
{
    [SerializeField] private Image profileImage;
    [SerializeField] private TextMeshProUGUI senderName;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI unreadCount;

    [SerializeField] private Image unreadMarkImage;
    [SerializeField] private Sprite unreadMark;
    [SerializeField] private Sprite readMark;

    public void SetUp(Sprite profile, string name, string text, int cnt)
    {
        profileImage.sprite = profile;
        senderName.text = name;
        messageText.text = text;
        unreadCount.text = cnt.ToString();

        if (cnt > 0)
        {
            unreadMarkImage.sprite = unreadMark;
            messageText.gameObject.SetActive(true);
        }
        else
        {
            unreadMarkImage.sprite = readMark;
            messageText.gameObject.SetActive(false);
        }
    }

}
