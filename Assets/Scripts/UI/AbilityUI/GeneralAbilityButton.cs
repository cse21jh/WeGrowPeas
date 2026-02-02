using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GeneralAbilityButton : MonoBehaviour
{
    private AbilityUIController abilityUIController;

    private bool isSelected = false;
    private GeneralAbilityData abilityData;

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI name;

    public void Init(GeneralAbilityData ability, bool isUnlocked, AbilityUIController controller)
    {
        abilityUIController = controller;
        abilityData = ability;
        icon.sprite = ability.icon;
        name.text = ability.abilityName;

        if(isUnlocked)
        {
            this.GetComponent<Button>().onClick.AddListener(OnClick);
        }
        else
        {
            //해금 관련 이미지 + 버튼 눌렀을 때 해금창 뜨도록
            this.GetComponent<Button>().onClick.AddListener(() =>
            {
                SoundManager.Instance.PlayEffect("Button");
            });
        }
    }

    private void OnClick()
    {
        if (!isSelected)
        {
            abilityUIController.SelectGeneralAbility(abilityData);
            SoundManager.Instance.PlayEffect("Button");
            isSelected = true;
            // 선택된 표시 해줘야함
        }
        else
        {
            abilityUIController.CancelGeneralAbility(abilityData);
            SoundManager.Instance.PlayEffect("Button");
            isSelected = false;
            //선택된 표시 풀어줘야 함
        }
    }
}
