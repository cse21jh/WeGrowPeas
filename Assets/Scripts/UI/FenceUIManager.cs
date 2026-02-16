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

    private int showingPlantGridIndex = -1;


    [Space(10)]
    [Header("Toggle Debug")]
    [SerializeField] private bool isToggleOn = false;
    [SerializeField] private int savedPlantIndex = -1;
    [SerializeField] private Plant savedPlant = null;

    [SerializeField] private int savedPlantIndex_stack = -1;
    [SerializeField] private Plant savedPlant_stack = null;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HideFenceElements();
    }

    #region 토글 기능 관련
    private void SaveData(int plantIndex, Plant plant)
    {
        if(savedPlant != null)
        {
            savedPlantIndex_stack = savedPlantIndex;
            savedPlant_stack = savedPlant;
        }

        savedPlantIndex = plantIndex;
        savedPlant = plant;
    }

    private void DeleteData(Plant plant)
    {
        // Debug.Log(plant + " " + savedPlant + " " + savedPlant_stack);

        if (savedPlant == plant)
        {
            savedPlant = savedPlant_stack;
            savedPlantIndex = savedPlantIndex_stack;

            savedPlant_stack = null;
            savedPlantIndex_stack = -1;
        }
        else if(savedPlant_stack == plant)
        {
            savedPlant_stack = null;
            savedPlantIndex_stack = -1;
        }
    }

    public void ToggleOn(int plantIndex, Plant plant)
    {
        SaveData(plantIndex, plant);

        SetFenceElements(plantIndex, plant);
    }

    public void ToggleOff(int plantIndex, Plant plant)
    {
        DeleteData(plant);

        if(savedPlant == null)
        {
            HideFenceElements();
        }
    }

    #endregion


    public void SetFenceElements(int plantIndex, Plant plant)
    {
        //if (isToggleOn) return;

        priceSign.SetPrice(plant.GetSellingPrice());

        showingPlantGridIndex = plant.gridIndex;

        StartCoroutine(ShowUI());
        /*
        foreach (var element in fenceElements)
        {
            element.gameObject.SetActive(true);
        }
        */


        List<GeneticTrait> Traits = plant.GetGeneticTrait();
        int taste = plant.GetTaste();
        FindAnyObjectByType<PriceSignController>().ShowTaste(taste);

        int tmp = 0;
        //이제 여기서 Taste 관리 안 함
        List<TraitType> types = new List<TraitType> {TraitType.NaturalDeath, TraitType.Pest, TraitType.Wind, TraitType.Flood, TraitType.HeavyRain, TraitType.Cold };
        for (int i = 0; i < Traits.Count; i++)
        {
            if ((int)Traits[i].traitType >= (int)TraitType.Drought)
            {
                break;
            }
            bool isTasteActive = (int)Traits[i].traitType < taste;
            fenceElements[(int)Traits[i].traitType].SetElement(plantIndex, Traits[i], isTasteActive, plant);
            tmp++;
            types.Remove(Traits[i].traitType);
        }

        foreach(var i in types)        
        {
            bool isTasteActive = (int)i < taste;
            GeneticTrait defaultTrait = new GeneticTrait
            {
                traitType = TraitType.None,
                resistance = 0f,
                additionalResistance = 0f,
                genetics = 0
            };
            fenceElements[(int)i].SetElement(plantIndex, defaultTrait, isTasteActive, plant);
        }



        List<Vector2> pairDatas = plant.GetPairData_TraitFace();
        for (int i = 0; i < fenceElements.Length; i++)
        {
            fenceElements[i].SetFaceAnim(pairDatas);
        }
    }

    public void HideFenceElements()
    {
        showingPlantGridIndex = -1;
        priceSign.HidePrice();
        StartCoroutine(HideUI());

        /*
        foreach (var element in fenceElements)
        {
            element.gameObject.SetActive(false);
        }
        */

        if(isToggleOn && savedPlant != null)
        {
            SetFenceElements(savedPlantIndex, savedPlant);
        }
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
                fenceElements[(int)WaveType.Aging].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Wind:
                fenceElements[(int)WaveType.Wind].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Flood:
                fenceElements[(int)WaveType.Flood].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Pest:
                fenceElements[(int)WaveType.Pest].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Cold:
                fenceElements[(int)WaveType.Cold].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.HeavyRain:
                fenceElements[(int)WaveType.HeavyRain].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Drought:
                fenceElements[(int)WaveType.Drought - 2].SetLightActive(true, wave.WaveType);
                break;
            case WaveType.Heat:
                fenceElements[(int)WaveType.Heat - 2].SetLightActive(true, wave.WaveType);
                break;
            default:
                break;
        }
    }

    public bool CheckFenceIsShowingMe(int gridNum)
    {
        if(showingPlantGridIndex == gridNum) return true;

        return false;
    }
}
