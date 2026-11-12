using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using System;
using JetBrains.Annotations;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    private string currentPlant = PlantIds.Pea;

    [SerializeField] private List<PlantAbilityData> allPlantAbilities;
    [SerializeField] private List<GeneralAbilityData> allGeneralAbilities;


    //인게임에 따로 적용은 되지만 세이브 데이터별 저장은 해야하는 값들. UI에 보여줘야 함
    private List<PlantAbilityData> currentPlantAbility = new();

    private List<GeneralAbilityData> currentGeneralAbility = new();

    public List<PlantAbilityData> CurrentPlantAbility => currentPlantAbility;
    public List<GeneralAbilityData> CurrentGeneralAbility => currentGeneralAbility;
    public string CurrentPlantName => currentPlant;



    // 프로필 데이터로 저장 필요한 값들. 추후 불러오기 시 Initialize다음에 불러와야 함
    private Dictionary<PlayablePlantType, bool> isPlantUnlocked = new();

    /// <summary>식물 특성 1개의 최대 레벨.</summary>
    public const int MaxPlantAbilityLevel = 5;

    /// <summary>일반 특성을 끼울 수 있는 최대 칸 수.</summary>
    public const int MaxGeneralAbilitySlots = 3;

    private Dictionary<PlayablePlantType, int> plantAbilityPoint = new();

    // 처음부터 해금된 일반 특성의 에셋 이름.
    public const string DefaultUnlockedGeneralAbility = "AddGoldAbility";

    private Dictionary<string, bool> isGeneralAbilityDataUnlocked = new(); // 일반 특성 에셋 이름, 해당 특성의 해금 여부

    private int generalAbilityPoint = 0;

    private const int DefaultGenetics = 10000;
    private int genetics = DefaultGenetics;

    private int storage = 0; //퀘스트, 등 게임 중간에 저장해 뒀다가 일정 기간을 두고 추가
    public int Storage => storage;

    public Dictionary<PlayablePlantType, bool> IsPlantUnlocked => isPlantUnlocked;
    public Dictionary<PlayablePlantType, int> PlantAbilityPoint => plantAbilityPoint;
    public Dictionary<string, bool> IsGeneralAbilityDataUnlocked => isGeneralAbilityDataUnlocked; // 일반 특성 에셋 이름, 해당 특성의 해금 여부
    public int GeneralAbilityPoint => generalAbilityPoint;
    public int Genetics => genetics;


    [SerializeField] private List<PlantInfoData> plantInfos;

    public PlantInfoData GetPlantInfo(PlayablePlantType type)
    {
        return plantInfos.Find(x => x.type == type);
    }


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

        ResetProfileToDefaults();
    }

    /// <summary>프로필 값(유전자·해금·포인트)을 새 프로필 상태로 되돌린다.</summary>
    public void ResetProfileToDefaults()
    {
        genetics = DefaultGenetics;
        isPlantUnlocked = new Dictionary<PlayablePlantType, bool>();
        plantAbilityPoint = new Dictionary<PlayablePlantType, int>();
        isGeneralAbilityDataUnlocked = new Dictionary<string, bool>();
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
            isGeneralAbilityDataUnlocked.Add(ability.UnlockKey, ability.UnlockKey == DefaultUnlockedGeneralAbility);
        }
    }

    public void SetPlant(string plantName)
    {
        currentPlant = plantName;
    }

    public void SetPlantByEnum(PlayablePlantType plant)
    {
        SetPlant(GetPlantInfo(plant).plantName);
    }

    public PlayablePlantType GetCurrentPlantType()
    {
        var t = plantInfos.Find(x => x.plantName == currentPlant);

        if (t == null)
            return PlayablePlantType.Pea;
        return t.type;
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
        if (!isPlantUnlocked.ContainsKey(plant) || isPlantUnlocked[plant]) // 키가 없거나 이미 해금 되어있던 경우
            return false;

        if (UseGenetics(GetPlantInfo(plant).price))
            return isPlantUnlocked[plant] = true;

        return false; // 돈 없어서 실패
    }

    public bool UnlockGeneralAbility(GeneralAbilityData ability)
    {
        if (!isGeneralAbilityDataUnlocked.ContainsKey(ability.UnlockKey) || isGeneralAbilityDataUnlocked[ability.UnlockKey]) // 키가 없거나 이미 해금 되어있던 경우
            return false;

        if(UseGenetics(ability.price))
        {
            isGeneralAbilityDataUnlocked[ability.UnlockKey] = true;
            return true;
        }

        return false; // 돈 없어서 실패
    }

    public bool AddPlantAbilityPoint(PlayablePlantType plant)
    {
        if (!plantAbilityPoint.ContainsKey(plant) || plantAbilityPoint[plant] >= 10)
            return false;


        if (UseGenetics(plantAbilityPoint[plant] * 300))
        {
            plantAbilityPoint[plant]++;
            return true;
        }

        return false;
    }

    public bool AddGeneralAbilityPoint()
    {
        if(generalAbilityPoint >= MaxGeneralAbilitySlots)
            return false;

        if (UseGenetics(generalAbilityPoint * 500))
        {
            generalAbilityPoint++;
            return true;
        }

        return false;
    }

    /// <summary>보유 능력과 유전자 저장고를 저장 데이터에 담는다. <see cref="LoadCurrentAbilityManager"/>와 짝.</summary>
    public void SaveCurrentAbilityManager(AbilitySave save)
    {
        save.currentPlantAbility = CurrentPlantAbility;
        save.currentGeneralAbility = CurrentGeneralAbility;
        save.geneStorage = Storage;
    }

    // 각 세이브 파일별 저장이 필요한 AbilityManager를 Load
    /// <param name="currentPlant">선택한 작물 이름(ProgressSave.currentPlant).</param>
    public void LoadCurrentAbilityManager(AbilitySave saveData, string currentPlant)
    {
        SetPlant(currentPlant);
        SetPlantAbility(saveData.currentPlantAbility);
        SetGeneralAbility(saveData.currentGeneralAbility);
        storage = saveData.geneStorage;
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

    public bool UseGenetics(int val)
    {
        if(genetics >= val)
        {
            ChangeGenetics(genetics - val);
            return true;
        }
        return false;
    }
    public void ChangeGenetics(int val) => ChangeGenetics(val, requestSave: true);

    internal void ChangeGenetics(int val, bool requestSave)
    {
        genetics = val;
        if (requestSave) SaveManager.RequestProfileSave();
        var panel = FindAnyObjectByType<AbilityUIController>();
        if (panel != null) panel.UpdateGenetics();

        // 인게임 내에서 변동 등도 필요하면 수정 필요
    }

    public int GetGenetics()
    {
        return genetics;
    }

    /// <summary>프로필에 담는다. <see cref="LoadProfile"/>과 짝.</summary>
    public void SaveProfile(AbilityProfile profile)
    {
        profile.genetics = genetics;
        profile.generalAbilityPoint = generalAbilityPoint;

        profile.plants.Clear();
        foreach (var kv in isPlantUnlocked)
        {
            plantAbilityPoint.TryGetValue(kv.Key, out int point);
            profile.plants.Add(new PlantProgress { type = kv.Key, unlocked = kv.Value, abilityPoint = point });
        }

        profile.generalAbilities.Clear();
        foreach (var kv in isGeneralAbilityDataUnlocked)
            profile.generalAbilities.Add(new NamedFlag { name = kv.Key, value = kv.Value });
    }

    /// <summary>
    /// 기본값 위에 저장된 값을 덮어쓴다. 저장 이후 새로 추가된 식물·특성도 기본값으로 들어 있게 된다.
    /// </summary>
    public void LoadProfile(AbilityProfile profile)
    {
        ResetProfileToDefaults();
        if (profile == null) return;

        genetics = profile.genetics;
        generalAbilityPoint = profile.generalAbilityPoint;

        foreach (var p in profile.plants)
        {
            if (p == null) continue;
            isPlantUnlocked[p.type] = p.unlocked;
            plantAbilityPoint[p.type] = p.abilityPoint;
        }

        foreach (var a in profile.generalAbilities)
        {
            // 에셋에서 빠진 특성은 버린다(목록에 없는 키가 UI에 남지 않도록).
            if (a != null && isGeneralAbilityDataUnlocked.ContainsKey(a.name))
                isGeneralAbilityDataUnlocked[a.name] = a.value;
        }
    }

    public void AddGeneStorage(int num)
    {
        storage += num;
    }

    /// <summary>게임 종료 시 얻은 유전자 몇 개마다 치장 토큰 1개를 주는가.</summary>
    public const int GeneticsPerCustomizeToken = 100;

    /// <summary>
    /// 이번 판에 모은 유전자(storage)를 지급한다. 런이 끝날 때 한 번만 불린다(RunRecordFormatter.AwardGenetics).
    /// 같은 양을 기준으로 치장 토큰도 함께 지급한다(유전자 100개마다 1개, 나머지는 버림).
    /// </summary>
    public void AddGenetics()
    {
        int earned = storage;
        genetics += earned;
        GameRecordHolder.SaveGenetics(earned);
        storage = 0;

        AddCustomizeTokensForGenetics(earned);
        SaveManager.RequestProfileSave();
    }

    /// <summary>얻은 유전자에 맞춰 치장 토큰을 준다. 토큰 잔액은 CustomizeInventory가 프로필에 저장한다.</summary>
    private static void AddCustomizeTokensForGenetics(int earnedGenetics)
    {
        int tokens = earnedGenetics / GeneticsPerCustomizeToken;
        if (tokens <= 0) return;
        CustomizeInventory.AddTokens(tokens);
        Debug.Log($"[Customize] 유전자 {earnedGenetics}개 획득 → 치장 토큰 {tokens}개 지급 (잔액 {CustomizeInventory.Tokens})");
    }
}
