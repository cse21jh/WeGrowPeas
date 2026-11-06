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

        // 볼륨은 기기 설정에서 읽는다(프로필과 무관).
        BGMVolume = DeviceSettings.Data.bgmVolume;
        EffectVolume = DeviceSettings.Data.effectVolume;
        

        LoadClips("Audio/Effect/", SoundIds.SelectPlant, SoundIds.Breed, SoundIds.WrongSelect, SoundIds.Shovel, SoundIds.HitBug,
            SoundIds.Button, SoundIds.PhoneTouch, SoundIds.QuestSuccess, SoundIds.Alarm, SoundIds.Vibration);
        LoadClips("Audio/Wave/", SoundIds.Aging, SoundIds.Wind, SoundIds.Flood, SoundIds.Pest, SoundIds.Cold,
            SoundIds.HeavyRain, SoundIds.Heat, SoundIds.Drought);
        LoadClips("Audio/", SoundIds.Tractor);

        EffectSoundDictionary.Add(SoundIds.Farm, Resources.Load<AudioClip>("Audio/BGM/BGM"));
        EffectSoundDictionary.Add(SoundIds.StartScene, Resources.Load<AudioClip>("Audio/BGM/StartSceneBGM"));
        EffectSoundDictionary.Add(SoundIds.IntroScene, Resources.Load<AudioClip>("Audio/BGM/IntroSceneBGM"));


        if (SceneManager.GetActiveScene().name == SceneLoader.StartSceneName)
            PlayBgm(SoundIds.StartScene);
    }

    // 파일 이름이 곧 소리 이름인 클립을 읽는다.
    private void LoadClips(string folder, params string[] names)
    {
        foreach (string name in names)
            EffectSoundDictionary.Add(name, Resources.Load<AudioClip>(folder + name));
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
        DeviceSettings.Data.bgmVolume = val;
    }

    public void ChangeEffectVolume(float val)
    {
        EffectVolume = val;
        EffectPlayer.volume = val;
        FlexibleEffectPlayer.volume = val;
        DeviceSettings.Data.effectVolume = val;
    }
}
