using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlantSelectionButton : MonoBehaviour
{
    private AbilityUIController abilityUIController;

    private PlayablePlantType plantType;

    private int price;    

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI name;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private GameObject lockIcon;

    

    //[SerializeField] List<Sprite> plantImage;
    //[SerializeField] List<string> plantName;
    //[SerializeField] List<string> plantDescription;    

    //언락 여부에 따라 활성화, 비활성화 시켜줘야 함
    public void Init(PlayablePlantType plant, bool isUnlocked, AbilityUIController controller) // 아직 해금 전인 경우엔 클릭 시 해금 안내 창 떠야함
    {
        abilityUIController = controller;

        plantType = plant;

        var info = AbilityManager.Instance.GetPlantInfo(plant);
        // info가 default값일 수 있으므로(리스트에 없을 때), 체크 필요할 수 있음
        
        if (icon != null) icon.sprite = info.icon;
        if (description != null) price = info.price;
        RefreshTexts();

        if (isUnlocked)
        {
            this.GetComponent<Button>().onClick.AddListener(OnClick);
            lockIcon.SetActive(false);
        }
        else
        {
            lockIcon.SetActive(true);
            //해금 관련 이미지 + 버튼 눌렀을 때 해금창 뜨도록
            this.GetComponent<Button>().onClick.AddListener(() =>
            {
                abilityUIController.OpenUnlockUI(name.text, price, () => abilityUIController.TryUnlockPlant(plant));
            });
        }
    }
    /// <summary>이름과 설명을 지금 언어로 다시 넣는다.</summary>
    public void RefreshTexts()
    {
        var info = AbilityManager.Instance.GetPlantInfo(plantType);
        if (name != null) name.text = info.LocalizedName;
        if (description != null) description.text = info.LocalizedDescription;
    }

    private void OnClick()
    {
        abilityUIController.SelectPlant(plantType);
        SoundManager.Instance.PlayEffect(SoundIds.Button);        
    }
}
