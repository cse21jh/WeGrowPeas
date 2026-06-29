using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;

public class FenceElementController : MonoBehaviour
{
    [Header("현재 웨이브 타입")]
    [SerializeField] private bool isWaveResistance = false; // 웨이브 저항 여부
    [SerializeField] private WaveType currentWaveType = WaveType.None; // 현재 웨이브 타입
    [SerializeField] private GameObject highlight; // 빛나는 효과
    [SerializeField] private bool LightActive = false; // 완두콩 빛나는 효과 활성화 여부

    [Header("완두콩 모습 관련")]
    [SerializeField] private TraitType currentTraitType = TraitType.None;
    [SerializeField] private GameObject[] peas;
    [SerializeField] private string[] peaNames;

    [Space(10)]
    [Header("땅콩 모습 관련")]
    [SerializeField] private GameObject[] peanuts;
    [SerializeField] private string[] peanutNames;

    [Space(10)]
    [Header("UI 요소들")]
    [SerializeField] private TextMeshProUGUI elementName;
    [SerializeField] private TextMeshProUGUI surviveProbability;
    [SerializeField] private Image[] dnaImages;
    [SerializeField] private Sprite[] dnaSprites;
    [SerializeField] private Image star;

    private void Start()
    {
        star.gameObject.SetActive(false);
    }

    public void SetLightActive(bool isActive, WaveType type)
    {
        LightActive = isActive;
        currentWaveType = (type == WaveType.Drought || type == WaveType.Heat) ? (WaveType)((int)type - 2) : type;
    }


    public void SetFaceAnim(List<Vector2> pairDatas)
    {
        bool isSet = false;

        for (int i = 0; i < pairDatas.Count; i++)
        {
            if(isSet == true)
            {
                break;
            }
            Debug.Log("페어데이터 확인 중... " + i + " / " + pairDatas[i].x + " " + pairDatas[i].y);

            if ((int)pairDatas[i].x == -1 || (int)pairDatas[i].y == -1)
            {
                continue;
            }

            if (pairDatas[i].x == (int)currentTraitType)
            {
                Debug.Log("형질에 맞는 얼굴 애니메이션 설정 시도! 타입 : " + currentTraitType + " / 페어데이터 : " + pairDatas[i].x + " " + pairDatas[i].y);

                foreach (GameObject pea in peas)
                {
                    if(pea.activeSelf == true)
                    {
                        Debug.Log("완두콩 얼굴 애니메이션 설정 완료! " + (TraitType)pairDatas[i].x + " / " + pairDatas[i].y + " in pea #" + (transform.GetSiblingIndex()-4));
                        Animator anim_pea = pea.GetComponentInChildren<AnimDelayController>().gameObject.GetComponent<Animator>();
                        anim_pea.Rebind();
                        anim_pea.SetInteger("faceIndex", (int)pairDatas[i].y);
                        anim_pea.SetTrigger("Start");
                        pairDatas.RemoveAt(i);

                        isSet = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                foreach (GameObject peanut in peanuts)
                {
                    if (peanut.activeSelf == true)
                    {
                        Debug.Log("땅콩 얼굴 애니메이션 설정 시도! 타입 : " + currentTraitType + " / 페어데이터 : " + pairDatas[i].x + " " + pairDatas[i].y);

                        Debug.Log("완두콩 얼굴 애니메이션 설정 완료! " + (TraitType)pairDatas[i].x + " / " + pairDatas[i].y + " in pea #" + (transform.GetSiblingIndex()-4));
                        Animator anim_peanut = peanut.GetComponentInChildren<AnimDelayController>().gameObject.GetComponent<Animator>();
                        anim_peanut.Rebind();
                        anim_peanut.SetInteger("faceIndex", (int)pairDatas[i].y);
                        anim_peanut.SetTrigger("Start");
                        pairDatas.RemoveAt(i);

                        isSet = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

        }


    }


    public void SetElement(int plantIndex, GeneticTrait trait, bool isTaste, Plant plant)
    {
        currentTraitType = trait.traitType;
        //Debug.Log($"SetElement called with trait: {trait.traitType}, isTaste: {isTaste}");
        float surviveProb = plant.GetResistanceValue((int)trait.traitType);
        int dnaIndex = (int)trait.genetics;

        //초기화
        foreach (GameObject pea in peas)
        {
            pea.SetActive(false);
        }
        foreach (GameObject peanut in peanuts)
        {
            peanut.SetActive(false);
        }

        isWaveResistance = false;
        //완두콩 및 땅콩 설정
        if (plantIndex == 0)
        {
            switch (trait.traitType)
            {
                case TraitType.NaturalDeath:
                    peas[1].SetActive(true);
                    elementName.text = peaNames[1];
                    if(currentWaveType == WaveType.Aging)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.Wind:
                    peas[2].SetActive(true);
                    elementName.text = peaNames[2];
                    if (currentWaveType == WaveType.Wind)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.Flood:
                    peas[3].SetActive(true);
                    elementName.text = peaNames[3];
                    if (currentWaveType == WaveType.Flood)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.Pest:
                    peas[4].SetActive(true);
                    elementName.text = peaNames[4];
                    if (currentWaveType == WaveType.Pest)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.Cold:

                    if(trait.genetics == 0)
                    {
                        // 유전자 0개
                        // heat
                        peas[8].SetActive(true);
                        elementName.text = peaNames[8];
                        if (currentWaveType == WaveType.Heat)
                        {
                            isWaveResistance = true;
                        }
                    }
                    else if(trait.genetics == 1)
                    {
                        // 유전자 1개
                        // 반반
                        peas[10].SetActive(true);
                        elementName.text = peaNames[10];
                        if (currentWaveType == WaveType.Cold || currentWaveType == WaveType.Heat)
                        {
                            isWaveResistance = true;
                        }
                    }
                    else
                    {
                        // 유전자 2개
                        // cold
                        peas[5].SetActive(true);
                        elementName.text = peaNames[5];
                        if (currentWaveType == WaveType.Cold)
                        {
                            isWaveResistance = true;
                        }
                    }
                    break;
                case TraitType.HeavyRain:

                    if (trait.genetics == 0)
                    {
                        // 유전자 0개
                        // drought
                        peas[7].SetActive(true);
                        elementName.text = peaNames[7];
                        if (currentWaveType == WaveType.Drought)
                        {
                            isWaveResistance = true;
                        }
                    }
                    else if (trait.genetics == 1)
                    {
                        // 유전자 1개
                        // 반반
                        peas[9].SetActive(true);
                        elementName.text = peaNames[9];
                        if (currentWaveType == WaveType.Drought || currentWaveType == WaveType.HeavyRain)
                        {
                            isWaveResistance = true;
                        }
                    }
                    else
                    {
                        // 유전자 2개
                        // heavyRain
                        peas[6].SetActive(true);
                        elementName.text = peaNames[6];
                        if (currentWaveType == WaveType.HeavyRain)
                        {
                            isWaveResistance = true;
                        }
                    }



                    
                    break;
                    /*
                case TraitType.Drought:
                    peas[7].SetActive(true);
                    elementName.text = peaNames[7];
                    if (currentWaveType == WaveType.Drought)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.Heat:
                    peas[8].SetActive(true);
                    elementName.text = peaNames[8];
                    if (currentWaveType == WaveType.Heat)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.FD:
                    peas[9].SetActive(true);
                    elementName.text = peaNames[9];
                    if (currentWaveType == WaveType.Drought || currentWaveType == WaveType.Flood)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.CH:
                    peas[10].SetActive(true);
                    elementName.text = peaNames[10];
                    if (currentWaveType == WaveType.Cold || currentWaveType == WaveType.Heat)
                    {
                        isWaveResistance = true;
                    }
                    break;
                    */
                default:
                    peas[0].SetActive(true); // 기본 완두콩
                    elementName.text = peaNames[0];
                    isWaveResistance = false; // 현재 표시 중인 완두콩이 웨이브 저항이 없다면(기본 완두콩이라면) false로 설정
                    break;
            }
        }
        else
        {
            switch (trait.traitType)
            {
                case TraitType.NaturalDeath:
                    peanuts[1].SetActive(true);
                    elementName.text = peanutNames[1];
                    break;
                case TraitType.Wind:
                    peanuts[2].SetActive(true);
                    elementName.text = peanutNames[2];
                    break;
                case TraitType.Flood:
                    peanuts[3].SetActive(true);
                    elementName.text = peanutNames[3];
                    break;
                case TraitType.Pest:
                    peanuts[4].SetActive(true);
                    elementName.text = peanutNames[4];
                    break;
                case TraitType.Cold:
                    peanuts[5].SetActive(true);
                    elementName.text = peanutNames[5];
                    break;
                case TraitType.HeavyRain:
                    peanuts[6].SetActive(true);
                    elementName.text = peanutNames[6];
                    break;
                default:
                    peanuts[0].SetActive(true); // 기본 땅콩
                    elementName.text = peanutNames[0];
                    isWaveResistance = false; // 현재 표시 중인 땅콩이 웨이브 저항이 없다면(기본 땅콩이라면) false로 설정
                    break;
            }
        }

        if (isWaveResistance)
        {
            // 웨이브 저항이 있는 경우
            highlight.SetActive(LightActive);   // 효과 활성화 여부에 따라 설정
        }
        else
        {
            // 웨이브 저항이 없는 경우
            highlight.SetActive(false);         // 빛나는 효과 비활성화
        }


        surviveProbability.gameObject.SetActive(true);

        if (trait.traitType == TraitType.HeavyRain)
        {
            surviveProbability.text = (surviveProb * 100f).ToString("F0") + "%\n" + (plant.GetResistanceValue((int)TraitType.Drought) * 100f) .ToString("F0") + "%";
        }
        else if(trait.traitType == TraitType.Cold)
        {
            surviveProbability.text = (surviveProb * 100f).ToString("F0") + "%\n" + (plant.GetResistanceValue((int)TraitType.Heat) * 100f).ToString("F0") + "%";
        }
        else
        {
            surviveProbability.text = (surviveProb * 100f).ToString("F0") + "%";
        }


        dnaImages[0].gameObject.SetActive(true);
        dnaImages[1].gameObject.SetActive(true);
        switch (dnaIndex)
        {
            case 0:
                dnaImages[0].sprite = dnaSprites[0];
                dnaImages[1].sprite = dnaSprites[0];
                break;
            case 1:
                dnaImages[0].sprite = dnaSprites[1];
                dnaImages[1].sprite = dnaSprites[0];
                break;
            case 2:
                dnaImages[0].sprite = dnaSprites[1];
                dnaImages[1].sprite = dnaSprites[1];
                break;
        }

        //star.gameObject.SetActive(isTaste);
    }


}
