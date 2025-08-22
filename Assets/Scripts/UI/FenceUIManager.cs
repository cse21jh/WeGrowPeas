using System;
using System.Collections.Generic;
using UnityEngine;

public class FenceUIManager : MonoBehaviour
{
    public static FenceUIManager Instance { get; private set; }


    [SerializeField] private FenceElementController[] fenceElements;

    [SerializeField] private string normalPeaName = "일반 완두콩";


    private void Awake()
    {
        Instance = this;
    }


    public void SetFenceElements(int plantIndex, List<GeneticTrait> Traits, int taste)
    {

        foreach (var element in fenceElements)
        {
            element.gameObject.SetActive(true);
        }

        Debug.Log($"SetFenceElements called with {Traits.Count} traits." + Traits);
        for (int i = 0; i < Traits.Count; i++)
        {
            bool isTasteActive = i < taste;
            fenceElements[i].SetElement(plantIndex, Traits[i], isTasteActive);
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
            fenceElements[i].SetElement(plantIndex, defaultTrait, isTasteActive);
        }
    }

    public void HideFenceElements()
    {
        foreach (var element in fenceElements)
        {
            element.gameObject.SetActive(false);
        }
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
