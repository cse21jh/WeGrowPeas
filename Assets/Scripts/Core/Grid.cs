using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;


public class Grid : MonoBehaviour
{
    protected EnemyController enemyController;
    [SerializeField] protected EconomyManager economyManager;

    List<Plant> plants = new List<Plant>();
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    

    protected bool isBreeding = false;

    protected GameObject breedObj1 = null;
    protected GameObject breedObj2 = null;
    protected bool isBreedButtonPressed = false;

    protected bool isBreedSkipButtonPressed = false;

    protected float breedTimer;

    [SerializeField] protected GameObject peaPrefab;
    [SerializeField] protected GameObject peanutPrefab;
    [SerializeField] protected GameObject nepenthesPrefab;
    [SerializeField] protected GameObject chiliPepperPrefab;
    //[SerializeField] private GameObject soilPrefab;
    [SerializeField] protected GameObject[] disabledSoil; // 4개 이상의 열이 추가될 때 활성화되는 토양들
    [SerializeField] protected List<GameObject> bugPrefabs;

    [SerializeField] protected TimerUI breedTimerUI;
    [SerializeField] protected GameObject breedButton;
    [SerializeField] protected GameObject breedSkipButton;
    [SerializeField] protected TextMeshProUGUI breedCountUI;


    [SerializeField] protected Sprite[] gardenSprites; // 정원 배경 스프라이트들
    [SerializeField] protected SpriteRenderer gardenRenderer; // 정원 배경 스프라이트 렌더러

    [Header("Shop")]
    [SerializeField] protected GameObject shopRoot;
    [SerializeField] protected CanvasGroup shopCanvas;

    protected bool isShopOpen = false;
    protected bool shopCloseRequested = false;


    //저장 필요
    [HideInInspector] public int maxCol = 4;
    public int killBugCount = 0;
    public int totalBreedCount = 0;
    public int totalPeaBreedcount = 0;
    public int totalPeanutBreedCount = 0;

    protected float bugSpawnTimeInterval = 10.0f;
    protected float lastBugSpawnTimeInterval = 0f;

    protected float bugSpeedDecreasement = 0f;
    protected float bugSpawnIntervalIncreasement = 0f;
    protected float ladybugSpawnProbability = 0f;
    protected int additionalBugGold = 0;

    protected float additionalPeanutCopyProbability = 0f;
    protected int additionalPeanutGold = 0;

    protected float additionalPestResistance = 0f;

    protected int additionalInheritance = 0;
    protected float maxBreedTimer = 30.0f;
    protected int maxBreedCount = 4;
    protected int breedCount = 0;

    public int MaxCol => maxCol;
    public float BugSpawnTimeInterval => bugSpawnTimeInterval;
    public float LastBugSpawnTimeInterval => lastBugSpawnTimeInterval;
    public float BugSpeedDecreasement => bugSpeedDecreasement;
    public float BugSpawnIntervalIncreasement => bugSpawnIntervalIncreasement;
    public float LadybugSpawnProbability => ladybugSpawnProbability;
    public int AdditionalBugGold => additionalBugGold;
    public float AdditionalPeanutCopyProbability => additionalPeanutCopyProbability;
    public int AdditionalPeanutGold => additionalPeanutGold;
    public float AdditionalPestResistance => additionalPestResistance;
    public int AdditionalInheritance => additionalInheritance;
    public float MaxBreedTimer => maxBreedTimer;
    public int MaxBreedCount => maxBreedCount;
    public int BreedCount => breedCount; // 스테이지 단위 저장이라 아직 ㄱㅊ

    // Start is called before the first frame update
    protected virtual void Start()
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
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f),
        };
            Debug.Log(basicTrait);
            FenceUIManager.Instance.SetFenceElements(0, pea);
            pea.SetTrait(basicTrait);
            //plants.Add(pea);
            AddPlantToGrid(pea);

        }
    }

    public IEnumerator Breeding()
    {
        //breedTimer 만큼 동안 아래 과정 반복 진행 가능

        //교배할 부모 완두콩 두 개 선택
        isBreeding = true;
        breedObj1 = null;
        breedObj2 = null;

        //int breedCount = 0;

        
        Debug.Log(maxBreedTimer + "초 시작. 최대 교배 횟수는 " + maxBreedCount + "입니다");
        UpdateBreedCountUI(maxBreedCount);
        breedTimer = maxBreedTimer;
        breedTimerUI.StartBreedingTimer();

        breedSkipButton.SetActive(true);
        enemyController.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;


        while (breedTimer > 0 && !isBreedSkipButtonPressed)
        {
            lastBugSpawnTimeInterval += Time.deltaTime;
            breedTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.S))
            {
                SkipBreed();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                enemyController.WaveSkip();
            }

            if (lastBugSpawnTimeInterval > bugSpawnTimeInterval * (1f + bugSpawnIntervalIncreasement))
            {
                List<int> targetIdx = new List<int>(plantGrid.Keys);
                if (targetIdx.Count > 0)
                {
                    SpawnRandomBug();
                    lastBugSpawnTimeInterval = 0f;
                }
            }

            if (isBreedButtonPressed || Input.GetKeyDown(KeyCode.Space))
            {
                if (breedObj1 != null && breedObj2 != null) // 교배 버튼 등으로 추후 수정
                {
                    Plant parent1 = breedObj1.GetComponent<Plant>();
                    Plant parent2 = breedObj2.GetComponent<Plant>();
                    //자식 완두콩 형질 계산 후 Instantiate

                    bool canBreed = false;
                    for (int idx = 0; idx < maxCol * 4; idx++) // 빈 칸이 있는가
                    {
                        if (!plantGrid.ContainsKey(idx))
                        {
                            canBreed = true;
                            break;
                        }
                    }

                    bool isEqualPlant = false;
                    if ((parent1.GetType() == parent2.GetType())) // 추후 아종 교배가 생긴다면 이곳과 교배 로직 수정을...
                    {
                        isEqualPlant = true;
                    }


                    if (canBreed && breedCount < maxBreedCount && isEqualPlant)
                    {
                        GameObject childObj = null;
                        if (parent1.GetType() == typeof(Pea))
                            childObj = Instantiate(peaPrefab);
                        else if (parent1.GetType() == typeof(Peanut))
                            childObj = Instantiate(peanutPrefab);
                        Plant child = childObj.GetComponent<Plant>();
                        if (child != null)
                        {
                            Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait(), child);
                            //plants.Add(child);
                            AddPlantToGrid(child);
                            breedCount++;
                            Debug.Log("자식 생성 성공. 남은 교배 횟수는 " + (maxBreedCount - breedCount) + "입니다");
                            SoundManager.Instance.PlayEffect("Breed");
                            totalBreedCount++;
                            if (child.GetType() == typeof(Pea))
                                totalPeaBreedcount++;
                            else if (child.GetType() == typeof(Peanut))
                                totalPeanutBreedCount++;
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
                            Debug.Log("자식 생성에 오류 발생");
                            Destroy(childObj);
                            isBreedButtonPressed = false;
                        }

                    }
                    else if (breedCount >= maxBreedCount)
                    {
                        Debug.Log("최대 교배 횟수 초과");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }
                    else if (isEqualPlant)
                    {
                        Debug.Log("두 종이 일치하지 않습니다");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }
                    else
                    {
                        Debug.Log("키울 공간이 부족합니다");
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }


                }
                else
                {
                    Debug.Log("아직 두 콩을 모두 선택하지 않았습니다");
                    isBreedButtonPressed = false;
                }
            }
            else
            {
                isBreedButtonPressed = false; // 버그로 인해 Breed 버튼이 활성화된 상태에서 버튼 먼저 누르면 교배가 바로 되던 현상 수정
            }

            yield return null;
        }

        if (breedObj1 != null) breedObj1.GetComponent<Plant>().MakeDefaultSprite();
        if (breedObj2 != null) breedObj2.GetComponent<Plant>().MakeDefaultSprite();

        breedTimerUI.StopTimer();
        breedCount = 0;
        Debug.Log("교배 페이즈 종료");
        breedButton.SetActive(false);
        enemyController.HideWaveSkipButton();
        isBreeding = false;
        breedSkipButton.SetActive(false);
        //GardenGrid 리로드

        yield return null;
    }

    private void Breed(List<GeneticTrait> parent1, List<GeneticTrait> parent2, Plant child)
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
                p1Trait = 0;
                traitNotInParent += 1;
            }

            if (parent2.Any(t => t.traitType == trait))
            {
                p2Trait = parent2.First(t => t.traitType == trait).genetics;
            }
            else
            {
                p2Trait = 0;
                traitNotInParent += 1;
            }

            if (traitNotInParent == 2)
                continue;

            switch (p1Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 0 : 1); break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 0 : 1); break;
                default: break;
            }

            float resistance = child.GetResistanceBasedOnGenetics(childGenetic);

            if (trait == CompleteTraitType.PestResistance)
                resistance += additionalPestResistance;

            childTrait.Add(new GeneticTrait(trait, resistance, childGenetic, 0.0f));
        }
        child.SetTrait(childTrait);
    }

    protected void AddPlantToGrid(Plant plant, int grididx = -1) // 이미 오브젝트로 만들어진 식물 그리드에 추가. grididx에 숫자 삽입 시 해당 위치에 식물 심어줌
    {
        if (grididx != -1)
        {
            if (!plantGrid.ContainsKey(grididx))
            {
                plant.Init(grididx, this);
                Plantplant(plant);

                return;
            }
        }

        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            if (!plantGrid.ContainsKey(idx))
            {
                plant.Init(idx, this);
                Debug.Log($"[Grid.AddPlant] Init called for {plant.name} at idx {idx}", plant);
                Plantplant(plant);

                return;
            }
        }

        Destroy(plant.gameObject);
        return;
    }

    public void AddPea(List<GeneticTrait> trait, int grididx = -1)
    {
        GameObject obj = Instantiate(peaPrefab);
        Pea pea = obj.GetComponent<Pea>();
        pea.SetTrait(trait);
        AddPlantToGrid(pea, grididx);
    }

    public void AddPeanut(List<GeneticTrait> trait, int grididx = -1)
    {
        GameObject obj = Instantiate(peanutPrefab);
        Peanut peanut = obj.GetComponent<Peanut>();
        peanut.SetTrait(trait);
        AddPlantToGrid(peanut, grididx);
    }

    public void AddNepenthes(int idx)
    {
        GameObject obj = Instantiate(nepenthesPrefab);
        Nepenthes nepenthes = obj.GetComponent<Nepenthes>();
        AddPlantToGrid(nepenthes, idx);
    }

    public void AddChiliPepper(int idx)
    {
        GameObject obj = Instantiate(chiliPepperPrefab);
        ChiliPepper chiliPepper = obj.GetComponent<ChiliPepper>();
        AddPlantToGrid(chiliPepper, idx);
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
        if (plantGrid.ContainsKey(gridIndex)) plantGrid.Remove(gridIndex);

        if (CheckGameOver())
        {
            StartCoroutine(GameManager.Instance.GameOver());
        }
    }

    public bool CheckGameOver()
    {
        Plant plant;
        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            plant = null;
            plantGrid.TryGetValue(idx, out plant);
            if (plant == null)
                continue;
            if (plant.GetType() == typeof(Pea) || plant.GetType() == typeof(Peanut))
                return false;
        }
        return true;
    }

    public void AddMaxBreedTimer(int time)
    {
        maxBreedTimer += time;
        return;
    }

    public float GetMaxBreedTimer()
    {
        return maxBreedTimer;
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

    public int GetMaxCol()
    {
        return maxCol;
    }

    public void AddAdditionalResistanceInGrid(CompleteTraitType traitType, float value)
    {
        for (int idx = 0; idx < GetMaxCol() * 4; idx++) // grid에 있는 식물들 저항력 증가
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

        gardenRenderer.sprite = gardenSprites[maxCol - 4]; // 정원 배경 스프라이트 변경

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
    public void UpdateSoil()
    {
        if (maxCol <= 4)
            return;
        for (int i = 5; i <= maxCol; i++)
        {
            gardenRenderer.sprite = gardenSprites[i - 4]; // 정원 배경 스프라이트 변경
            disabledSoil[i - 5].SetActive(true);
        }
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
        if (!isBreeding)        
            return;

        Plant clickedPea = clickedObject.GetComponent<Plant>();
        if (clickedPea == null) return;


        if (breedObj1 == clickedObject)
        {
            // 부모 1 선택 취소
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj1 = null;
        }
        else if (breedObj2 == clickedObject)
        {
            // 부모 2 선택 취소
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj2 = null;
        }
        else if (breedObj1 == null)
        {
            // 부모 1 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj1 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else if (breedObj2 == null)
        {
            // 부모 2 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj2 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else
        {
            // 이미 두 부모 선택됨
            SoundManager.Instance.PlayEffect("WrongSelect");
            Debug.Log("이미 두 부모가 모두 선택된 상태");
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
        breedCountUI.text = $"{count}개";
    }

    private void SpawnRandomBug()
    {
        int i = Random.Range(0, bugPrefabs.Count - 1);
        if (Random.Range(0, 100) < (ladybugSpawnProbability * 100))
            i = bugPrefabs.Count - 1;
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

        // 토양 감지 실패
        if (!targetIndex.HasValue)
        {
            // 원래 위치로 되돌리기
            Transform originalSoil = GetSoilTransform(plant.gridIndex);
            plant.transform.position = originalSoil.position;
            return false;
        }

        int fromIndex = plant.gridIndex;
        int toIndex = targetIndex.Value;

        if (plantGrid.ContainsKey(toIndex))
        {
            // 대상 칸에 식물이 있는 경우: 서로 위치 교환
            Plant targetPlant = plantGrid[toIndex];

            // 서로 gridIndex 바꾸기
            plant.SetGridIndex(toIndex);
            targetPlant.SetGridIndex(fromIndex);

            // 위치 바꾸기
            Transform fromSoil = GetSoilTransform(fromIndex);
            Transform toSoil = GetSoilTransform(toIndex);
            plant.transform.position = toSoil.position;
            targetPlant.transform.position = fromSoil.position;

            // plantGrid 업데이트
            plantGrid[toIndex] = plant;
            plantGrid[fromIndex] = targetPlant;

            return true;
        }
        else
        {
            // 빈 칸이면 원래대로 심기
            plantGrid.Remove(fromIndex); // 원래 위치에서 제거
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

    public void LoadGrid(SaveData saveData)
    {
        List<PlantData> plantList = saveData.plantList;
        foreach (var item in plantList)
        {
            GameObject obj;
            switch (item.speciesname)
            {
                case "완두콩": obj = Instantiate(peaPrefab); break;
                case "땅콩": obj = Instantiate(peanutPrefab); break;
                case "네펜데스": obj = Instantiate(nepenthesPrefab); break;
                case "고추": obj = Instantiate(chiliPepperPrefab); break;
                default: obj = Instantiate(peaPrefab); break;
            }
            
            Plant plant = obj.GetComponent<Plant>();
            plant.Init(item.gridIndex, this);
            plant.SetTrait(item.traits);
            plant.SetAdditionalResistances(item.additionalResistance);
            plant.SetTaste(item.taste);

            Plantplant(plant);
        }
        maxCol = saveData.maxCol;
        killBugCount = saveData.killBugCount;
        totalBreedCount = saveData.totalBreedCount;
        totalPeaBreedcount = saveData.totalPeaBreedcount;
        totalPeanutBreedCount = saveData.totalPeanutBreedCount;

        bugSpawnTimeInterval = saveData.bugSpawnTimeInterval;
        lastBugSpawnTimeInterval = saveData.lastBugSpawnTimeInterval;

        bugSpeedDecreasement = saveData.bugSpeedDecreasement;
        bugSpawnIntervalIncreasement = saveData.bugSpawnIntervalIncreasement;
        ladybugSpawnProbability = saveData.ladybugSpawnProbability;
        additionalBugGold = saveData.additionalBugGold;

        additionalPeanutCopyProbability = saveData.additionalPeanutCopyProbability;
        additionalPeanutGold = saveData.additionalPeanutGold;

        additionalPestResistance = saveData.additionalPestResistance;

        additionalInheritance = saveData.additionalInheritance;
        maxBreedTimer = saveData.maxBreedTimer;
        maxBreedCount = saveData.maxBreedCount;
        UpdateSoil();
    }

    public void AddBugSpeedDcreasement(float value)
    {
        bugSpeedDecreasement += value;
    }

    public float GetBugSpeedDecreasement()
    {
        return bugSpeedDecreasement;
    }

    public void AddBugSpawnIntervalIncreasement(float value)
    {
        bugSpawnIntervalIncreasement += value;
    }

    public void AddLadybugSpawnProbability(float value)
    {
        ladybugSpawnProbability += value;
    }

    public void AddAdditionalBugGold(int value)
    {
        additionalBugGold += value;
    }

    public int GetAdditionalBugGold()
    {
        return additionalBugGold;
    }

    public void AddAdditionalPeanutGold(int value)
    {
        additionalPeanutGold += value;
    }

    public int GetAdditionalPeanutGold()
    {
        return additionalPeanutGold;
    }

    public void AddAdditionalPeanutCopyProbability(float value)
    {
        additionalPeanutCopyProbability += value;
    }

    public float GetAdditionalPeanutCopyProbability()
    {
        return additionalPeanutCopyProbability;
    }
}


