using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>식물 오브젝트를 만들어 밭에 추가한다.</summary>
public class PlantSpawner
{
    private readonly Action<Plant, int> placePlant;
    private readonly Action onChiliPepperPlaced;
    private readonly PlantBoard board;
    private readonly GameObject peaPrefab;
    private readonly GameObject peanutPrefab;
    private readonly GameObject nepenthesPrefab;
    private readonly GameObject chiliPepperPrefab;
    private readonly GameObject moneyTreePrefab;
    private readonly GameObject sprinklerPrefab;

    public PlantSpawner(PlantBoard board, Action<Plant, int> placePlant, Action onChiliPepperPlaced, GameObject peaPrefab, GameObject peanutPrefab, GameObject nepenthesPrefab,
        GameObject chiliPepperPrefab, GameObject moneyTreePrefab, GameObject sprinklerPrefab)
    {
        this.board = board;
        this.placePlant = placePlant;
        this.onChiliPepperPlaced = onChiliPepperPlaced;
        this.peaPrefab = peaPrefab;
        this.peanutPrefab = peanutPrefab;
        this.nepenthesPrefab = nepenthesPrefab;
        this.chiliPepperPrefab = chiliPepperPrefab;
        this.moneyTreePrefab = moneyTreePrefab;
        this.sprinklerPrefab = sprinklerPrefab;
    }

    public GameObject InstantiatePlant(string plantName)
    {
        switch (plantName)
        {
            case PlantIds.Pea: return UnityEngine.Object.Instantiate(peaPrefab);
            case PlantIds.Peanut: return UnityEngine.Object.Instantiate(peanutPrefab);
            case PlantIds.Nepenthes: return UnityEngine.Object.Instantiate(nepenthesPrefab);
            case PlantIds.ChiliPepper: return UnityEngine.Object.Instantiate(chiliPepperPrefab);
            case PlantIds.Sprinkler: return UnityEngine.Object.Instantiate(sprinklerPrefab);
            case PlantIds.MoneyTree: return UnityEngine.Object.Instantiate(moneyTreePrefab);
            default: return UnityEngine.Object.Instantiate(peaPrefab);
        }
    }
    public Plant AddMovablePlant(List<GeneticTrait> trait, int grididx = -1)
    {
        GameObject obj = InstantiatePlant(GameManager.Instance.currentPlant);
        Plant plant = obj.GetComponent<Plant>();
        plant.SetTrait(trait);
        placePlant(plant, grididx);

        // 특수(임시땅콩A): 새 식물이 생겨날 때 가격의 50% 골드 (실제로 심어진 경우만)
        if (SpecialItemSystem.Has("peanut_special_4")
            && board.Plants.TryGetValue(plant.gridIndex, out Plant placed) && placed == plant)
            GameManager.Instance.economyManager.AddGold(Mathf.RoundToInt(plant.GetSellingPrice() * 0.5f));

        return plant;
    }
    public void AddNepenthes(int idx)
    {
        GameObject obj = UnityEngine.Object.Instantiate(nepenthesPrefab);
        Nepenthes nepenthes = obj.GetComponent<Nepenthes>();
        placePlant(nepenthes, idx);
    }
    public void AddChiliPepper(int idx)
    {
        GameObject obj = UnityEngine.Object.Instantiate(chiliPepperPrefab);
        ChiliPepper chiliPepper = obj.GetComponent<ChiliPepper>();
        placePlant(chiliPepper, idx);
        onChiliPepperPlaced();
    }
    public void AddMoneyTree(int idx)
    {
        GameObject obj = UnityEngine.Object.Instantiate(moneyTreePrefab);
        MoneyTree moneyTree = obj.GetComponent<MoneyTree>();
        placePlant(moneyTree, idx);
    }
    public void AddSprinkler(int idx)
    {
        GameObject obj = UnityEngine.Object.Instantiate(sprinklerPrefab);
        Sprinkler sprinkler = obj.GetComponent<Sprinkler>();
        placePlant(sprinkler, idx);
    }
    public void AddSuperPea()
    {
        List<GeneticTrait> peaTrait = new List<GeneticTrait>
        {
            new GeneticTrait(TraitType.NaturalDeath, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Pest, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Wind, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Flood, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.HeavyRain, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Cold, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Drought, 1f , 2, 0.0f),
            new GeneticTrait(TraitType.Heat, 1f , 2, 0.0f)
        };
        AddMovablePlant(peaTrait);
    }
}
