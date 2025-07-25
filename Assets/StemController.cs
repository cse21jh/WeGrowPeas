using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class StemController : MonoBehaviour
{
    [SerializeField] private PeaSpriteController[] peaSprites;



    public void SetTraits(List<GeneticTrait> traits)
    {
        for (int i = 0; i < traits.Count; i += 1)
        {
            peaSprites[i].SetPeaSprite((int)traits[i].traitType);
        }
    }
}
