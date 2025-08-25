using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cinemachine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] LightColorController lcController;
    private float waveDuration = 1f;

    [SerializeField] ShadowLoop[] shadowLoops;
    [SerializeField] private float shadowSpeed = 10f;

    [Space(10)]
    [Header("웨이브 별 효과 관련")]
    [SerializeField] private ParticleSystem[] windEffects;
    [SerializeField] private CinemachineVirtualCamera[] vcams;


    public void StartWave(float duration, WaveType type)
    {
        waveDuration = duration;
        StartCoroutine(WaveEffect(type));
    }

    private IEnumerator WaveEffect(WaveType type)
    {
        switch (type)
        {
            case WaveType.Aging:
                foreach (var shadowLoop in shadowLoops)
                {
                    shadowLoop.SetWaveSpeed(shadowSpeed);
                }

                float elapsed = 0f;
                while (elapsed < waveDuration)
                {
                    elapsed += Time.deltaTime;
                    lcController.time = Mathf.Clamp01(elapsed / waveDuration);
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
                    vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 1f;
                }


                yield return new WaitForSeconds(waveDuration);


                foreach (var windEffect in windEffects)
                {
                    windEffect.Stop();
                }
                foreach (var vcam in vcams)
                {
                    vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0f;
                }
                break;
            default:
                yield return null;
                break;
        }
    }
}
