using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FenceElementController : MonoBehaviour
{
    [Header("현재 웨이브 타입")]
    [SerializeField] private bool isWaveResistance = false; // 웨이브 저항 여부
    [SerializeField] private WaveType currentWaveType = WaveType.None; // 현재 웨이브 타입
    [SerializeField] private GameObject highlight; // 빛나는 효과
    [SerializeField] private bool LightActive = false; // 완두콩 빛나는 효과 활성화 여부

    [Header("완두콩 모습 관련")]
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


    public void SetElement(int plantIndex, GeneticTrait trait, bool isTaste, Plant plant)
    {
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
                    peas[5].SetActive(true);
                    elementName.text = peaNames[5];
                    if (currentWaveType == WaveType.Cold)
                    {
                        isWaveResistance = true;
                    }
                    break;
                case TraitType.HeavyRain:
                    peas[6].SetActive(true);
                    elementName.text = peaNames[6];
                    if (currentWaveType == WaveType.HeavyRain)
                    {
                        isWaveResistance = true;
                    }
                    break;
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
