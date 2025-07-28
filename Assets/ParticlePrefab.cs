using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePrefab : MonoBehaviour
{
    [SerializeField] private ParticleSystem effect;

    public void PlayEffect()
    {
        effect.Play();
        Destroy(gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
    }


}
