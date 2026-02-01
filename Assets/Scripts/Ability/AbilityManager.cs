using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using System;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    private string currentPlant = "완두콩";

    //인게임에 따로 적용은 되지만 세이브 데이터별 저장은 해야하는 값들. UI에 보여줘야 함
    private List<PlantAbilityData> currentPlantAbility = new();

    private List<GeneralAbilityData> currentGeneralAbility = new();

    public List<PlantAbilityData> CurrentPlantAbility => currentPlantAbility;
    public List<GeneralAbilityData> CurrentGeneralAbility => currentGeneralAbility;



    // 프로필 데이터로 저장 필요한 값들
    private Dictionary<PlayablePlantType, bool> unlockedPlant = new();

    private Dictionary<PlayablePlantType, int> plantAbilityPoint = new();

    private int generalAbilityPoint = 0;

    public Dictionary<PlayablePlantType, bool> UnlockedPlant => unlockedPlant;

    public Dictionary<PlayablePlantType, int> PlantAbilityPoint => plantAbilityPoint;

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
        unlockedPlant = new Dictionary<PlayablePlantType, bool>();
        plantAbilityPoint = new Dictionary<PlayablePlantType, int>();
        generalAbilityPoint = 1;

        for (int i = 0; i < Enum.GetValues(typeof(PlayablePlantType)).Length; i++)
        {
            if(i == 0)
            {
                unlockedPlant.Add((PlayablePlantType)i, true);
                plantAbilityPoint.Add((PlayablePlantType)i, 3);
            }
            else
            {
                unlockedPlant.Add((PlayablePlantType)i, false);
                plantAbilityPoint.Add((PlayablePlantType)i, 3);
            }
        }
    }

    public void SetPlant(string plantName)
    {
        currentPlant = plantName;
    }

    public void SetPlantAbility(List<PlantAbilityData> plantAbility)
    {
        currentPlantAbility = plantAbility;
    }

    public void SetGeneralAbility(List<GeneralAbilityData> generalAbility)
    {
        currentGeneralAbility = generalAbility;
    }

    public void ResetAbilities()
    {
        currentPlant = null;
        currentPlantAbility.Clear();
        currentGeneralAbility.Clear();
    }

    // 현 식물 해금 여부, 식물 특성 포인트 받아오는 함수들
    public Dictionary<PlayablePlantType, bool> GetUnlockedPlant()
    {
        return UnlockedPlant;
    }

    public Dictionary<PlayablePlantType, int> GetPlantAbilityPoint()
    {
        return PlantAbilityPoint;
    }

    public int GetGeneralAbilityPoint()
    {
        return GeneralAbilityPoint;
    }

    //해금 관련 함수들
    public bool UnlockPlant(PlayablePlantType plant)
    {
        if (!unlockedPlant.ContainsKey(plant) && !unlockedPlant[plant])
            return false;

        return unlockedPlant[plant] = true;        
    }

    public bool AddPlantAbilityPoint(PlayablePlantType plant)
    {
        if (!plantAbilityPoint.ContainsKey(plant) && plantAbilityPoint[plant] >= 10)
            return false;

        plantAbilityPoint[plant]++;
        return true;
    }

    public bool AddGeneralAbilityPoint()
    {
        if(generalAbilityPoint >= 3)
            return false;

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
