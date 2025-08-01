using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class StemController : MonoBehaviour
{
    [SerializeField] private PeaSpriteController[] peaSprites;
    [SerializeField] private GameObject electricEffectPrefab;



    public void SetTraits(List<GeneticTrait> traits)
    {
        for (int i = 0; i < traits.Count; i += 1)
        {
            peaSprites[i].SetPeaSprite((int)traits[i].traitType);

            if (traits[i].traitType == CompleteTraitType.PestResistance)
            {
                GameObject effect = Instantiate(electricEffectPrefab, peaSprites[i].transform.position, Quaternion.identity);
                effect.transform.SetParent(peaSprites[i].transform);
                effect.transform.localPosition = Vector3.zero;
                SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
                sr.sortingOrder = peaSprites[i].GetComponent<SpriteRenderer>().sortingOrder + 2;
            }
        }
    }
}
