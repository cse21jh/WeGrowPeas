using UnityEngine;

/// <summary>JODAL-style vowel chatter driven by actual TMP character reveals.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(MandatoryMessagePopupController))]
public sealed class SpeechTypingSound : MonoBehaviour
{
    [SerializeField] private MandatoryMessagePopupController popup;
    [Header("말풍선 목소리")]
    [Tooltip("아, 에, 이, 오, 우 순서의 짧은 모음 효과음입니다.")]
    [SerializeField] private AudioClip[] vowels = new AudioClip[5];
    [Tooltip("설정창의 효과음 볼륨에 곱할 목소리 크기입니다.")]
    [SerializeField, Range(0f, 1f)] private float volume = .72f;
    [Tooltip("모음 효과음의 기본 음높이입니다. 1보다 낮으면 조금 더 낮고 부드럽게 들립니다.")]
    [SerializeField, Range(.5f, 1.5f)] private float basePitch = .88f;
    [SerializeField, Range(0f, .25f)] private float pitchVariation = .04f;
    [Tooltip("목소리의 날카로운 고음을 완화하는 저역 통과 필터입니다.")]
    [SerializeField, Range(1000f, 22000f)] private float lowPassCutoff = 4200f;
    [Tooltip("빠른 타이핑이나 느린 프레임에서 소리가 과하게 겹치지 않도록 제한합니다.")]
    [SerializeField, Range(0f, .2f)] private float minimumSyllableInterval = .024f;

    private readonly AudioSource[] sources = new AudioSource[6];
    private readonly AudioLowPassFilter[] filters = new AudioLowPassFilter[6];
    private int nextSource;
    private double nextSyllable, nextAllowedSyllable;
    private float EffectiveVolume => Mathf.Clamp01(volume) * Mathf.Clamp01(DeviceSettings.Data.effectVolume);

    private void Awake()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            var child = new GameObject("Speech syllable " + i);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 40;
            sources[i] = source;
            var filter = child.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = lowPassCutoff;
            filter.lowpassResonanceQ = 1f;
            filters[i] = filter;
        }
    }

    private void OnEnable()
    {
        if (popup == null) popup = GetComponent<MandatoryMessagePopupController>();
        popup.CharacterRevealed += PlayCharacter;
        popup.RevealStopped += StopPlayback;
    }

    private void LateUpdate()
    {
        float level = EffectiveVolume;
        // Read the shared setting so the settings slider also changes active syllables.
        foreach (var source in sources)
            if (source != null) source.volume = level;
        foreach (var filter in filters)
            if (filter != null) filter.cutoffFrequency = lowPassCutoff;
        if (level <= 0f) StopPlayback();
    }

    private void PlayCharacter(char character, int index)
    {
        if (!isActiveAndEnabled || !char.IsLetterOrDigit(character) || vowels == null || vowels.Length == 0)
            return;
        float level = EffectiveVolume;
        if (level <= 0f) return;
        double now = AudioSettings.dspTime;
        if (now < nextAllowedSyllable) return;
        var clip = vowels[VowelIndex(character) % vowels.Length];
        if (clip == null) return;
        var source = sources[nextSource];
        if (source == null) return;
        nextSource = (nextSource + 1) % sources.Length;
        source.Stop();
        source.clip = clip;
        source.pitch = basePitch + ((character * 17 + index * 31) % 101 / 50f - 1f) * pitchVariation;
        source.volume = level;
        // Keep any catch-up audio within 45ms instead of queuing a whole sentence.
        double at = System.Math.Max(now + .008, System.Math.Min(nextSyllable, now + .045));
        source.PlayScheduled(at);
        nextAllowedSyllable = now + minimumSyllableInterval;
        nextSyllable = at + .024;
    }

    private static int VowelIndex(char character)
    {
        if (character >= '\uAC00' && character <= '\uD7A3')
        {
            int vowel = (character - '\uAC00') / 28 % 21;
            if (vowel == 0 || vowel == 2 || vowel == 9) return 0;
            if (vowel == 1 || vowel == 3 || vowel == 5 || vowel == 7 || vowel == 10 || vowel == 14 || vowel == 15) return 1;
            if (vowel == 11 || vowel == 16 || vowel == 19 || vowel == 20) return 2;
            if (vowel == 4 || vowel == 6 || vowel == 8 || vowel == 12) return 3;
            return 4;
        }
        return character % 5;
    }

    public void StopPlayback()
    {
        foreach (var source in sources)
            if (source != null) source.Stop();
        nextSource = 0;
        nextSyllable = nextAllowedSyllable = 0;
    }

    private void OnDisable()
    {
        if (popup != null)
        {
            popup.CharacterRevealed -= PlayCharacter;
            popup.RevealStopped -= StopPlayback;
        }
        StopPlayback();
    }
}
