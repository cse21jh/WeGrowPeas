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

        //각 로드하기 
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

        //TutorialManager


        string json = JsonUtility.ToJson(profileData, true);
        File.WriteAllText(GetSavePath(), json);
    }
    private string GetSavePath()
    {
        return Application.dataPath + "/ProfileData.json";
    }
}
