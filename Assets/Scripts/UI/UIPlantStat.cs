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

        for(int i = 0; i < traits.Count; i += 2)
        {
            string left = $"<sprite={(int)(traits[i].traitType+1)}> {((traits[i].resistance + traits[i].additionalResistance) * 100f):F2}% | {traits[i].genetics}";
            string right = (i + 1 < traits.Count) ? $"<sprite={(int)(traits[i + 1].traitType + 1)}> {((traits[i + 1].resistance + traits[i + 1].additionalResistance) * 100f):F2}% | {traits[i + 1].genetics}" : "";

            traitline += $"{left}\t{right}\n";
        }
        textStat.text = traitline;

        statPanel.SetActive(true);
    }

    public void HideInfo()
    {
        statPanel.SetActive(false);
    }
}
