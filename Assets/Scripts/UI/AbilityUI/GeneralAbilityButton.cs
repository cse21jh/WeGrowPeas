using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GeneralAbilityButton : MonoBehaviour
{
    private AbilityUIController abilityUIController;

    private bool isSelected = false;
    private GeneralAbilityData abilityData;

    private int price;


    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI name;
    [SerializeField] private GameObject lockIcon;

    public void Init(GeneralAbilityData ability, bool isUnlocked, AbilityUIController controller)
    {
        abilityUIController = controller;
        abilityData = ability;
        icon.sprite = ability.icon;
        name.text = ability.abilityName;
        price = ability.price;

        if(isUnlocked)
        {
            this.GetComponent<Button>().onClick.AddListener(OnClick);
            lockIcon.SetActive(false);
        }
        else
        {
            //해금 관련 이미지 + 버튼 눌렀을 때 해금창 뜨도록
            this.GetComponent<Button>().onClick.AddListener(() =>
            {
                abilityUIController.OpenUnlockUI(name.text, price, () => abilityUIController.TryUnlockGeneralAbility(abilityData));                
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
