using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정보 팝업의 일반 특성 한 칸(CommonTraitPrefab).
/// 어떤 오브젝트를 쓸지는 인스펙터에서 지정한다(이름으로 찾지 않는다).
/// </summary>
public class CommonTraitRow : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    [Tooltip("수량 표시. 일반 특성에는 수량이 없어 보통 비워 둔다.")]
    [SerializeField] private TMP_Text amountText;

    public void Setup(Sprite sprite, string traitName, string amount = "")
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (nameText != null) nameText.text = traitName;

        if (amountText != null)
        {
            amountText.text = amount;
            amountText.gameObject.SetActive(!string.IsNullOrEmpty(amount));
        }
    }
}
