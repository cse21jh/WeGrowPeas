using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FenceUIManager : MonoBehaviour
{
    public static FenceUIManager Instance { get; private set; }


    [SerializeField] private FenceElementController[] fenceElements;
    [SerializeField] private RectTransform[] showPos;
    [SerializeField] private RectTransform[] hidePos;
    [SerializeField] private float showDelay = 0.1f;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private Ease moveEase = Ease.OutBack;

    [SerializeField] private int fenceAnimSytle = 0; // 0: move, 1: scale

    [SerializeField] private PriceSignController priceSign;


    private void Awake()
    {
        Instance = this;
    }


    public void SetFenceElements(int plantIndex, Plant plant)
    {
        priceSign.SetPrice(plant.GetSellingPrice());

        StartCoroutine(ShowUI());
        /*
        foreach (var element in fenceElements)
        {
            element.gameObject.SetActive(true);
        }
        */

        List<GeneticTrait> Traits = plant.GetGeneticTrait();
        int taste = plant.GetTaste();
        //Debug.Log($"SetFenceElements called with {Traits.Count} traits." + Traits);
        for (int i = 0; i < Traits.Count; i++)
        {
            bool isTasteActive = i < taste;
            fenceElements[i].SetElement(plantIndex, Traits[i], isTasteActive, plant);
        }

        for(int i = Traits.Count; i < fenceElements.Length; i++)
        {
            bool isTasteActive = i < taste;
            GeneticTrait defaultTrait = new GeneticTrait
            {
                traitType = CompleteTraitType.None,
                resistance = 0f,
                additionalResistance = 0f,
                genetics = 0
            };
            fenceElements[i].SetElement(plantIndex, defaultTrait, isTasteActive, plant);
        }
    }

    public void HideFenceElements()
    {
        priceSign.HidePrice();
        StartCoroutine(HideUI());

        /*
        foreach (var element in fenceElements)
        {
            element.gameObject.SetActive(false);
        }
        */
    }

    private IEnumerator ShowUI()
    {
        Sequence showSeq = DOTween.Sequence();

        for (int i = 0; i < fenceElements.Length; i++)
        {
            if(fenceAnimSytle == 0)
            {
                showSeq.Insert(i * showDelay, fenceElements[i].GetComponent<RectTransform>().DOAnchorPos(showPos[i].anchoredPosition, moveDuration).SetEase(Ease.OutBack));
            }
            else
            {
                showSeq.Insert(i * showDelay, fenceElements[i].GetComponent<RectTransform>().DOScale(new Vector3(0.8f, 0.8f, 0.8f), moveDuration).SetEase(Ease.OutBack));
            }                
        }

        yield return null;
    }

    private IEnumerator HideUI()
    {
        Sequence showSeq = DOTween.Sequence();

        for (int i = 0; i < fenceElements.Length; i++)
        {
            if (fenceAnimSytle == 0)
            {
                showSeq.Insert(i * showDelay, fenceElements[i].GetComponent<RectTransform>().DOAnchorPos(hidePos[i].anchoredPosition, moveDuration).SetEase(Ease.OutBack));
            }
            else
            {
                showSeq.Insert(i * showDelay, fenceElements[i].GetComponent<RectTransform>().DOScale(new Vector3(0, 0, 0), moveDuration).SetEase(Ease.OutBack));
            }
        }

        yield return null;
    }


    public void SetWaveHighlight(Wave wave)
    {
        foreach(var element in fenceElements)
        {
            element.SetLightActive(false, wave.WaveType);
        }

        switch (wave.WaveType)
        {
            case WaveType.Aging:
                fenceElements[0].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Wind:
                fenceElements[1].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Flood:
                fenceElements[2].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Pest:
                fenceElements[3].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Cold:
                fenceElements[4].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.HeavyRain:
                fenceElements[5].SetLightActive(true, wave.WaveType);
                break;
            default:
                break;
        }
    }
}
