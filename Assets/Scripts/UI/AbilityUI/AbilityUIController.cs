using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityUIController : MonoBehaviour
{
    private AbilityManager abilityManager;

    [SerializeField] private SaveSlotUI saveSlotUI;

    //각 특성 패널
    [SerializeField] private GameObject plantAbilityPanel;
    [SerializeField] private GameObject generalAbilityPanel;

    //식물 특성 선택
    [SerializeField] private Transform plantListContent;
    [SerializeField] private GameObject plantButtonPrefab;
    [SerializeField] private Transform plantAbilityListContent;
    [SerializeField] private GameObject plantAbilityPrefab;

    [SerializeField] private Button addPlantAbilityButton;
    [SerializeField] private TextMeshProUGUI plantAbilityPointText;

    //일반 특성 UI
    [SerializeField] private Transform generalAbilityListContent;
    [SerializeField] private GameObject generalAbilityPrefab;

    [SerializeField] private Image generalAbilityIcon;
    [SerializeField] private TextMeshProUGUI generalAbilityName;
    [SerializeField] private TextMeshProUGUI generalAbilityDescription;

    [SerializeField] private Button addGeneralAbilityButton;
    [SerializeField] private TextMeshProUGUI generalAbilityPointText;

    //해금 UI
    [SerializeField] private GameObject unlockUI;

    //유전자 개수 UI
    [SerializeField] private TextMeshProUGUI geneticsInPlantPanel;
    [SerializeField] private TextMeshProUGUI geneticsInGeneralPanel;

    private PlayablePlantType selectedPlant = PlayablePlantType.Pea;
    private List<PlantAbilityData> selectedPlantAbilities = new();
    private List<GeneralAbilityData> selectedGeneralAbilities = new();

    private int remainPlantAbilityPoint = -1;
    private int remainGeneralAbilityPoint = -1;

    private void Start()
    {
        abilityManager = AbilityManager.Instance;
    }

    public void CloseAbilityPanel()
    {
        plantAbilityPanel.SetActive(false);
        generalAbilityPanel.SetActive(false);

        selectedPlant = PlayablePlantType.Pea;
        selectedPlantAbilities.Clear();
        selectedGeneralAbilities.Clear();

        remainPlantAbilityPoint = -1;
        remainGeneralAbilityPoint = -1;
    }

    public void OpenPlantAbilityPanel() // 첫 시작
    {        
        plantAbilityPanel.SetActive(true);
        generalAbilityPanel.SetActive(false);
        UpdateGenetics();
        UpdatePlantList();        
    }

    private void UpdatePlantList()
    {
        foreach (Transform child in plantListContent) Destroy(child.gameObject);

        var unlockedPlant = abilityManager.GetIsPlantUnlocked();

        foreach(PlayablePlantType plantType in System.Enum.GetValues(typeof(PlayablePlantType)))
        {
            GameObject plantButton = Instantiate(plantButtonPrefab, plantListContent);

            plantButton.GetComponent<PlantSelectionButton>().Init(plantType, unlockedPlant[plantType], this);
        }
        ClearPlantAbilityList();
        SelectPlant(PlayablePlantType.Pea);
    }

    public void TryUnlockPlant(PlayablePlantType plant)
    {
        if (abilityManager.UnlockPlant(plant)) // 해금 성공
        {
            UpdatePlantList();
            CloseUnlockUI();
            return;
        }

        FailUnlcok();
        return;
    }

    public void SelectPlant(PlayablePlantType plant) // 식물을 선택
    {
        selectedPlant = plant;
        remainPlantAbilityPoint = abilityManager.GetPlantAbilityPoint()[plant];
        addPlantAbilityButton.GetComponent<Button>().onClick.RemoveAllListeners();
        addPlantAbilityButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            OpenUnlockUI(AbilityManager.Instance.GetPlantInfo(plant).plantName + "의 특성 포인트 1", AbilityManager.Instance.PlantAbilityPoint[plant] * 300, () => TryAddPlantAbilityPoint(plant));            
        });
        UpdatePlantAbilityList(plant);
        UpdateRemainPlantAbilityPoint(remainPlantAbilityPoint);
    }

    private void ClearPlantAbilityList()
    {
        foreach (Transform child in plantAbilityListContent) Destroy(child.gameObject);
    }

    private void UpdatePlantAbilityList(PlayablePlantType plant) // 식물을 고르면 해당 식물에 대한 특성들 띄워주는 함수
    {
        foreach (Transform child in plantAbilityListContent) Destroy(child.gameObject);
        selectedPlantAbilities.Clear();

        var abilities = abilityManager.GetAllPlantAbility().Where(ability => ability.type == plant);

        foreach(var a in abilities) 
        {
            GameObject ability = Instantiate(plantAbilityPrefab, plantAbilityListContent);
            a.level = 0;
            ability.GetComponent<PlantAbilityButton>().Init(a, this);
        }
    }

    private void UpdateRemainPlantAbilityPoint(int point) // 특성 포인트 사용, 반환, 최대량 증가 시 remain에 적용 및 UI에 적용
    {
        remainPlantAbilityPoint = point;
        plantAbilityPointText.text = "남은 포인트 : " + remainPlantAbilityPoint.ToString();
        //UI에 적용 필요
    }

    public bool LevelUpPlantAbility(PlantAbilityData ability) // 특정 특성 레벨 + 1
    {
        if (remainPlantAbilityPoint <= 0) return false; // 스킬 포인트 없

        int idx = selectedPlantAbilities.FindIndex(item => item == ability);

        if (idx == -1) // 아직 리스트에 삽입 안 된 경우
        {
            ability.level = 1;
            selectedPlantAbilities.Add(ability);
            UpdateRemainPlantAbilityPoint(remainPlantAbilityPoint - 1);
            return true;
        }
        // 리스트 안에 들어있는 경우

        if (selectedPlantAbilities[idx].level == 5) // 이미 최고랩
            return false;

        selectedPlantAbilities[idx].level++; // 들어 있고, 최고랩이 아닌 경우
        UpdateRemainPlantAbilityPoint(remainPlantAbilityPoint - 1);
        return true;
    }

    public bool LevelDownPlantAbility(PlantAbilityData ability)// 특정 특성 레벨 - 1
    {        
        int idx = selectedPlantAbilities.FindIndex(item => item == ability);

        if (idx == -1) // 아직 리스트에 삽입 안 된 경우
            return false;

        // 리스트 안에 들어있는 경우
        if (selectedPlantAbilities[idx].level == 1) // 풀에서 제거
        {
            selectedPlantAbilities[idx].level--;
            selectedPlantAbilities.Remove(ability);
            UpdateRemainPlantAbilityPoint(remainPlantAbilityPoint + 1);
            return true;
        }

        selectedPlantAbilities[idx].level--; // 들어 있고, 1렙이 아닌 경우
        UpdateRemainPlantAbilityPoint(remainPlantAbilityPoint + 1);
        return true;
    }

    public void TryAddPlantAbilityPoint(PlayablePlantType plant) // 해당 식물의 특성 포인트 증가
    {
        if (remainPlantAbilityPoint == -1) // 아직 식물 선택 전이라 포인트 증가 X. 안전장치
            return;

        if (AbilityManager.Instance.AddPlantAbilityPoint(selectedPlant))
        {
            remainPlantAbilityPoint++;
            UpdateRemainPlantAbilityPoint(remainPlantAbilityPoint);
            CloseUnlockUI();
            return;
        }

        FailUnlcok();
        return;
        // 포인트 증가 실패 (이미 포인트가 최대거나 재화 부족)
    }

    // 아래는 일반 특성 관련

    public void OpenGeneralAbilityPanel() // 식물 선택 끝, 일반 특성 선택 패널 띄우기. 확인 버튼 누를 때 호출
    {
        plantAbilityPanel.SetActive(false);
        generalAbilityPanel.SetActive(true);
        ClearGeneralAbilityDescription();
        UpdateGeneralAbilityList();
        UpdateGenetics();
    }

    private void UpdateGeneralAbilityList() // 
    {
        foreach (Transform child in generalAbilityListContent) Destroy(child.gameObject);
        selectedGeneralAbilities.Clear();
        
        foreach (var a in abilityManager.GetAllGeneralAbility())
        {
            GameObject ability = Instantiate(generalAbilityPrefab, generalAbilityListContent);

            ability.GetComponent<GeneralAbilityButton>().Init(a, abilityManager.IsGeneralAbilityDataUnlocked[a.abilityName] ,this);
        }

        addGeneralAbilityButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            OpenUnlockUI("일반 특성 포인트 1", AbilityManager.Instance.GeneralAbilityPoint * 500, () => TryAddGeneralAbilityPoint());            
        });

        remainGeneralAbilityPoint = abilityManager.GetGeneralAbilityPoint();
        UpdateRemainGeneralAbilityPoint(remainGeneralAbilityPoint);
    }

    public void UpdateRemainGeneralAbilityPoint(int point)
    {
        remainGeneralAbilityPoint = point;
        generalAbilityPointText.text = "남은 포인트 : " + remainGeneralAbilityPoint.ToString();
        //UI 업데이트 필요
    }
    public bool SelectGeneralAbility(GeneralAbilityData ability) // 특정 특성 레벨 + 1
    {
        if (remainGeneralAbilityPoint <= 0) return false; // 스킬 포인트 없

        if (selectedGeneralAbilities.Contains(ability)) // 이미 리스트에 들어있음
        {
            return false ;
        }
        else // 리스트에 없음
        {
            selectedGeneralAbilities.Add(ability);
            UpdateRemainGeneralAbilityPoint(remainGeneralAbilityPoint - 1);
            ShowGeneralAbilityDescription(ability);
            return true;
        }                
    }

    public bool CancelGeneralAbility(GeneralAbilityData ability)// 특정 특성 레벨 - 1
    {
        if (selectedGeneralAbilities.Contains(ability)) // 이미 리스트에 들어있음
        {
            selectedGeneralAbilities.Remove(ability);
            UpdateRemainGeneralAbilityPoint(remainGeneralAbilityPoint + 1);
            ClearGeneralAbilityDescription();
            return true;
        }
        else // 리스트에 없음
        {
            return false;
        }
    }

    public void TryUnlockGeneralAbility(GeneralAbilityData ability)
    {
        if (abilityManager.UnlockGeneralAbility(ability)) // 해금 성공
        {
            UpdateGeneralAbilityList();
            CloseUnlockUI();
            return;
        }
        FailUnlcok();
        return;

    }

    public void TryAddGeneralAbilityPoint() // 일반 특성 포인트 증가
    {
        if (remainGeneralAbilityPoint == -1) // 일반 특성 창 들어오지 않은 오류
            return;

        if (AbilityManager.Instance.AddGeneralAbilityPoint())
        {
            remainGeneralAbilityPoint++;
            UpdateRemainGeneralAbilityPoint(remainGeneralAbilityPoint);
            CloseUnlockUI();
            return;
        }

        FailUnlcok();
        return;
        // 포인트 증가 실패 (이미 포인트가 최대거나 재화 부족)
    }

    public void ShowGeneralAbilityDescription(GeneralAbilityData ability)
    {
        generalAbilityIcon.sprite = ability.icon;
        Color c = generalAbilityIcon.color;
        c.a = 1f;
        generalAbilityIcon.color = c;
        generalAbilityName.text = ability.abilityName;
        generalAbilityDescription.text = ability.description;
    }

    public void ClearGeneralAbilityDescription()
    {
        generalAbilityIcon.sprite = null ;
        Color c = generalAbilityIcon.color;
        c.a = 0f;
        generalAbilityIcon.color = c;
        generalAbilityName.text = "";
        generalAbilityDescription.text = "";
    }

    public void ConfirmAbility()
    {
        abilityManager.SetPlantByEnum(selectedPlant);
        abilityManager.SetPlantAbility(selectedPlantAbilities);
        abilityManager.SetGeneralAbility(selectedGeneralAbilities);

        saveSlotUI.OnClickNewGame();
        //게임 시작
    }


    public void OpenUnlockUI(string name, int price, Action unlockAction)
    {
        unlockUI.gameObject.SetActive(true);
        unlockUI.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = name + " 해금에\n" + price.ToString() + "개의 유전자가 필요합니다.\n해금하시겠습니까?";
        Button b = unlockUI.transform.Find("ConfirmButton").GetComponent<Button>();
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() =>
        {            
            unlockAction();
            SoundManager.Instance.PlayEffect("Button");
        });
    }

    public void FailUnlcok()
    {
        SoundManager.Instance.PlayEffect("WrongSelect");
        unlockUI.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "<color=#FF4F4F>유전자가 부족하거나 이미 최대치입니다</color>";
    }

    public void CloseUnlockUI()
    {
        unlockUI.gameObject.SetActive(false);

    }

    public void UpdateGenetics()
    {
        geneticsInGeneralPanel.text = abilityManager.GetGenetics().ToString();
        geneticsInPlantPanel.text = abilityManager.GetGenetics().ToString();
    }
}

