using DG.Tweening;
using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class PhoneAlarmEffectController : MonoBehaviour
{
    [SerializeField] private GameObject maxRoot;
    [SerializeField] private GameObject minRoot;

    [SerializeField] private float maxRotateZ = 5f;
    [SerializeField] private float minRotateZ = 15f;

    [SerializeField] private float alarmDuration = 5f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private int vibrato = 10;
    [SerializeField] private float alarmInterval = 1f;

    [SerializeField] private bool isPermanentOn = false;
    [SerializeField] private bool isImpermanentOn = false;

    [SerializeField] private bool isAlarmAble = true;

    [Space(10)]
    [Header("Vibration Sprite")]
    [SerializeField] private CanvasGroup[] vibWaves;
    [SerializeField] private float duration = 0.6f;    // 개별 파동이 커지는 시간
    [SerializeField] private float interval = 0.2f;    // 파동 간의 간격 (딜레이)
    [SerializeField] private float maxScale = 1.2f;    // 최대 크기
    [SerializeField] private Color permanentColor;
    [SerializeField] private Color periodColor;

    public void AlarmPermanent()
    {
        if (isPermanentOn || !isAlarmAble) return;
        StopAlarm();

        isPermanentOn = true;
        Debug.Log("Alarm Permanent On");

        StartCoroutine(AlarmEffectCoroutine(0));
        foreach (var wave in vibWaves)
        {
            wave.GetComponent<Image>().color = permanentColor;
        }
    }

    public void AlarmImpermanent()
    {
        if (isImpermanentOn || !isAlarmAble) return;
        StopAlarm();

        isImpermanentOn = true;

        StartCoroutine(AlarmEffectCoroutine(1));
        foreach (var wave in vibWaves)
        {
            wave.GetComponent<Image>().color = periodColor;
        }
    }

    public void StopAlarm()
    {
        Debug.Log("Alarm Off");
        isPermanentOn = false;
        isImpermanentOn = false;
        StopAllCoroutines();

        minRoot.transform.rotation = Quaternion.Euler(0, 0, 0);
        maxRoot.transform.rotation = Quaternion.Euler(0, 0, 0);

        foreach (var wave in vibWaves)
        {
            wave.alpha = 0f;
            wave.transform.localScale = Vector3.zero;
        }
    }

    public void DisableAlarm()
    {
        isAlarmAble = false;
        StopAlarm();
    }

    public void EnableAlarm()
    {
        isAlarmAble = true;
        if (PhoneManager.Instance != null
            && PhoneManager.Instance.TotalPhoneAlarmState == AlarmState.Mandatory
            && PhoneManager.Instance.ShouldResumePermanentAlarm)
        {
            AlarmPermanent();
        }

    }

    private IEnumerator AlarmEffectCoroutine(int count)
    {
        // 1. minRoot 흔들기 (기존 강도)
        minRoot.transform.DOShakeRotation(alarmDuration, new Vector3(0, 0, strength), vibrato, 90, false, ShakeRandomnessMode.Harmonic)
            .SetLoops(2, LoopType.Restart).SetLink(minRoot);

        // 2. maxRoot 흔들기 (강도를 strength * 0.2f 정도로 약하게 설정)
        // 0.2f는 원하시는 느낌에 따라 0.1f ~ 0.3f 사이로 조절해보세요.
        //maxRoot.transform.DOShakeRotation(alarmDuration, new Vector3(0, 0, strength * 0.1f), vibrato, 90, false, ShakeRandomnessMode.Harmonic)
        //    .SetLoops(2, LoopType.Restart);

        // 3. 파동 효과 실행
        PlayVibration(2);

        count--;

        yield return new WaitForSecondsRealtime(alarmInterval);

        if (count != 0)
        {
            StartCoroutine(AlarmEffectCoroutine(count));
        }
        else
        {
            StopAlarm();
        }
    }

    public void PlayVibration(int loopCount)
    {
        SoundManager.Instance.PlayEffect("Vibration");

        // 시퀀스 생성
        Sequence mainSeq = DOTween.Sequence().SetLink(gameObject);

        for (int i = 0; i < vibWaves.Length; i++)
        {
            CanvasGroup wave = vibWaves[i];
            float delay = i * interval; // 안쪽부터 순서대로 딜레이 계산

            // 각 파동의 개별 연출을 Join으로 묶어서 실행 (시작 시점은 delay로 조절)
            mainSeq.Join(CreateWaveSequence(wave, i));
        }

        // 전체 시퀀스 반복 설정
        mainSeq.SetLoops(loopCount, LoopType.Restart);
    }

    private Sequence CreateWaveSequence(CanvasGroup wave, int index)
    {
        // 초기화
        wave.alpha = 0f;
        wave.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence().SetLink(gameObject);

        // 1. 인덱스에 따른 가변 파라미터 설정
        float startDelay = index * interval;
        // 인덱스가 커질수록 최대 크기가 0.2f씩 증가 (예: 1.2 -> 1.4 -> 1.6)
        float indexedMaxScale = maxScale + (index * 0.2f);

        // 2. 스케일 애니메이션
        seq.Append(wave.transform.DOScale(indexedMaxScale, duration)
            .SetDelay(startDelay)
            .SetEase(Ease.OutQuad)).SetLink(wave.transform.gameObject);

        // 3. 페이드인 타이밍 (인덱스가 높을수록 더 커진 상태에서 등장)
        // index * 0.2f를 더해줄수록 더 늦게(더 커졌을 때) 나타납니다.
        float fadeInDelay = startDelay + (index * 0.2f);
        float fadeInDuration = duration * 0.3f;

        seq.Insert(fadeInDelay, wave.DOFade(1f, fadeInDuration).SetEase(Ease.OutCubic)).SetLink(wave.transform.gameObject);

        // 4. 페이드아웃 (스케일 끝자락에 맞춰 소멸)
        float fadeOutStart = startDelay + (duration * 0.7f);
        seq.Insert(fadeOutStart, wave.DOFade(0f, duration * 0.3f)).SetLink(wave.transform.gameObject);

        return seq;
    }
}
