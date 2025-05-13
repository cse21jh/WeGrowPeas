using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIPlantStat : MonoBehaviour
{
    [SerializeField] private GameObject statPanel;
    [SerializeField] private TextMeshProUGUI textSpecies;
    [SerializeField] private TextMeshProUGUI textStat;

    public static UIPlantStat Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        statPanel.SetActive(false);
    }
    public void ShowInfo(string speciesname, List<GeneticTrait> traits)
    {
        textSpecies.text = speciesname;
        string traitline = $"";

        foreach (var trait in traits)
        {
            string temp = $"{trait.traitType} | {(trait.resistance * 100f):F1}% | {trait.genetics}\n";
            traitline += temp;
        }
        textStat.text = traitline;

        statPanel.SetActive(true);
    }

    public void HideInfo()
    {
        statPanel.SetActive(false);
    }
}
