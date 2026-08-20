using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 날씨 팝업의 지난 웨이브 한 줄(WaveBanner).
/// 어떤 오브젝트를 쓸지는 인스펙터에서 지정한다(이름으로 찾지 않는다).
/// </summary>
public class WaveBannerRow : MonoBehaviour
{
    [Tooltip("\"오늘\" / \"3일 전\"")]
    [SerializeField] private TMP_Text dayText;

    [SerializeField] private Image waveImage;

    [Tooltip("\"완두콩 5개 죽음\" / \"피해 없음\"")]
    [SerializeField] private TMP_Text dieText;

    public void Setup(string day, Sprite waveIcon, string die)
    {
        if (dayText != null) dayText.text = day;

        if (waveImage != null)
        {
            waveImage.sprite = waveIcon;
            waveImage.enabled = waveIcon != null;
        }

        if (dieText != null) dieText.text = die;
    }
}
