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
    [SerializeField] private GameObject selectedIcon;

    /// <summary>이름을 지금 언어로 다시 넣는다.</summary>
    public void RefreshTexts() => name.text = abilityData.LocalizedName;

    public void Init(GeneralAbilityData ability, bool isUnlocked, AbilityUIController controller)
    {
        abilityUIController = controller;
        abilityData = ability;
        icon.sprite = ability.icon;
        RefreshTexts();
        price = ability.price;

        if(isUnlocked)
        {
            this.GetComponent<Button>().onClick.AddListener(OnClick);
            lockIcon.SetActive(false);
            selectedIcon.SetActive(false);
        }
        else
        {
            //해금 관련 이미지 + 버튼 눌렀을 때 해금창 뜨도록
            selectedIcon.SetActive(false);
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
            if(abilityUIController.SelectGeneralAbility(abilityData))
            {
                isSelected = true;
                selectedIcon.SetActive(true);
            }
            SoundManager.Instance.PlayEffect(SoundIds.Button);            
            // 선택된 표시 해줘야함
        }
        else
        {
            abilityUIController.CancelGeneralAbility(abilityData);
            SoundManager.Instance.PlayEffect(SoundIds.Button);
            isSelected = false;
            selectedIcon.SetActive(false);
            //선택된 표시 풀어줘야 함
        }
    }
}
