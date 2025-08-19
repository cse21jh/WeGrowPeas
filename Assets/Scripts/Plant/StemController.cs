using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class StemController : MonoBehaviour
{
    [SerializeField] private PeaSpriteController[] peaSprites;
    [SerializeField] private Animator[] peaAnimators;
    [SerializeField] private float maxStartDelay = 0.1f; // 애니메이션 시작 지연 시간
    [SerializeField] private GameObject electricEffectPrefab;

    [SerializeField] private bool isDebugMode = false;


    private void Start()
    {
        if (isDebugMode)
        {
            SetTraits(new List<GeneticTrait> { 
                new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0f),
                new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1, 0f),
                new GeneticTrait(CompleteTraitType.FloodResistance, 0.5f, 1, 0f),
                new GeneticTrait(CompleteTraitType.PestResistance, 0.5f, 1, 0f),
                new GeneticTrait(CompleteTraitType.ColdResistance, 0.5f, 1, 0f),
                new GeneticTrait(CompleteTraitType.HeavyRainResistance, 0.5f, 1, 0f),
            });
        }

        StartCoroutine(StartPodAnim());
    }

    private IEnumerator StartPodAnim()
    {
        for(int i = 0; i < peaAnimators.Length; i++)
        {
            if (peaAnimators[i] != null)
            {
                float rand = Random.Range(0f, maxStartDelay);
                //Debug.Log($"Animator {i} start delay: {rand} seconds");
                yield return new WaitForSeconds(rand);
                peaAnimators[i].SetTrigger("Start");
            }
        }
    }


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
