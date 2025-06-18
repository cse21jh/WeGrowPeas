using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : Singleton<SoundManager>
{
    public AudioSource BgmPlayer;
    public AudioSource EffectPlayer;

    public float BGMVolume { get; set; }
    public float EffectVolume { get; set; }

    [SerializeField] private AudioClip[] EffectAudioClips;

    private Dictionary<string, AudioClip> EffectSoundDictionary = new Dictionary<string, AudioClip>();

    void Awake()
    {

        GameObject EffectTempObject = new GameObject("Effect");
        EffectTempObject.transform.SetParent(gameObject.transform);
        EffectPlayer = EffectTempObject.AddComponent<AudioSource>();

        GameObject BgmTempObject = new GameObject("Bgm");
        BgmTempObject.transform.SetParent(gameObject.transform);
        BgmPlayer = BgmTempObject.AddComponent<AudioSource>();

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
        EffectSoundDictionary.Add("KillBug", Resources.Load<AudioClip>("Audio/Effect/KillBug"));

        EffectSoundDictionary.Add("Aging", Resources.Load<AudioClip>("Audio/Wave/Aging"));
        EffectSoundDictionary.Add("Wind", Resources.Load<AudioClip>("Audio/Wave/Wind"));
        EffectSoundDictionary.Add("Flood", Resources.Load<AudioClip>("Audio/Wave/Flood"));
        EffectSoundDictionary.Add("Pest", Resources.Load<AudioClip>("Audio/Wave/Pest"));
        EffectSoundDictionary.Add("Cold", Resources.Load<AudioClip>("Audio/Wave/Cold"));
        EffectSoundDictionary.Add("HeavyRain", Resources.Load<AudioClip>("Audio/Wave/HeavyRain"));

        EffectSoundDictionary.Add("Farm", Resources.Load<AudioClip>("Audio/BGM/BGM"));
        EffectSoundDictionary.Add("StartScene", Resources.Load<AudioClip>("Audio/BGM/StartSceneBGM"));

        PlayBgm("StartScene");
    }

    public void PlayEffect(string name)
    {
        EffectPlayer.PlayOneShot(EffectSoundDictionary[name], EffectVolume);
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

}