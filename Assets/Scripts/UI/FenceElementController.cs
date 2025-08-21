using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FenceElementController : MonoBehaviour
{
    [Header("¿ÏµÎÄá ¸ð½À °ü·Ã")]
    [SerializeField] private GameObject[] peas;
    [SerializeField] private string[] peaNames;

    [Space(10)]
    [Header("¶¥Äá ¸ð½À °ü·Ã")]
    [SerializeField] private GameObject[] peanuts;
    [SerializeField] private string[] peanutNames;

    [Space(10)]
    [Header("UI ¿ä¼Òµé")]
    [SerializeField] private TextMeshProUGUI elementName;
    [SerializeField] private TextMeshProUGUI surviveProbability;
    [SerializeField] private Image[] dnaImages;
    [SerializeField] private Sprite[] dnaSprites;
    [SerializeField] private Image star;

    private void Start()
    {
    }

    public void SetElement(int plantIndex, GeneticTrait trait, bool isTaste)
    {
        Debug.Log($"SetElement called with trait: {trait.traitType}, isTaste: {isTaste}");
        float surviveProb = trait.resistance + trait.additionalResistance;
        int dnaIndex = (int)trait.genetics;

        //ÃÊ±âÈ­
        foreach (GameObject pea in peas)
        {
            pea.SetActive(false);
        }
        foreach (GameObject peanut in peanuts)
        {
            peanut.SetActive(false);
        }

        //¿ÏµÎÄá ¹× ¶¥Äá ¼³Á¤
        if (plantIndex == 0)
        {
            switch (trait.traitType)
            {
                case CompleteTraitType.NaturalDeath:
                    peas[1].SetActive(true);
                    elementName.text = peaNames[1];
                    break;
                case CompleteTraitType.WindResistance:
                    peas[2].SetActive(true);
                    elementName.text = peaNames[2];
                    break;
                case CompleteTraitType.FloodResistance:
                    peas[3].SetActive(true);
                    elementName.text = peaNames[3];
                    break;
                case CompleteTraitType.PestResistance:
                    peas[4].SetActive(true);
                    elementName.text = peaNames[4];
                    break;
                case CompleteTraitType.ColdResistance:
                    peas[5].SetActive(true);
                    elementName.text = peaNames[5];
                    break;
                case CompleteTraitType.HeavyRainResistance:
                    peas[6].SetActive(true);
                    elementName.text = peaNames[6];
                    break;
                default:
                    peas[0].SetActive(true); // ±âº» ¿ÏµÎÄá
                    elementName.text = peaNames[0];
                    break;
            }
        }
        else
        {
            switch (trait.traitType)
            {
                case CompleteTraitType.NaturalDeath:
                    peanuts[1].SetActive(true);
                    elementName.text = peanutNames[1];
                    break;
                case CompleteTraitType.WindResistance:
                    peanuts[2].SetActive(true);
                    elementName.text = peanutNames[2];
                    break;
                case CompleteTraitType.FloodResistance:
                    peanuts[3].SetActive(true);
                    elementName.text = peanutNames[3];
                    break;
                case CompleteTraitType.PestResistance:
                    peanuts[4].SetActive(true);
                    elementName.text = peanutNames[4];
                    break;
                case CompleteTraitType.ColdResistance:
                    peanuts[5].SetActive(true);
                    elementName.text = peanutNames[5];
                    break;
                case CompleteTraitType.HeavyRainResistance:
                    peanuts[6].SetActive(true);
                    elementName.text = peanutNames[6];
                    break;
                default:
                    peanuts[0].SetActive(true); // ±âº» ¿ÏµÎÄá
                    elementName.text = peanutNames[0];
                    break;
            }
        }


            surviveProbability.gameObject.SetActive(true);
        surviveProbability.text = (surviveProb * 100f).ToString("F0") + "%";


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

        star.gameObject.SetActive(isTaste);
    }


}
