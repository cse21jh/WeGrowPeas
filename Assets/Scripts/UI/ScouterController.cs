using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>저항력·골드 스카우터의 보유 여부, 켜고 끄기, 식물 위 표시를 맡는다.</summary>
public class ScouterController : MonoBehaviour
{
    [SerializeField] private GameObject scouterButton; // 스카우터 온오프 버튼
    [SerializeField] private bool isScouterOn = false;

    private bool hasResistanceScouter = false;
    private bool hasGoldScouter = false;
    private Grid grid;

    private Grid Grid => grid != null ? grid : (grid = GetComponent<Grid>());

    public bool HasResistanceScouter => hasResistanceScouter;
    public bool HasGoldScouter => hasGoldScouter;
    public bool IsScouterOn => isScouterOn;

    private void Update()
    {
        if (GameInput.GetDown(GameAction.Scouter))
        {
            ToggleScouter();
        }
    }

    public void ShowScouterButton()
    {
        scouterButton.SetActive(true);
        scouterButton.GetComponent<Button>().onClick.RemoveAllListeners();
        scouterButton.GetComponent<Button>().onClick.AddListener(ToggleScouter);
        scouterButton.GetComponent<Button>().onClick.AddListener(() => SoundManager.Instance.PlayEffect(SoundIds.Button));
        HideGoldScouterImageInGrid();
        HideResistanceScouterImageInGrid();
    }
    public void ToggleScouter()
    {
        if (!hasResistanceScouter && !hasGoldScouter)
            return;

        if (isScouterOn)
        {
            isScouterOn = false;
            if (hasResistanceScouter)
                HideResistanceScouterImageInGrid();
            if (hasGoldScouter)
                HideGoldScouterImageInGrid();
        }
        else
        {
            isScouterOn = true;
            if (hasResistanceScouter)
                UpdateResistanceScouterImageInGrid(Grid.Enemy.CurrentWave.WaveType);
            if (hasGoldScouter)
                UpdateGoldScouterImageInGrid();
        }
    }
    public void UpdateResistanceScouterImageInGrid(WaveType wave)
    {
        if (!hasResistanceScouter || !isScouterOn)
            return;
        foreach (var plant in Grid.Board.Plants.Values)
        {
            if (plant is MovablePlant p)
            {
                p.CheckResistanceScouterImage(wave);
            }
        }
    }
    public void UpdateGoldScouterImageInGrid()
    {
        if (!hasGoldScouter || !isScouterOn)
            return;
        List<MovablePlant> sortedPlants = Grid.Board.Plants.Values
                                                   .OfType<MovablePlant>()
                                                   .OrderByDescending(p => p.GetSellingPrice()) // GetSellingPrice() 결과가 높은 순으로 정렬
                                                   .ToList(); // 정렬된 결과를 리스트로 변환

        for (int i = 0; i < sortedPlants.Count; i++)
        {
            MovablePlant currentPlant = sortedPlants[i];

            if (i < 3)
            {
                currentPlant.ShowGoldScouterImage();
            }
            else
            {
                currentPlant.HideGoldScouterImage();
            }
        }
    }
    public void HideResistanceScouterImageInGrid()
    {
        foreach (var plant in Grid.Board.Plants.Values)
        {
            if (plant is MovablePlant p)
            {
                p.HideResistanceScouterImage();
            }
        }
    }
    public void HideGoldScouterImageInGrid()
    {
        foreach (var plant in Grid.Board.Plants.Values)
        {
            if (plant is MovablePlant p)
            {
                p.HideGoldScouterImage();
            }
        }
    }
    public void SetResistanceScouter(bool val)
    {
        if (hasResistanceScouter = val)
        {
            ShowScouterButton();
        }
    }
    public void SetGoldScouter(bool val)
    {
        if (hasGoldScouter = val)
        {
            ShowScouterButton();
        }
    }
}
