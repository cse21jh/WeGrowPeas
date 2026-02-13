using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource BgmPlayer;
    public AudioSource EffectPlayer;

    public AudioSource FlexibleEffectPlayer;
    public float BGMVolume { get; set; }
    public float EffectVolume { get; set; }

    [SerializeField] private AudioClip[] EffectAudioClips;
    [SerializeField] private Slider BGMVolumeSlider;
    [SerializeField] private Slider EffectVolumeSlider;

    private Dictionary<string, AudioClip> EffectSoundDictionary = new Dictionary<string, AudioClip>();


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        
        GameObject EffectTempObject = new GameObject("Effect");
        EffectTempObject.transform.SetParent(gameObject.transform);
        EffectPlayer = EffectTempObject.AddComponent<AudioSource>();

        GameObject BgmTempObject = new GameObject("Bgm");
        BgmTempObject.transform.SetParent(gameObject.transform);
        BgmPlayer = BgmTempObject.AddComponent<AudioSource>();

        GameObject FexibleEffectTempObject = new GameObject("FlexibleEffect");
        FexibleEffectTempObject.transform.SetParent(gameObject.transform);
        FlexibleEffectPlayer = FexibleEffectTempObject.AddComponent<AudioSource>();

        foreach (AudioClip audioclip in EffectAudioClips)
        {
            EffectSoundDictionary.Add(audioclip.name, audioclip);
        }

        BGMVolume = 0.05f;
        EffectVolume = 0.3f;
        

        EffectSoundDictionary.Add("SelectPlant", Resources.Load<AudioClip>("Audio/Effect/SelectPlant"));
        EffectSoundDictionary.Add("Breed", Resources.Load<AudioClip>("Audio/Effect/Breed"));
        EffectSoundDictionary.Add("WrongSelect", Resources.Load<AudioClip>("Audio/Effect/WrongSelect"));
        EffectSoundDictionary.Add("Shovel", Resources.Load<AudioClip>("Audio/Effect/Shovel"));
        EffectSoundDictionary.Add("HitBug", Resources.Load<AudioClip>("Audio/Effect/HitBug"));

        EffectSoundDictionary.Add("Button", Resources.Load<AudioClip>("Audio/Effect/Button"));
        EffectSoundDictionary.Add("PhoneTouch", Resources.Load<AudioClip>("Audio/Effect/PhoneTouch"));
        EffectSoundDictionary.Add("QuestSuccess", Resources.Load<AudioClip>("Audio/Effect/QuestSuccess"));
        EffectSoundDictionary.Add("Alarm", Resources.Load<AudioClip>("Audio/Effect/Alarm"));
        EffectSoundDictionary.Add("Vibration", Resources.Load<AudioClip>("Audio/Effect/Vibration"));

        EffectSoundDictionary.Add("Aging", Resources.Load<AudioClip>("Audio/Wave/Aging"));
        EffectSoundDictionary.Add("Wind", Resources.Load<AudioClip>("Audio/Wave/Wind"));
        EffectSoundDictionary.Add("Flood", Resources.Load<AudioClip>("Audio/Wave/Flood"));
        EffectSoundDictionary.Add("Pest", Resources.Load<AudioClip>("Audio/Wave/Pest"));
        EffectSoundDictionary.Add("Cold", Resources.Load<AudioClip>("Audio/Wave/Cold"));
        EffectSoundDictionary.Add("HeavyRain", Resources.Load<AudioClip>("Audio/Wave/HeavyRain"));
        EffectSoundDictionary.Add("Heat", Resources.Load<AudioClip>("Audio/Wave/Heat")); 
        EffectSoundDictionary.Add("Drought", Resources.Load<AudioClip>("Audio/Wave/Drought")); // 폭우와 동일한 임시 사운드 삽입해둠

        EffectSoundDictionary.Add("Tractor", Resources.Load<AudioClip>("Audio/Tractor"));

        EffectSoundDictionary.Add("Farm", Resources.Load<AudioClip>("Audio/BGM/BGM"));
        EffectSoundDictionary.Add("StartScene", Resources.Load<AudioClip>("Audio/BGM/StartSceneBGM"));

        
        if(SceneManager.GetActiveScene().name == "StartScene")
            PlayBgm("StartScene");
    }

    public void ConnectSlider(Slider BGM, Slider Effect)
    {
        BGMVolumeSlider = BGM;
        EffectVolumeSlider = Effect;
        BGMVolumeSlider.value = BGMVolume;
        EffectVolumeSlider.value = EffectVolume;
        BGMVolumeSlider.onValueChanged.AddListener(ChangeBGMVolume);
        EffectVolumeSlider.onValueChanged.AddListener(ChangeEffectVolume);
    }

    public void PlayEffect(string name)
    {
        EffectPlayer.PlayOneShot(EffectSoundDictionary[name], EffectVolume);
    }

    public IEnumerator PlayEffectLouder(string name, float time)
    {
        FlexibleEffectPlayer.volume = 0f;
        FlexibleEffectPlayer.clip = EffectSoundDictionary[name];
        FlexibleEffectPlayer.Play();
        float t = 0f;
        while(t < time)
        {
            t += Time.deltaTime;
            FlexibleEffectPlayer.volume += (Time.deltaTime / time) * EffectVolume;
            yield return null;
        }
        FlexibleEffectPlayer.Stop();

    }

    public IEnumerator StopEffectSmaller(string name, float time)
    {
        FlexibleEffectPlayer.volume = EffectVolume;
        FlexibleEffectPlayer.clip = EffectSoundDictionary[name];
        FlexibleEffectPlayer.Play();
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            FlexibleEffectPlayer.volume -= (Time.deltaTime / time) * EffectVolume;
            yield return null;
        }
        FlexibleEffectPlayer.Stop();
    }

    public void PlayBgm(string name)
    {
        BgmPlayer.loop = true;
        BgmPlayer.volume = BGMVolume;

        BgmPlayer.clip = EffectSoundDictionary[name];
        BgmPlayer.Play();
    }

    public void StopBgm()
    {
        BgmPlayer.clip = null;
        BgmPlayer.Stop();
    }

    public void ChangeBGMVolume(float val)
    {
        BGMVolume = val;
        BgmPlayer.volume = val;
    }

    public void ChangeEffectVolume(float val)
    {
        EffectVolume = val;
        EffectPlayer.volume = val;
        FlexibleEffectPlayer.volume = val;
    }

    public void LoadSoundManager(ProfileData data)
    {
        ChangeBGMVolume(data.BGMVolume);
        ChangeEffectVolume(data.EffectVolume);        
    }
}
