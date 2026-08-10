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
    public void ShowInfo(string speciesname, List<GeneticTrait> traits, Plant plant)
    {
        textSpecies.text = speciesname;
        string traitline = $"";

        for(int i = 0; i < traits.Count; i += 2)
        {
            string left = $"<sprite={(int)(traits[i].traitType+1)}> {((plant.GetResistanceValue(i)) * 100f):F2}% | {traits[i].genetics}";
            string right = (i + 1 < traits.Count) ? $"<sprite={(int)(traits[i + 1].traitType + 1)}> {((plant.GetResistanceValue(i+1)) * 100f):F2}% | {traits[i + 1].genetics}" : "";

            traitline += $"{left}\t{right}\n";
            traitline += $"맛 : {plant.GetTaste()}";
        }
        textStat.text = traitline;

        statPanel.SetActive(true);
    }

    public void UpdateInfo(string speciesname, List<GeneticTrait> traits, Plant plant)
    {
        textSpecies.text = speciesname;
        string traitline = $"";

        for (int i = 0; i < traits.Count; i += 2)
        {
            string left = $"<sprite={(int)(traits[i].traitType + 1)}> {((plant.GetResistanceValue(i)) * 100f):F2}% | {traits[i].genetics}";
            string right = (i + 1 < traits.Count) ? $"<sprite={(int)(traits[i + 1].traitType + 1)}> {((plant.GetResistanceValue(i + 1)) * 100f):F2}% | {traits[i + 1].genetics}" : "";

            traitline += $"{left}\t{right}\n";
            traitline += $"맛 : {plant.GetTaste()}";
        }
        textStat.text = traitline;

        statPanel.SetActive(true);
    }

    public void HideInfo()
    {
        statPanel.SetActive(false);
    }
}
