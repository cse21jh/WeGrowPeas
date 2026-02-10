using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlantSelectionButton : MonoBehaviour
{
    private AbilityUIController abilityUIController;

    private PlayablePlantType plantType;

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI name;
    [SerializeField] private TextMeshProUGUI description;

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
        if (name != null) name.text = info.plantName;
        if (description != null) description.text = info.description;

        if (isUnlocked)
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
        abilityUIController.SelectPlant(plantType);
        SoundManager.Instance.PlayEffect("Button");        
    }
}
