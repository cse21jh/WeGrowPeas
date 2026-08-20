using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 밭 정보 팝업 오른쪽의 효과 한 줄(GridDetailPrefab).
/// 어떤 오브젝트를 쓸지는 인스펙터에서 지정한다(이름으로 찾지 않는다).
/// </summary>
public class GridDetailRow : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    public void Setup(Sprite sprite, string text, Color color)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.color = color;
            icon.enabled = sprite != null;
        }

        if (label != null) label.text = text;
    }
}
