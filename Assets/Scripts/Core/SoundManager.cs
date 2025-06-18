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

        BGMVolume = 1f;
        EffectVolume = 0.3f;

        EffectSoundDictionary.Add("SelectPlant", Resources.Load<AudioClip>("Audio/CasualGameSounds/DM-CGS-21"));
        //EffectSoundDictionary.Add("MoveScene", Resources.Load<AudioClip>("Audio/CasualGameSounds/DM-CGS-26"));
        //EffectSoundDictionary.Add("GetItem", Resources.Load<AudioClip>("Audio/CasualGameSounds/DM-CGS-45"));
        
        EffectSoundDictionary.Add("KillBug", Resources.Load<AudioClip>("Audio/Shapeforms Audio Free Sound Effects/PUNCH_DESIGNED_HEAVY_23"));
        
        //EffectSoundDictionary.Add("Farming", Resources.Load<AudioClip>("Audio/BGM/farming_¾û¶×ÇÑ ÀÛ´ç¸ðÀÇ"));
        //EffectSoundDictionary.Add("Magician Cave", Resources.Load<AudioClip>("Audio/BGM/magician_´Ï°¡ ¸ÕÀú ÇßÀÝ¾Æ (Short)"));
        //EffectSoundDictionary.Add("Cake Shop", Resources.Load<AudioClip>("Audio/BGM/Shop_Fluffing a Duck"));
        //EffectSoundDictionary.Add("TutorialScene", Resources.Load<AudioClip>("Audio/BGM/tutorial_LP1607180062_ÀÌÇý¸°_Tongtong"));
        
        //EffectSoundDictionary.Add("MainMenu", EffectSoundDictionary["Cake Shop"]);
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