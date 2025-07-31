using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{
    private EnemyController enemyController;

    List<Plant> plants = new List<Plant>();
    [HideInInspector] public int maxCol = 4;
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    private int additionalInheritance = 0;
    private float breedTimer = 30.0f;
    private int maxBreedCount = 4;
    private int breedCount = 0;
    public int BreedCount => breedCount;

    private bool isBreeding = false;

    private GameObject breedObj1 = null;
    private GameObject breedObj2 = null;
    private bool isBreedButtonPressed = false;

    private bool isBreedSkipButtonPressed = false;

    private float bugSpawnTimeInterval = 5.0f;
    
    private float additionalPestResistance = 0f;

    [SerializeField] private GameObject peaPrefab;
    //[SerializeField] private GameObject soilPrefab;
    [SerializeField] private GameObject[] disabledSoil; // 4�� �̻��� ���� �߰��� �� Ȱ��ȭ�Ǵ� ����
    [SerializeField] private List<GameObject> bugPrefabs;

    [SerializeField] private TimerUI breedTimerUI;
    [SerializeField] private GameObject breedButton;
    [SerializeField] private GameObject breedSkipButton;
    [SerializeField] private TextMeshProUGUI breedCountUI;
    
    public int killBugCount = 0;
    public int totalBreedCount = 0;

    [SerializeField] private Sprite[] gardenSprites; // ���� ��� ��������Ʈ��
    [SerializeField] private SpriteRenderer gardenRenderer; // ���� ��� ��������Ʈ ������


    // Start is called before the first frame update
    void Start()
    {
        enemyController = GameObject.Find("EnemyController").GetComponent<EnemyController>();
        //InitGrid();
        InitSoils();
        breedButton.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InitGrid()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = Instantiate(peaPrefab);
            Pea pea = obj.GetComponent<Pea>();
            List<GeneticTrait> basicTrait = new List<GeneticTrait>
            {
                new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f)
            };
            Debug.Log(basicTrait);
            pea.SetTrait(basicTrait);
            //plants.Add(pea);
            AddPlantToGrid(pea);
        }
    }

    public IEnumerator Breeding()
    {
        //breedTimer ��ŭ ���� �Ʒ� ���� �ݺ� ���� ����

        //������ �θ� �ϵ��� �� �� ����
        isBreeding = true;
        breedObj1 = null;
        breedObj2 = null;

        //int breedCount = 0;

        breedTimerUI.StartBreedingTimer();

        Debug.Log(breedTimer + "�� ����. �ִ� ���� Ƚ���� " + maxBreedCount + "�Դϴ�");
        UpdateBreedCountUI(maxBreedCount);
        float startTime = Time.time;
        float endTime = startTime + breedTimer;

        float spawnBugTime = startTime + bugSpawnTimeInterval;

        breedSkipButton.SetActive(true);
        enemyController.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;


        while (Time.time < endTime && !isBreedSkipButtonPressed)
        {
            if (Time.time > spawnBugTime)
            {
                List<int> targetIdx = new List<int>(plantGrid.Keys);
                if (targetIdx.Count > 0)
                {
                    SpawnRandomBug();
                    spawnBugTime += bugSpawnTimeInterval;
                }
            }

            if (isBreedButtonPressed)
            {
                if (breedObj1 != null && breedObj2 != null) // ���� ��ư ������ ���� ����
                {
                    Plant parent1 = breedObj1.GetComponent<Plant>();
                    Plant parent2 = breedObj2.GetComponent<Plant>();
                    //�ڽ� �ϵ��� ���� ��� �� Instantiate

                    bool canBreed = false;
                    for (int idx = 0; idx < maxCol * 4; idx++)
                    {
                        if (!plantGrid.ContainsKey(idx))
                        {
                            canBreed = true;
                        }
                    }
                    if (canBreed && breedCount < maxBreedCount)
                    {
                        List<GeneticTrait> childTrait = Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait());
                        GameObject childObj = Instantiate(peaPrefab);
                        Plant child = childObj.GetComponent<Plant>();
                        if (child != null)
                        {
                            child.SetTrait(childTrait);
                            //plants.Add(child);
                            AddPlantToGrid(child);
                            breedCount++;
                            Debug.Log("�ڽ� ���� ����. ���� ���� Ƚ���� " + (maxBreedCount - breedCount) + "�Դϴ�");
                            UpdateBreedCountUI(maxBreedCount - breedCount);
                            Plant p1 = breedObj1.GetComponent<Plant>();
                            Plant p2 = breedObj2.GetComponent<Plant>();
                            p1.MakeDefaultSprite();
                            p2.MakeDefaultSprite();
                            breedObj1 = null;
                            breedObj2 = null;
                            DeactivateBreed();
                        }
                        else
                        {
                            Debug.Log("�ڽ� ������ ���� �߻�");
                            Destroy(childObj);
                            isBreedButtonPressed = false;
                        }

                    }
                    else if (breedCount >= maxBreedCount)
                    {
                        Debug.Log("�ִ� ���� Ƚ�� �ʰ�");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }
                    else
                    {
                        Debug.Log("Ű�� ������ �����մϴ�");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }


                }
                else
                {
                    Debug.Log("���� �� ���� ��� �������� �ʾҽ��ϴ�");
                }
            }

            yield return null;
        }

        if(breedObj1 != null) breedObj1.GetComponent<Plant>().MakeDefaultSprite();
        if(breedObj2 != null) breedObj2.GetComponent<Plant>().MakeDefaultSprite();

        breedTimerUI.StopTimer();
        breedCount = 0;
        Debug.Log("���� ������ ����");
        breedButton.SetActive(false);
        enemyController.HideWaveSkipButton();
        isBreeding = false;
        breedSkipButton.SetActive(false);
        //Grid ���ε�

        yield return null;
    }

    private List<GeneticTrait> Breed(List<GeneticTrait> parent1, List<GeneticTrait> parent2)
    {
        List<GeneticTrait> childTrait = new List<GeneticTrait>();

        foreach (CompleteTraitType trait in System.Enum.GetValues(typeof(CompleteTraitType)))
        {
            if (trait == CompleteTraitType.None)
                break;

            int p1Trait;
            int p2Trait;

            int childGenetic = 0;

            int traitNotInParent = 0;

            if (parent1.Any(t => t.traitType == trait))
            {
                p1Trait = parent1.First(t => t.traitType == trait).genetics;
            }
            else
            {
                p1Trait = 2;
                traitNotInParent += 1;
            }

            if (parent2.Any(t => t.traitType == trait))
            {
                p2Trait = parent2.First(t => t.traitType == trait).genetics;
            }
            else
            {
                p2Trait = 2;
                traitNotInParent += 1;
            }

            if (traitNotInParent == 2)
                continue;

            switch (p1Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 1 : 0); break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 1 : 0); break;
                default: break;
            }

            float resistance = 0f;

            if (trait == CompleteTraitType.PestResistance)
                resistance += additionalPestResistance;

            switch (childGenetic)
            {
                case 0: resistance += 0.8f; break;
                default: resistance += 0.5f; break;
            }
            childTrait.Add(new GeneticTrait(trait, resistance, childGenetic, 0.0f));
        }
        SoundManager.Instance.PlayEffect("Breed");
        totalBreedCount++;
        return childTrait;
    }

    private void AddPlantToGrid(Plant plant)
    {
        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            if (!plantGrid.ContainsKey(idx))
            {
                plant.Init(idx, this);
                Plantplant(plant);

                return;
            }
        }

        Destroy(plant.gameObject);
        return;
    }

    public Transform GetSoilTransform(int idx)
    {
        int row = idx / 4;
        int col = idx % 4;

        Transform rowT = transform.GetChild(row);
        Transform colT = rowT.GetChild(col);

        return colT;
    }

    /*public void DestroyPlant(int gridNum)
    {
        Plant plant = plantGrid[gridNum];
        plant.Die();
        plantGrid.Remove(gridNum);
        return;
    }*/

    public void ClearGridIndex(int gridIndex)
    {
        if(plantGrid.ContainsKey(gridIndex)) plantGrid.Remove(gridIndex);

        if (CheckGameOver())
        {
            StartCoroutine(GameManager.Instance.GameOver());
        }
    }

    public bool CheckGameOver()
    { 
        if (plantGrid.Count == 0)
            return true;
        return false;
    }

    public void AddBreedTimer(int time)
    {
        breedTimer += time;
        return;
    }

    public float GetBreedTimer()
    {
        return breedTimer;
    }

    public void AddMaxBreedCount(int count)
    {
        maxBreedCount += count;
        return;
    }

    public void AddPlant(List<GeneticTrait> trait)
    {
        GameObject obj = Instantiate(peaPrefab);
        Pea pea = obj.GetComponent<Pea>();
        pea.SetTrait(trait);
        AddPlantToGrid(pea);
    }

    public int GetMaxCol()
    {
        return maxCol;
    }

    public void AddAdditionalResistanceInGrid(CompleteTraitType traitType, float value)
    {
        for (int idx = 0; idx < GetMaxCol() * 4; idx++) // grid�� �ִ� �Ĺ��� ���׷� ����
        {
            if (plantGrid.ContainsKey(idx))
            {
                Plant plant = plantGrid[idx];
                plant.AddAdditionalResistance(traitType, value);
            }
        }
        return;
    }

    public void AddAdditionalPestResistance(float value)
    {
        additionalPestResistance += value;
        if (additionalPestResistance > 0.15f)
            additionalPestResistance = 0.15f;
        AddAdditionalResistanceInGrid(CompleteTraitType.PestResistance, value);
    }

    public float GetAdditionalPestResistance()
    {
        return additionalPestResistance;
    }

    public void AddAdditionalInheritance(int value)
    {
        additionalInheritance += value;
        return;
    }

    public void AddSoil()
    {
        maxCol += 1;
        //GameObject obj = Instantiate(soilPrefab, this.transform);
        //obj.transform.localPosition = new Vector3(1.7f * (maxCol-1), 0f, 0f);

        gardenRenderer.sprite = gardenSprites[maxCol - 4]; // ���� ��� ��������Ʈ ����

        disabledSoil[maxCol - 5].SetActive(true);

        //for (int row = 0; row < 4; row++)
        //{
        //    Transform soilT = disabledSoil[maxCol - 5].transform.GetChild(row);
        //    Soil soil = soilT.GetComponent<Soil>();

        //    if (soil != null)
        //    {
        //        int index = row + (maxCol - 1) * 4;
        //        soil.Init(index);
        //    }
        //}
    }
    private void InitSoils()
    {
        for (int col = 0; col < 8; col++)
        {
            Transform soilColT = transform.GetChild(col);
            for (int row = 0; row < 4; row++)
            {
                Transform soilT = soilColT.GetChild(row);
                Soil soil = soilT.GetComponent<Soil>();
                if (soil != null)
                {
                    int index = col * 4 + row;
                    soil.Init(index);
                }
            }
        }
    }

    public void RequestBreedSelect(GameObject clickedObject)
    {
        Plant clickedPea = clickedObject.GetComponent<Plant>();
        if (clickedPea == null) return;

        if (breedObj1 == clickedObject)
        {
            // �θ� 1 ���� ���
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj1 = null;
        }
        else if (breedObj2 == clickedObject)
        {
            // �θ� 2 ���� ���
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj2 = null;
        }
        else if (breedObj1 == null)
        {
            // �θ� 1 ����
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj1 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else if (breedObj2 == null)
        {
            // �θ� 2 ����
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj2 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else
        {
            // �̹� �� �θ� ���õ�
            SoundManager.Instance.PlayEffect("WrongSelect");
            Debug.Log("�̹� �� �θ� ��� ���õ� ����");
        }

        breedButton.SetActive(breedObj1 != null && breedObj2 != null);
    }

    public void ActivateBreed()
    {
        isBreedButtonPressed = true;
    }

    private void DeactivateBreed()
    {
        breedButton.SetActive(false);
        isBreedButtonPressed = false;
    }

    public void SkipBreed()
    {
        isBreedSkipButtonPressed = true;
    }

    private void UpdateBreedCountUI(int count)
    {
        breedCountUI.text = $"<sprite=8> {count}";
    }

    private void SpawnRandomBug()
    {
        int i = Random.Range(0, bugPrefabs.Count);
        Instantiate(bugPrefabs[i]);
        return;
    }

    public bool GetIsBreeding()
    { 
        return isBreeding;
    }

    public bool TryPlacePlant(Plant plant, Vector3 screenPosition)
    {
        int? targetIndex = GetGridIndexFromPosition(screenPosition);

        // ��� ���� ����
        if (!targetIndex.HasValue)
        {
            // ���� ��ġ�� �ǵ�����
            Transform originalSoil = GetSoilTransform(plant.gridIndex);
            plant.transform.position = originalSoil.position;
            return false;
        }

        int fromIndex = plant.gridIndex;
        int toIndex = targetIndex.Value;

        if (plantGrid.ContainsKey(toIndex))
        {
            // ��� ĭ�� �Ĺ��� �ִ� ���: ���� ��ġ ��ȯ
            Plant targetPlant = plantGrid[toIndex];

            // ���� gridIndex �ٲٱ�
            plant.SetGridIndex(toIndex);
            targetPlant.SetGridIndex(fromIndex);

            // ��ġ �ٲٱ�
            Transform fromSoil = GetSoilTransform(fromIndex);
            Transform toSoil = GetSoilTransform(toIndex);
            plant.transform.position = toSoil.position;
            targetPlant.transform.position = fromSoil.position;

            // plantGrid ������Ʈ
            plantGrid[toIndex] = plant;
            plantGrid[fromIndex] = targetPlant;

            return true;
        }
        else
        {
            // �� ĭ�̸� ������� �ɱ�
            plantGrid.Remove(fromIndex); // ���� ��ġ���� ����
            plant.SetGridIndex(toIndex);
            plant.transform.position = GetSoilTransform(toIndex).position;
            plantGrid[toIndex] = plant;

            return true;
        }
    }

    public int? GetGridIndexFromPosition(Vector3 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);
        //Debug.Log(hit.transform.name + hit.transform.position);

        if (hit.collider != null)
        {
            Soil soil = hit.collider.GetComponent<Soil>();
            if (soil != null)
            {
                return soil.GridIndex;
            }
        }
        return null;
    }

    private void Plantplant(Plant plant)
    {
        plantGrid[plant.gridIndex] = plant;

        Transform soilT = GetSoilTransform(plant.gridIndex);
        plant.transform.position = soilT.position;
    }

    public void LoadGrid(List<PlantData> plantList)
    {
        foreach (var item in plantList)
        {
            GameObject obj = Instantiate(peaPrefab);
            Plant plant = obj.GetComponent<Plant>();
            plant.Init(item.gridIndex, this);
            plant.SetTrait(item.traits);

            Plantplant(plant);
        }
    }
}

