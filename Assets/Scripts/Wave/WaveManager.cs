using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WaveManager : MonoBehaviour
{
    [SerializeField] LightColorController lcController;
    private float waveDuration = 1f;

    [SerializeField] ShadowLoop[] shadowLoops;
    [SerializeField] private float shadowSpeed = 10f;


    public void StartWave(float duration)
    {
        waveDuration = duration;
        StartCoroutine(WaveEffect());
    }

    private IEnumerator WaveEffect()
    {
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
    }
}
