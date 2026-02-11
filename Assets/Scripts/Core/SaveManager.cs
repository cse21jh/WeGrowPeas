using UnityEngine;

public class ProfileData
{
    //AbilityManager
    public int genetics;

    //SoundManager
    public float BGMVolume;
    public float EffectVolume;

    //TutorialManager
    public bool hasSeenTutorial;

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
    }
    void OnApplicationQuit()
    {       
        
    }

    private void LoadProfileData()
    {

    }

    private void SaveProfileData()
    {

    }    
}
