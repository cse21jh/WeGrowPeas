using System.Collections;
using System.Collections.Generic;
//using Unity.Android.Types;
using UnityEngine;
using static UnityEngine.ParticleSystem;


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
    [SerializeField] private bool isGold = false;
    [SerializeField] private SpriteRenderer stem;
    [SerializeField] private SpriteRenderer left_back;
    [SerializeField] private SpriteRenderer left_front;
    [SerializeField] private SpriteRenderer right_back;
    [SerializeField] private SpriteRenderer right_front;
    [SerializeField] private GameObject goldCrown;
    [SerializeField] private Sprite[] normalSprites;    // 0: stem, 1: left_back, 2: left_front, 3: right_back, 4: right_front
    [SerializeField] private Sprite[] goldSprites;      // 0: stem, 1: left_back, 2: left_front, 3: right_back, 4: right_front

    [SerializeField] private SpriteRenderer[] back;
    [SerializeField] private SpriteRenderer[] front;


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


    public void SetTraits(List<GeneticTrait> traits, PlayablePlantType type = PlayablePlantType.Pea)
    {
        switch (type)
        {
            case PlayablePlantType.Pea:
                for (int i = 0; i < traits.Count; i += 1)
                {
                    if ((int)traits[i].traitType >= (int)TraitType.Drought)
                        continue; // 가뭄, 더위는 폭우, 추위 판단 할 때 같이

                    if (traits[i].traitType == TraitType.HeavyRain) // 홍수는 유전자 개수 따라서 스프라이트 다르게
                    {
                        switch(traits[i].genetics)
                        {
                            case 0:
                                peaSprites[i].SetPeaSprite((int)TraitType.Drought);
                                break;
                            case 1:
                                peaSprites[i].SetPeaSprite((int)TraitType.FD);
                                break;
                            case 2:
                                peaSprites[i].SetPeaSprite((int)traits[i].traitType);
                                break;
                        }
                        continue;
                    }

                    if (traits[i].traitType == TraitType.Cold) // 추위는 유전자 개수 따라서 스프라이트 다르게
                    {
                        switch (traits[i].genetics)
                        {
                            case 0:
                                peaSprites[i].SetPeaSprite((int)TraitType.Heat);
                                break;
                            case 1:
                                peaSprites[i].SetPeaSprite((int)TraitType.CH);
                                break;
                            case 2:
                                peaSprites[i].SetPeaSprite((int)traits[i].traitType);
                                break;
                        }
                        continue;
                    }

                    peaSprites[i].SetPeaSprite((int)traits[i].traitType);

                    if (traits[i].traitType == TraitType.Pest)
                    {
                        GameObject effect = Instantiate(electricEffectPrefab, peaSprites[i].transform.position, Quaternion.identity);
                        effect.transform.SetParent(peaSprites[i].transform);
                        effect.transform.localPosition = Vector3.zero;
                        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = peaSprites[i].GetComponent<SpriteRenderer>().sortingOrder + 2;
                    }
                }

                break;
            case PlayablePlantType.Peanut:
                for (int i = 0; i < traits.Count; i += 1)
                {
                    if ((int)traits[i].traitType >= (int)TraitType.Drought)
                        continue; // 가뭄, 더위는 폭우, 추위 판단 할 때 같이

                    if (traits[i].traitType == TraitType.HeavyRain) // 홍수는 유전자 개수 따라서 스프라이트 다르게
                    {
                        switch (traits[i].genetics)
                        {
                            case 0:
                                peanutSprites[i].SetPeanutSprite((int)TraitType.Drought);
                                break;
                            case 1:
                                peanutSprites[i].SetPeanutSprite((int)TraitType.None + 1);
                                break;
                            case 2:
                                peanutSprites[i].SetPeanutSprite((int)traits[i].traitType);
                                break;
                        }
                        continue;
                    }

                    if (traits[i].traitType == TraitType.Cold) // 추위는 유전자 개수 따라서 스프라이트 다르게
                    {
                        switch (traits[i].genetics)
                        {
                            case 0:
                                peanutSprites[i].SetPeanutSprite((int)TraitType.Heat);
                                break;
                            case 1:
                                peanutSprites[i].SetPeanutSprite((int)TraitType.None + 2);
                                break;
                            case 2:
                                peanutSprites[i].SetPeanutSprite((int)traits[i].traitType);
                                break;
                        }
                        continue;
                    }
                    peanutSprites[i].SetPeanutSprite((int)traits[i].traitType);

                    if (traits[i].traitType == TraitType.Pest)
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

    public bool CheckGold(List<GeneticTrait> traits)
    {
        int n = 0;
        foreach(var t in traits)
        {
            if (t.resistance + t.additionalResistance < 0.7999f) //부동소수점 이슈로 임시로 이렇게 처리하겠습니다 ㅎ
                return  isGold = false;
            n++;
        }
        if (n == (int)TraitType.None)
        {
            FindAnyObjectByType<FirstGoldManager>().SetFirstGold();
            UnlockManager.Unlock(UnlockManager.Ids.GoldenPlantCreated); // 황금 비료 해금
            return isGold = true;
        }
        return isGold = false;
    }

    public void SetGold(bool isG)
    {
        if (isG)
        {
            stem.sprite = goldSprites[0];
            for(int i = 0; i < back.Length; i++)
            {
                back[i].sprite = goldSprites[i + 1];
            }
            for(int i = 0; i < front.Length; i++)
            {
                front[i].sprite = goldSprites[i + 3];
            }
            //left_back.sprite = goldSprites[1];
            //left_front.sprite = goldSprites[2];
            //right_back.sprite = goldSprites[3];
            //right_front.sprite = goldSprites[4];
            goldCrown.SetActive(true);
            isGold = true;
        }
        else
        {
            stem.sprite = normalSprites[0];
            for (int i = 0; i < back.Length; i++)
            {
                back[i].sprite = normalSprites[i + 1];
            }
            for (int i = 0; i < front.Length; i++)
            {
                front[i].sprite = normalSprites[i + 3];
            }
            //left_back.sprite = normalSprites[1];
            //left_front.sprite = normalSprites[2];
            //right_back.sprite = normalSprites[3];
            //right_front.sprite = normalSprites[4];
            goldCrown.SetActive(false);
            isGold = false;
        }
    }

    public bool IsGold()
    {
        return isGold;
    }
}
