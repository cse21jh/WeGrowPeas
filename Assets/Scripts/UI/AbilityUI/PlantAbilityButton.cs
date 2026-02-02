using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantAbilityButton : MonoBehaviour
{
    private AbilityUIController abilityUIController;

    private PlantAbilityData abilityData;

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI name;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI level;

    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button levelDownButton;

    public void Init(PlantAbilityData ability, AbilityUIController controller)
    {
        abilityUIController = controller;
        abilityData = ability;
        icon.sprite = ability.icon;
        name.text = ability.abilityName;
        description.text = ability.description;
        level.text = "0";

        levelUpButton.onClick.AddListener(() =>
        {
            abilityUIController.LevelUpPlantAbility(ability);
            ChangeLevelUI(ability.level);
            SoundManager.Instance.PlayEffect("Button");
        });

        levelDownButton.onClick.AddListener(() =>
        {
            abilityUIController.LevelDownPlantAbility(ability);
            ChangeLevelUI(ability.level);
            SoundManager.Instance.PlayEffect("Button");
        });
    }

    public void ChangeLevelUI(int var)
    {        
        level.text = var.ToString();
    }
}
