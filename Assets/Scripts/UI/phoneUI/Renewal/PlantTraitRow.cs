using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정보 팝업의 식물 특성 한 줄(PlantTraitPrefab). 아이콘·이름과 레벨 바를 채운다.
/// 어떤 오브젝트를 쓸지는 인스펙터에서 지정한다(이름으로 찾지 않는다).
/// </summary>
public class PlantTraitRow : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    [Tooltip("배운 레벨을 채우는 바. 없으면 생략")]
    [SerializeField] private Slider levelSlider;

    public void Setup(Sprite sprite, string traitName, int level, int maxLevel)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (nameText != null) nameText.text = traitName;

        if (levelSlider != null && maxLevel > 0)
        {
            float ratio = Mathf.Clamp01((float)level / maxLevel);
            levelSlider.value = Mathf.Lerp(levelSlider.minValue, levelSlider.maxValue, ratio);
        }
    }
}
