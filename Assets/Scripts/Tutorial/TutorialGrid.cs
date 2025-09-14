using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialGrid : Grid
{
    //public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();

    [SerializeField] private GameObject tutorialBug;

    private bool isTBreeding = false;
    private int TMaxBreedCount = 1;
    private int curBreedCount = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        curBreedCount = TMaxBreedCount;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InitTGrid()
    {
        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 0, 0.0f)
    });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1, 0.0f)
    });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 0, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1, 0.0f)
    });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 0, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 2, 0.0f)
    });
    }

    private void SpawnTPea(List<GeneticTrait> traits)
    {
        var p = Instantiate(peaPrefab);

        var pea = p.GetComponent<Pea>();
        pea.SetTrait(traits);

        AddPlantToGrid(pea);
    }

    public void SpawnTutorialBug()
    {
        StartCoroutine(TutorialBugSpawnRoutine());
    }

    private IEnumerator TutorialBugSpawnRoutine()
    {
        TutorialBug b = Instantiate(tutorialBug).GetComponent<TutorialBug>();

        //isBreeding = true;

        yield return new WaitForSeconds(1.75f);

        b.StopMoving();
    }

    public void StartTutorialBreeding()
    {
        StartCoroutine(TutorialBreeding());
    }

    private IEnumerator TutorialBreeding()
    {
        Debug.Log("음하하하하");
        isBreeding = true;
        isTBreeding = true;
        breedObj1 = null;
        breedObj2 = null;

        //breedSkipButton.SetActive(true);
        enemyController.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;

        while (isTBreeding)
        {
            breedTimer -= Time.deltaTime;

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


                    if (canBreed && breedCount < TMaxBreedCount && isEqualPlant)
                    {
                        GameObject childObj = null;
                        if (parent1.GetType() == typeof(Pea))
                            childObj = Instantiate(peaPrefab);
                        else if (parent1.GetType() == typeof(Peanut))
                            childObj = Instantiate(peanutPrefab);
                        Plant child = childObj.GetComponent<Plant>();
                        if (child != null)
                        {
                            TBreed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait(), child);
                            //plants.Add(child);
                            AddPlantToGrid(child);
                            breedCount++;
                            curBreedCount = TMaxBreedCount - breedCount;
                            Debug.Log("자식 생성 성공. 남은 교배 횟수는 " + (curBreedCount) + "입니다");
                            SoundManager.Instance.PlayEffect("Breed");
                            //totalBreedCount++;
                            /*if (child.GetType() == typeof(Pea))
                                totalPeaBreedcount++;
                            else if (child.GetType() == typeof(Peanut))
                                totalPeanutBreedCount++;*/
                            UpdateBreedCountUI(curBreedCount);
                            Plant p1 = breedObj1.GetComponent<Plant>();
                            Plant p2 = breedObj2.GetComponent<Plant>();
                            p1.MakeDefaultSprite();
                            p2.MakeDefaultSprite();
                            breedObj1 = null;
                            breedObj2 = null;
                            DeactivateBreed();
                            TutorialManager.Instance.OnBreedSucess();
                            //isTBreeding = false;
                        }
                        else
                        {
                            Debug.Log("자식 생성에 오류 발생");
                            Destroy(childObj);
                            isBreedButtonPressed = false;
                        }

                    }
                    else if (breedCount >= TMaxBreedCount)
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

    private void TBreed(List<GeneticTrait> parent1, List<GeneticTrait> parent2, Plant child)
    {
        List<GeneticTrait> childTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.8f, 2, 0.0f),
            new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1, 0.0f)
        };

        child.SetTrait(childTrait);
    }

    public override bool CheckGameOver()
    {
        return false;
    }
    public override void RequestBreedSelect(GameObject clickedObject)
    {
        if (!isBreeding || (curBreedCount < 1))
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
        else if (breedObj1 == null && clickedPea.gridIndex == 0)
        {
            // 부모 1 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj1 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else if (breedObj2 == null && clickedPea.gridIndex == 1)
        {
            // 부모 2 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj2 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else
        {
            // 이미 두 부모 선택됨
            //SoundManager.Instance.PlayEffect("WrongSelect");
            Debug.Log("이미 두 부모가 모두 선택된 상태");
        }

        breedButton.SetActive(breedObj1 != null && breedObj2 != null);
    }
}
