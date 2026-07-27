using UnityEngine;
using System.IO;
using NUnit.Framework;
using System.Collections.Generic;

public class ProfileData
{
    //AbilityManager
    public int genetics;

    public List<PlayablePlantType> unlockPlantType = new();
    public List<bool> isPlantUnlocked = new();

    public List<PlayablePlantType> plantTypeOfAbilityPoint = new();
    public List<int> plantAbilityPoint = new();

    public List<string> generalAbilityDataName = new();
    public List<bool> isGeneralAbilityDataUnlocked = new();

    public int generalAbilityPoint;

    //SoundManager
    public float BGMVolume;
    public float EffectVolume;

    //TutorialManager
    public bool hasSeenTutorial;

    public bool showBreedPopupSetting = true;

    //PhoneManager & Messenger
    public List<string> readMessengerKeys = new List<string>();
    public List<string> unlockedItems = new List<string>();
    public bool playAlarmForSeenMessages = true;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProfileData();
    }
    void OnApplicationQuit()
    {
        SaveProfileData();
    }

    private void LoadProfileData()
    {
        string path = GetSavePath();
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        ProfileData profileData = JsonUtility.FromJson<ProfileData>(json);

        SoundManager.Instance.LoadSoundManager(profileData);
        AbilityManager.Instance.LoadAbilityManager(profileData);

        UIManager.Instance.LoadUIManager(profileData);

        // 로드하기
        MessengerSaveSystem.PlayAlarmForSeenMessages = profileData.playAlarmForSeenMessages;
        UnlockManager.SetUnlockedList(profileData.unlockedItems);
        MessengerSaveSystem.SetReadKeys(profileData.readMessengerKeys);
    }

    public void SaveProfileData()
    {
        var profileData = new ProfileData();

        //AbilityManager
        profileData.genetics = AbilityManager.Instance.Genetics;

        foreach (KeyValuePair<PlayablePlantType, bool> val in AbilityManager.Instance.IsPlantUnlocked)
        {
            profileData.unlockPlantType.Add(val.Key);
            profileData.isPlantUnlocked.Add(val.Value);
        }

        foreach (KeyValuePair<PlayablePlantType, int> val in AbilityManager.Instance.PlantAbilityPoint)
        {
            profileData.plantTypeOfAbilityPoint.Add(val.Key);
            profileData.plantAbilityPoint.Add(val.Value);
        }

        foreach (KeyValuePair<string, bool> val in AbilityManager.Instance.IsGeneralAbilityDataUnlocked)
        {
            profileData.generalAbilityDataName.Add(val.Key);
            profileData.isGeneralAbilityDataUnlocked.Add(val.Value);
        }

        profileData.generalAbilityPoint = AbilityManager.Instance.GeneralAbilityPoint;

        //SoundManager
        profileData.BGMVolume = SoundManager.Instance.BGMVolume;
        profileData.EffectVolume = SoundManager.Instance.EffectVolume;

        profileData.showBreedPopupSetting = UIManager.Instance.ShowBreedPopupSetting;

        //PhoneManager & Messenger
        profileData.playAlarmForSeenMessages = MessengerSaveSystem.PlayAlarmForSeenMessages;
        profileData.readMessengerKeys = MessengerSaveSystem.GetReadKeys();
        profileData.unlockedItems = UnlockManager.GetUnlockedList();

        //TutorialManager


        string json = JsonUtility.ToJson(profileData, true);
        File.WriteAllText(GetSavePath(), json);
    }
    private string GetSavePath()
    {
        return Application.dataPath + "/ProfileData.json";
    }

    [ContextMenu("Debug: Unlock All Elements")]
    public void DebugUnlockAllElements()
    {
        // 0. 모든 식물 타입 및 일반 특성 강제 해금 (AbilityManager)
        if (AbilityManager.Instance != null)
        {
            foreach (System.Enum type in System.Enum.GetValues(typeof(PlayablePlantType)))
            {
                var plantType = (PlayablePlantType)type;
                if (!AbilityManager.Instance.IsPlantUnlocked.ContainsKey(plantType))
                    AbilityManager.Instance.IsPlantUnlocked.Add(plantType, true);
                else
                    AbilityManager.Instance.IsPlantUnlocked[plantType] = true;
            }

            foreach (var ability in AbilityManager.Instance.GetAllGeneralAbility())
            {
                if (ability != null && !string.IsNullOrEmpty(ability.abilityName))
                {
                    if (!AbilityManager.Instance.IsGeneralAbilityDataUnlocked.ContainsKey(ability.abilityName))
                        AbilityManager.Instance.IsGeneralAbilityDataUnlocked.Add(ability.abilityName, true);
                    else
                        AbilityManager.Instance.IsGeneralAbilityDataUnlocked[ability.abilityName] = true;
                }
            }
        }

        // 1. 모든 새벽 단계 해금 (모든 식물)
        // 간혹 로드 타이밍 문제로 StageCount가 0이 되는 것을 방지하여 최소 20단계까지 강제 보장
        int maxStage = Mathf.Max(20, DawnSystem.StageCount);
        foreach (var plant in DawnSystem.Plants)
        {
            DawnSystem.SetMaxUnlockedStage(plant, maxStage);
        }

        // 2. 상점 아이템 모두 해금
        var allItems = Resources.LoadAll<ItemData>("");
        foreach (var item in allItems)
        {
            if (item.requiresUnlock && !string.IsNullOrEmpty(item.UnlockId))
            {
                UnlockManager.Unlock(item.UnlockId);
            }
        }

        // 3. 특수 아이템 모두 해금
        var allSpecialItems = Resources.LoadAll<SpecialItemData>("");
        foreach (var spec in allSpecialItems)
        {
            if (spec.plantSpecific && !string.IsNullOrEmpty(spec.UnlockId))
            {
                UnlockManager.Unlock(spec.UnlockId);
            }
        }

        // 4. 인게임 사건 해금
        UnlockManager.Unlock(UnlockManager.Ids.GoldenPlantCreated);
        UnlockManager.Unlock(UnlockManager.Ids.WinterReached);
        UnlockManager.Unlock(UnlockManager.Ids.FertilizerFourColumns);

        // 변경 사항 저장
        SaveProfileData();

        Debug.Log("[SaveManager] 디버그: 모든 새벽 단계 및 아이템을 성공적으로 해금했습니다.");
    }

    [ContextMenu("Debug: Reset All Data")]
    public void DebugResetAllData()
    {
        // 1. ProfileData.json (아이템 해금, 특성, 재화 등) 파일 삭제
        string path = GetSavePath();
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log($"[SaveManager] 프로필 데이터를 삭제했습니다: {path}");
        }

        // 2. 메모리 상의 아이템 해금 목록 초기화
        UnlockManager.ResetAll();

        // 3. PlayerPrefs에 저장된 새벽 단계 진행도 및 기타 세이브 초기화
        DawnSystem.ResetAllPlantProgress();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[SaveManager] 디버그: 모든 게임 진행 데이터가 초기화되었습니다. 초기 화면으로 돌아갑니다.");

        // 4. 싱글톤 매니저들을 파괴하고 첫 씬(타이틀)으로 리로드하여 완벽하게 초기 상태로 재구축
        if (AbilityManager.Instance != null) Destroy(AbilityManager.Instance.gameObject);
        if (SoundManager.Instance != null) Destroy(SoundManager.Instance.gameObject);
        if (UIManager.Instance != null) Destroy(UIManager.Instance.gameObject);
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);

        // 첫 번째 씬(타이틀 화면)으로 리로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);

        // SaveManager 자신도 파괴 (씬 리로드 시 새로 생성되도록)
        Destroy(gameObject);
    }
}
