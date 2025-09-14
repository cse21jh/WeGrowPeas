using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using DG.Tweening;

public class WaveManager : MonoBehaviour
{
    [SerializeField] LightColorController lcController;
    private float waveDuration = 1f;

    [SerializeField] ShadowLoop[] shadowLoops;
    [SerializeField] private float shadowSpeed = 10f;

    [SerializeField] GameObject waveEffectSkipButton;

    [Space(10)]
    [Header("바람 효과 관련")]
    [SerializeField] private ParticleSystem[] windEffects;
    [SerializeField] private CinemachineVirtualCamera[] vcams;
    [SerializeField] private float windStrength = 1f;
    [SerializeField] private float windFrequency = 1f;

    [Space(10)]
    [Header("홍수 효과 관련")]
    [SerializeField] private GameObject floodEffect;
    [SerializeField] private float floodStartPosX = 0;
    [SerializeField] private float floodEndPosX = 10f;
    [SerializeField] private Ease floodEase = Ease.InOutSine;

    [Space(10)]
    [Header("해충 효과 관련")]
    [SerializeField] private GameObject grassHopper;
    [SerializeField] private GameObject shadow;
    [SerializeField] private GameObject dust;
    [SerializeField] private float dustStartPosX = 0;
    [SerializeField] private float dustEndPosX = 10f;
    [SerializeField] private float dustMoveDuration = 1.5f;
    [SerializeField] private Ease dustEase = Ease.InOutSine;

    [Space(10)]
    [Header("추위 효과 관련")]
    [SerializeField] private ParticleSystem snowEffect;
    [SerializeField] private Material[] snowMats;
    [SerializeField] private float snowDuration = 1.5f;
    [SerializeField] private Ease snowEase = Ease.InOutSine;

    [Space(10)]
    [Header("폭우 효과 관련")]
    [SerializeField] private ParticleSystem rainEffect;
    [SerializeField] private GameObject lightningEffect;
    [SerializeField, Range(0f, 1f)] private float lightningDuration = 0.3f;
    [SerializeField, Range(0f, 10f)] private float maxLightningInterval = 3f;
    [SerializeField] private int lightningIndex = 0;
    [SerializeField] private int lightningCount = 4;


    public void StartWave(float duration, WaveType type)
    {
        waveDuration = duration;
        StartCoroutine(WaveEffect(type));
    }

    private IEnumerator WaveEffect(WaveType type)
    {
        float t;
        waveEffectSkipButton.SetActive(true);
        switch (type)
        {
            case WaveType.Aging:
                foreach (var shadowLoop in shadowLoops)
                {
                    shadowLoop.SetWaveSpeed(shadowSpeed);
                }

                float elapsed_aging = 0f;
                while (elapsed_aging < waveDuration)
                {
                    elapsed_aging += Time.deltaTime;
                    lcController.UpdateType(LightColorType.Natural);
                    lcController.time = Mathf.Clamp01(elapsed_aging / waveDuration);
                    yield return null;
                }
                lcController.time = 1f; // Ensure it ends exactly at 1

                foreach (var shadowLoop in shadowLoops)
                {
                    shadowLoop.SetNormalSpeed();
                }
                break;
            case WaveType.Wind:
                foreach (var windEffect in windEffects)
                {
                    windEffect.Play();
                }
                foreach (var vcam in vcams)
                {
                    vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = windStrength;
                    vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = windFrequency;
                }

                t = 0f;
                while (t < waveDuration)
                {
                    t += Time.deltaTime;
                    yield return null;
                }


                foreach (var windEffect in windEffects)
                {
                    windEffect.Stop();
                }
                foreach (var vcam in vcams)
                {
                    vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0f;
                }
                break;
            case WaveType.Flood:
                floodEffect.transform.position = new Vector3(floodStartPosX, floodEffect.transform.position.y, floodEffect.transform.position.z);
                floodEffect.SetActive(true);
                Plant[] plants = FindObjectsByType<Plant>(FindObjectsSortMode.None);
                foreach (var plant in plants)
                {
                    plant.PlayFoamEffect();
                }
                DOTween.To(()=> floodEffect.transform.position.x, x => floodEffect.transform.position = new Vector3(x, floodEffect.transform.position.y, floodEffect.transform.position.z), floodEndPosX, waveDuration).SetEase(floodEase);
                
                t = 0f;
                while (t < waveDuration)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                floodEffect.SetActive(false);
                break;
            case WaveType.Pest:
                dust.transform.position = new Vector3(dustStartPosX, dust.transform.position.y, dust.transform.position.z);
                grassHopper.SetActive(true);
                shadow.SetActive(true);
                dust.SetActive(true);
                DOTween.To(() => dust.transform.position.x, x => dust.transform.position = new Vector3(x, dust.transform.position.y, dust.transform.position.z), dustEndPosX, dustMoveDuration).SetEase(dustEase);
                t = 0f;
                while (t < waveDuration)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                grassHopper.SetActive(false);
                shadow.SetActive(false);
                dust.SetActive(false);
                break;
            case WaveType.Cold:
                Plant[] plantsSnow = FindObjectsByType<Plant>(FindObjectsSortMode.None);

                foreach (var plant in plantsSnow)
                {
                    plant.ShowSnow(snowDuration, snowEase);
                }


                snowEffect.Play();
                foreach (var mat in snowMats)
                {
                    float meltAmount = 1.2f;
                    DOTween.To(() => meltAmount,
                       x => { meltAmount = x; mat.SetFloat("_MeltStrength", x); },
                       -0.2f,
                       snowDuration)
                   .SetEase(snowEase);
                }

                t = 0f;
                while (t < waveDuration)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                foreach (var plant in plantsSnow)
                {
                    plant.HideSnow(snowDuration, snowEase);
                }

                foreach (var mat in snowMats)
                {
                    float meltAmount = -0.2f;
                    DOTween.To(() => meltAmount,
                       x => { meltAmount = x; mat.SetFloat("_MeltStrength", x); },
                       1.2f,
                       snowDuration)
                   .SetEase(snowEase);
                }

                snowEffect.Stop();
                break;
            case WaveType.HeavyRain:
                rainEffect.Play();
                lightningIndex = 0;
                StartCoroutine(Lightning());

                float elapsed_rain = 0f;
                while (elapsed_rain < waveDuration)
                {
                    elapsed_rain += Time.deltaTime;
                    lcController.UpdateType(LightColorType.Rain);
                    lcController.time = Mathf.Clamp01(elapsed_rain / waveDuration);
                    yield return null;
                }
                lcController.time = 1f; // Ensure it ends exactly at 1

                rainEffect.Stop();
                lightningIndex = lightningCount; // Stop further lightning
                StopCoroutine(Lightning());
                break;
            default:
                yield return null;
                break;
        }
        waveEffectSkipButton.SetActive(false);
    }

    private IEnumerator Lightning()
    {
        float lightningInterval = Random.Range(0f, maxLightningInterval);
        yield return new WaitForSeconds(lightningInterval);

        lightningEffect.SetActive(true);
        lightningIndex++;
        yield return new WaitForSeconds(lightningDuration);
        lightningEffect.SetActive(false);

        if (lightningIndex < lightningCount)
        {
            StartCoroutine(Lightning());
        }
    }

    public void SkipWaveEffect()
    {
        waveDuration = 0f;        
    }
}
