using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;
using DG.Tweening;
using System.Runtime.InteropServices;

public class WaveManager : MonoBehaviour
{
    [SerializeField] LightColorController lcController;
    private float waveDuration = 1f;

    [SerializeField] ShadowLoop[] shadowLoops;
    [SerializeField] private float shadowSpeed = 10f;

    [SerializeField] GameObject waveEffectSkipButton;

    [Space(10)]
    [Header("휴대폰 밤/낮 변경 효과 관련")]
    [SerializeField] private float dayNightTransitionDuration = 1f;


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
    [Header("더위 효과 관련")]
    [SerializeField] private Light2D heatLight;
    [SerializeField] private GameObject heatEffect;
    [SerializeField] private SpriteRenderer heatDistortionSprite;
    [SerializeField] private Color heatDistortionColor;
    [SerializeField] private float heatDuration = 1.5f;
    [SerializeField] private Ease heatEase = Ease.InOutSine;


    [Space(10)]
    [Header("폭우 효과 관련")]
    [SerializeField] private ParticleSystem rainEffect;
    [SerializeField] private GameObject lightningEffect;
    [SerializeField, Range(0f, 1f)] private float lightningDuration = 0.3f;
    [SerializeField, Range(0f, 10f)] private float maxLightningInterval = 3f;
    [SerializeField] private int lightningIndex = 0;
    [SerializeField] private int lightningCount = 4;

    [Space(10)]
    [Header("가뭄 효과 관련")]
    [SerializeField] private GameObject[] grassObjects;
    [SerializeField] private GameObject droughtEffect;
    [SerializeField] private Material grassMat;
    [SerializeField] private float dissolveDuration = 1.5f;
    [SerializeField] private Ease dissolveEase = Ease.InOutSine;


    // ================ Day/Night Effects ================



    public IEnumerator StartNightCoroutine()
    {
        waveDuration = dayNightTransitionDuration;
        lcController.UpdateType(LightColorType.Night);

        float elapsed_night = 0f;
        while (elapsed_night < waveDuration)
        {
            elapsed_night += Time.deltaTime;
            lcController.UpdateType(LightColorType.Night);
            lcController.time = Mathf.Clamp01(elapsed_night / waveDuration);
            yield return null;
        }
        lcController.time = 1f; // Ensure it ends exactly at 1
    }

    public IEnumerator StopNightCoroutine()
    {
        waveDuration = dayNightTransitionDuration;
        lcController.UpdateType(LightColorType.Day);

        float elapsed_day = 0f;
        while (elapsed_day < waveDuration)
        {
            elapsed_day += Time.deltaTime;
            lcController.UpdateType(LightColorType.Day);
            lcController.time = Mathf.Clamp01(elapsed_day / waveDuration);
            yield return null;
        }
        lcController.time = 1f; // Ensure it ends exactly at 1
    }



    // ================ Wave Effects ================



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
            case WaveType.Drought:

                droughtEffect.SetActive(true);

                float dissolveAmount = 0f;
                DOTween.To(() => dissolveAmount,
                   x => { dissolveAmount = x; grassMat.SetFloat("_DissolveAmount", x); },
                   1f,
                   dissolveDuration)
               .SetEase(dissolveEase);

                float droughtHeatAmount = 1f;
                DOTween.To(() => droughtHeatAmount,
                   x => { droughtHeatAmount = x; heatLight.intensity = x; },
                   1.5f,
                   heatDuration)
               .SetEase(heatEase);

                float elapsed_drought = 0f;
                while (elapsed_drought < waveDuration)
                {
                    elapsed_drought += Time.deltaTime;
                    lcController.UpdateType(LightColorType.Drought);
                    lcController.time = Mathf.Clamp01(elapsed_drought / waveDuration);
                    yield return null;
                }
                lcController.time = 1f; // Ensure it ends exactly at 1

                dissolveAmount = 1f;
                DOTween.To(() => dissolveAmount,
                   x => { dissolveAmount = x; grassMat.SetFloat("_DissolveAmount", x); },
                   0f,
                   dissolveDuration)
               .SetEase(dissolveEase);

                droughtHeatAmount = 1.5f;
                DOTween.To(() => droughtHeatAmount,
                   x => { droughtHeatAmount = x; heatLight.intensity = x; },
                   1f,
                   heatDuration)
               .SetEase(heatEase);

                droughtEffect.SetActive(false);

                break;                
            case WaveType.Heat:

                heatEffect.SetActive(true);
                float distortionAmount = 0f;
                DOTween.To(() => distortionAmount,
                   x => { distortionAmount = x; heatDistortionSprite.color = new Color(heatDistortionColor.r, heatDistortionColor.g, heatDistortionColor.b, x); },
                   1f,
                   heatDuration)
               .SetEase(heatEase);

                float heatAmount = 1f;
                DOTween.To(() => heatAmount,
                   x => { heatAmount = x; heatLight.intensity = x; },
                   3f,
                   heatDuration)
               .SetEase(heatEase);


                t = 0f;
                while (t < waveDuration)
                {
                    t += Time.deltaTime;
                    yield return null;
                }


                heatAmount = 3f;
                DOTween.To(() => heatAmount,
                   x => { heatAmount = x; heatLight.intensity = x; },
                   1f,
                   heatDuration)
               .SetEase(heatEase);

                distortionAmount = 1f;
                DOTween.To(() => distortionAmount,
                   x => { distortionAmount = x; heatDistortionSprite.color = new Color(heatDistortionColor.r, heatDistortionColor.g, heatDistortionColor.b, x); },
                   0f,
                   heatDuration)
               .SetEase(heatEase);
                heatEffect.SetActive(false);

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
