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


    public void SetFenceElements(List<GeneticTrait> peaTraits)
    { 
        Debug.Log($"SetFenceElements called with {peaTraits.Count} traits.");
        for (int i = 0; i < peaTraits.Count; i++)
        {
            switch (i)
            {
                case 0:
                    if (peaTraits[i].traitType == CompleteTraitType.NaturalDeath)
                    {
                        SetElements(i, peaTraits[i]);
                    }
                    else
                    {
                        ResetElements(i);
                    }
                    break;
                case 1:
                    if (peaTraits[i].traitType == CompleteTraitType.WindResistance)
                    {
                        SetElements(i, peaTraits[i]);
                    }
                    else
                    {
                        ResetElements(i);
                    }
                    break;
                case 2:
                    if (peaTraits[i].traitType == CompleteTraitType.FloodResistance)
                    {
                        SetElements(i, peaTraits[i]);
                    }
                    else
                    {
                        ResetElements(i);
                    }
                    break;
                case 3:
                    if (peaTraits[i].traitType == CompleteTraitType.PestResistance)
                    {
                        SetElements(i, peaTraits[i]);
                    }
                    else
                    {
                        ResetElements(i);
                    }
                    break;
                case 4:
                    if (peaTraits[i].traitType == CompleteTraitType.ColdResistance)
                    {
                        SetElements(i, peaTraits[i]);
                    }
                    else
                    {
                        ResetElements(i);
                    }
                    break;
                case 5:
                    if (peaTraits[i].traitType == CompleteTraitType.HeavyRainResistance)
                    {
                        SetElements(i, peaTraits[i]);
                    }
                    else
                    {
                        ResetElements(i);
                    }
                    break;
                default:
                    Debug.LogError($"Unexpected index {i} in SetFenceElements. Expected 0-5.");
                    ResetElements(i);
                    break;
            }
        }

        for(int i = peaTraits.Count; i < fenceElements.Length; i++)
        {
            ResetElements(i);
        }
    }

    private void SetElements(int index, GeneticTrait trait)
    {
        // If the trait is not None, set the element to active with the trait's properties
        float surviveProb = trait.resistance + trait.additionalResistance;
        int dnaIndex = (int)trait.genetics;
        bool isStarActive = false;
        //isStarActive = i < trait.taste;
        fenceElements[index].SetElement(true, "", surviveProb, dnaIndex, isStarActive);
    }

    private void ResetElements(int index)
    {
        // If the trait is None, set the element to inactive with the normal pea name
        fenceElements[index].SetElement(false, normalPeaName);
    }
}
