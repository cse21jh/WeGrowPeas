using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIPlantStat : MonoBehaviour
{
    [SerializeField] GameObject statPanel;

    public static UIPlantStat Instance { get; private set; }

    private void Awake()
    {
        statPanel.SetActive(false);
    }
    public void ShowInfo(string speciesname, List<GeneticTrait> traits)
    {
        Debug.Log(speciesname);
        statPanel.SetActive(true);
    }

    public void HideInfo()
    {
        statPanel.SetActive(false);
    }
}
