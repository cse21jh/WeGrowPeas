using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;


public enum PlantType
{
    Pea,
    Peanut
}


public class StemController : MonoBehaviour
{
    [SerializeField] private PeaSpriteController[] peaSprites;
    [SerializeField] private PeanutSpriteController[] peanutSprites;
    [SerializeField] private Animator[] peaAnimators;
    [SerializeField] private float maxStartDelay = 0.1f; // 애니메이션 시작 지연 시간
    [SerializeField] private GameObject electricEffectPrefab;

    [SerializeField] private bool isDebugMode = false;

    [Space(10)]
    [Header("Gold Effect")]
    [SerializeField] private bool isDebugGold = false;
    [SerializeField] private SpriteRenderer stem;
    [SerializeField] private SpriteRenderer left_back;
    [SerializeField] private SpriteRenderer left_front;
    [SerializeField] private SpriteRenderer right_back;
    [SerializeField] private SpriteRenderer right_front;
    [SerializeField] private GameObject goldCrown;
    [SerializeField] private Sprite[] normalSprites;    // 0: stem, 1: left_back, 2: left_front, 3: right_back, 4: right_front
    [SerializeField] private Sprite[] goldSprites;      // 0: stem, 1: left_back, 2: left_front, 3: right_back, 4: right_front


    private void Start()
    {
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


    public void SetTraits(List<GeneticTrait> traits, PlantType type = PlantType.Pea)
    {
        switch (type)
        {
            case PlantType.Pea:
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

                break;
            case PlantType.Peanut:
                for (int i = 0; i < traits.Count; i += 1)
                {
                    peanutSprites[i].SetPeanutSprite((int)traits[i].traitType);

                    if (traits[i].traitType == CompleteTraitType.PestResistance)
                    {
                        GameObject effect = Instantiate(electricEffectPrefab, peanutSprites[i].transform.position, Quaternion.identity);
                        effect.transform.SetParent(peanutSprites[i].transform);
                        effect.transform.localPosition = Vector3.zero;
                        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = peanutSprites[i].GetComponent<SpriteRenderer>().sortingOrder + 2;
                    }
                }

                break;
            default:
                Debug.LogError("Unknown PlantType");
                break;
        }
        SetGold(CheckGold(traits));
    }

    private bool CheckGold(List<GeneticTrait> traits)
    {
        int i = 0;
        foreach(var t in traits)
        {
            if (t.genetics == 2)
                i++;
        }

        if (i == 6)
            return true;

        return false;
    }

    private void SetGold(bool isGold)
    {
        if (isGold)
        {
            stem.sprite = goldSprites[0];
            left_back.sprite = goldSprites[1];
            left_front.sprite = goldSprites[2];
            right_back.sprite = goldSprites[3];
            right_front.sprite = goldSprites[4];
            goldCrown.SetActive(true);
        }
        else
        {
            stem.sprite = normalSprites[0];
            left_back.sprite = normalSprites[1];
            left_front.sprite = normalSprites[2];
            right_back.sprite = normalSprites[3];
            right_front.sprite = normalSprites[4];
            goldCrown.SetActive(false);
        }
    }

}
