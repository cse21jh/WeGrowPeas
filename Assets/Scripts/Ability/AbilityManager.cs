using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using System;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    private string currentPlant = "완두콩";

    [SerializeField] private List<PlantAbilityData> allPlantAbilities;
    [SerializeField] private List<GeneralAbilityData> allGeneralAbilities;


    //인게임에 따로 적용은 되지만 세이브 데이터별 저장은 해야하는 값들. UI에 보여줘야 함
    private List<PlantAbilityData> currentPlantAbility = new();

    private List<GeneralAbilityData> currentGeneralAbility = new();

    public List<PlantAbilityData> CurrentPlantAbility => currentPlantAbility;
    public List<GeneralAbilityData> CurrentGeneralAbility => currentGeneralAbility;



    // 프로필 데이터로 저장 필요한 값들. 추후 불러오기 시 Initialize다음에 불러와야 함
    private Dictionary<PlayablePlantType, bool> isPlantUnlocked = new();

    private Dictionary<PlayablePlantType, int> plantAbilityPoint = new();

    private Dictionary<string, bool> isGeneralAbilityDataUnlocked = new(); // 일반 특성 이름, 해당 특성의 해금 여부

    private int generalAbilityPoint = 0;

    public Dictionary<PlayablePlantType, bool> IsPlantUnlocked => isPlantUnlocked;

    public Dictionary<PlayablePlantType, int> PlantAbilityPoint => plantAbilityPoint;
    public Dictionary<string, bool> IsGeneralAbilityDataUnlocked => isGeneralAbilityDataUnlocked; // 일반 특성 이름, 해당 특성의 해금 여부

    public int GeneralAbilityPoint => generalAbilityPoint;


    void Awake()
    {
        // Singleton 패턴: 이미 인스턴스가 있으면 자신을 파괴, 없으면 자신을 인스턴스로 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeData();
    }

    private void InitializeData()
    {
        if (generalAbilityPoint != 0) // Load가 먼저 된 경우
            return;

        // 기본값으로 초기화
        isPlantUnlocked = new Dictionary<PlayablePlantType, bool>();
        plantAbilityPoint = new Dictionary<PlayablePlantType, int>();
        generalAbilityPoint = 1;

        for (int i = 0; i < Enum.GetValues(typeof(PlayablePlantType)).Length; i++)
        {
            if(i == 0)
            {
                isPlantUnlocked.Add((PlayablePlantType)i, true);
                plantAbilityPoint.Add((PlayablePlantType)i, 3);
            }
            else
            {
                isPlantUnlocked.Add((PlayablePlantType)i, false);
                plantAbilityPoint.Add((PlayablePlantType)i, 3);
            }
        }

        foreach(var ability in allGeneralAbilities)
        {
            isGeneralAbilityDataUnlocked.Add(ability.name, ability.isUnlocked);
        }
    }

    public void SetPlant(string plantName)
    {
        currentPlant = plantName;
    }

    public void SetPlantByEnum(PlayablePlantType plant)
    {
        switch(plant)
        {
            case PlayablePlantType.Pea:
                SetPlant("완두콩");
                return;
            case PlayablePlantType.Peanut:
                SetPlant("땅콩");
                return;
        }
    }

    public void SetPlantAbility(List<PlantAbilityData> plantAbility)
    {
        currentPlantAbility = plantAbility;
    }

    public void SetGeneralAbility(List<GeneralAbilityData> generalAbility)
    {
        currentGeneralAbility = generalAbility;
    }

    public void ResetCurrentAbility()
    {
        currentPlant = null;
        currentPlantAbility.Clear();
        currentGeneralAbility.Clear();
    }

    public List<PlantAbilityData> GetAllPlantAbility()
    {
        return allPlantAbilities;
    }

    public List<GeneralAbilityData> GetAllGeneralAbility()
    {
        return allGeneralAbilities;
    }

    // 현 식물 해금 여부, 식물 특성 포인트 받아오는 함수들
    public Dictionary<PlayablePlantType, bool> GetIsPlantUnlocked()
    {
        return isPlantUnlocked;
    }

    public Dictionary<PlayablePlantType, int> GetPlantAbilityPoint()
    {
        return plantAbilityPoint;
    }

    public int GetGeneralAbilityPoint()
    {
        return generalAbilityPoint;
    }

    //해금 관련 함수들. 돈 관련 처리도 여기서
    public bool UnlockPlant(PlayablePlantType plant)
    {
        if (!isPlantUnlocked.ContainsKey(plant) && !isPlantUnlocked[plant]) // 키가 없거나 이미 해금 되어있던 경우
            return false;

        //재화 관련 판단

        return isPlantUnlocked[plant] = true;        
    }

    public bool UnlockGeneralAbility(GeneralAbilityData ability)
    {
        if (!isGeneralAbilityDataUnlocked.ContainsKey(ability.name) && !isGeneralAbilityDataUnlocked[ability.name]) // 키가 없거나 이미 해금 되어있던 경우
            return false;

        //재화 관련 판단

        isGeneralAbilityDataUnlocked[ability.name] = true;
        allGeneralAbilities.Find(a => a == ability).isUnlocked = true;

        return true;
    }

    public bool AddPlantAbilityPoint(PlayablePlantType plant)
    {
        if (!plantAbilityPoint.ContainsKey(plant) && plantAbilityPoint[plant] >= 10)
            return false;

        //재화 관련 판단

        plantAbilityPoint[plant]++;
        return true;
    }

    public bool AddGeneralAbilityPoint()
    {
        if(generalAbilityPoint >= 3)
            return false;

        //재화 관련 판단

        generalAbilityPoint++;
        return true;
    }

    // 각 세이브 파일별 저장이 필요한 AbilityManager를 Load
    public void LoadCurrentAbilityManager(SaveData saveData) 
    {
        SetPlant(saveData.currentPlant);
        SetPlantAbility(saveData.currentPlantAbility);
        SetGeneralAbility(saveData.currentGeneralAbility);
    }


    //인게임 들어가 특성들 적용시키는 함수 
    public void ApplyAbilities(GameManager gameManager)
    {
        gameManager.currentPlant = currentPlant;

        foreach (var ability in currentPlantAbility)
        {
            if (ability != null)
            {
                // 각 AbilityData에 구현된 ApplyEffect를 호출
                ability.ApplyEffect(gameManager);
            }
        }

        foreach (var ability in currentGeneralAbility)
        {
            if (ability != null)
            {
                // 각 AbilityData에 구현된 ApplyEffect를 호출
                ability.ApplyEffect(gameManager);
            }
        }
    }
}
