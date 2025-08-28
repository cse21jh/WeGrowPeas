using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Peanut : Plant
{
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] selectedSprite;

    //이동을 위한 변수
    private float holdTime = 0f;
    private bool isHolding = false;
    private bool isDragging = false;
    private const float HoldDuration = 0.7f;

    //옮기기 게이지
    [SerializeField] private Image holdGaugeImage;
    [SerializeField] private GameObject holdGaugeCanvasObj;

    private float peanutCopyProbability = 0.25f;
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "땅콩";
        base.Init(gridIndex, grid);
    }

    public override void SetTrait(List<GeneticTrait> newTraits)
    {
        traits = newTraits;

        foreach (GeneticTrait g in traits)
        {
            additionalResistance.Add(g.traitType, 0f);
        }

        
        StemController stem = GetComponentInChildren<StemController>();
        if (stem != null)
        {
            stem.SetTraits(newTraits, PlantType.Peanut);
        }
        else
        {
            Debug.LogWarning("StemController not found in Plant");
        }
        
    }

    protected void Update()
    {
        if (isHolding)
        {
            holdTime += Time.deltaTime;
            holdGaugeImage.fillAmount = Mathf.Clamp01(holdTime / HoldDuration);

            if (holdTime >= HoldDuration && !isDragging)
            {
                StartDragging();
                holdGaugeCanvasObj.SetActive(false);
            }
        }

        if (isDragging)
        {
            if (!grid.GetIsBreeding())
                grid.TryPlacePlant(this, Input.mousePosition);
            else
                FollowMouse();
        }

        if (!grid.GetIsBreeding())
        {
            isDragging = false;
            isHolding = false;
            holdTime = 0f;
            holdGaugeImage.fillAmount = 0f;
            holdGaugeCanvasObj.SetActive(false);
        }
    }

    public override List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }

    public override void Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        base.Die(cause, killer);
    }

    public override void MakeSelectedSprite()
    {
        base.MakeSelectedSprite();
        /*
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[1];
        */
    }

    public override void MakeDefaultSprite()
    {
        base.MakeDefaultSprite();
        /*
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[0];
        */
    }

    protected void OnMouseEnter()
    {
        //if (ClickRouter.Instance.IsBlockedByUI) return;

        //UIPlantStat.Instance.ShowInfo(speciesname, traits, this);
        FenceUIManager.Instance.SetFenceElements(1, this);
        priceSign.gameObject.SetActive(true);
        priceSign.SetPrice(GetSellingPrice());
    }

    protected void OnMouseExit()
    {
        //UIPlantStat.Instance.HideInfo();
        FenceUIManager.Instance.HideFenceElements();
        priceSign.gameObject.SetActive(false);
    }

    private void OnMouseDown()
    {
        if (!grid.GetIsBreeding())
            return;
        holdTime = 0f;
        isHolding = true;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvasObj.SetActive(true);
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            grid.TryPlacePlant(this, Input.mousePosition);
            FenceUIManager.Instance.SetFenceElements(0, this);
        }
        else
        {
            if (ClickRouter.Instance.IsBlockedByUI) return;
            grid.RequestBreedSelect(this.gameObject);
        }

        isDragging = false;
        isHolding = false;
        holdTime = 0f;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvasObj.SetActive(false);
    }

    private void StartDragging()
    {
        Debug.Log("식물 들기 성공");
        isDragging = true;

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, pos.z - 0.1f);
    }

    private void FollowMouse()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        transform.position = worldPos;
    }

    public override float GetResistanceBasedOnGenetics(int genetics)
    {
        switch (genetics)
        {
            case 0: return 0.4f;
            case 1: return 0.4f;
            case 2: return 0.7f;
        }
        return 0.1f;
    }

    public int FindEmptyGridToCopy()
    {
        List<int> emptyGrid = new List<int>();
        if ((gridIndex - 1) / 4 == gridIndex / 4) // 위칸
        {
            if (!grid.plantGrid.ContainsKey(gridIndex - 1))
                emptyGrid.Add(gridIndex - 1);

        }

        if ((gridIndex + 1) / 4 == gridIndex / 4) // 아래칸
        {
            if (!grid.plantGrid.ContainsKey(gridIndex + 1))
                emptyGrid.Add(gridIndex + 1);

        }

        if ((gridIndex - 4) >= 0) // 왼쪽칸
        {
            if (!grid.plantGrid.ContainsKey(gridIndex - 4))
                emptyGrid.Add(gridIndex - 4);

        }

        if ((gridIndex + 4) < grid.GetMaxCol() * 4) // 오른쪽칸
        {
            if (!grid.plantGrid.ContainsKey(gridIndex + 4))
                emptyGrid.Add(gridIndex + 4);

        }

        if (emptyGrid.Count == 0)
            return -1;

        return emptyGrid[Random.Range(0, emptyGrid.Count)];
    }

    public void TrySpawnCopy()
    {
        if (Random.Range(0, 100) > 100 * (peanutCopyProbability + grid.GetAdditionalPeanutCopyProbability())) // 25프로 확률로 스폰
            return;
        int spawnGridIdx = FindEmptyGridToCopy();

        if (spawnGridIdx == -1) // 스폰할 수 있는 위치가 없음
            return;

        List<GeneticTrait> copyTriats = traits.ToList();
        grid.AddPeanut(copyTriats, spawnGridIdx);
        grid.totalPeanutBreedCount++;
        return;
    }
    public override int GetSellingPrice()
    {
        switch (taste)
        {
            case 0: return 60 + grid.GetAdditionalPeanutGold();
            case 1: return 100 + grid.GetAdditionalPeanutGold();
            case 2: return 130 + grid.GetAdditionalPeanutGold();
            case 3: return 150 + grid.GetAdditionalPeanutGold();
            case 4: return 170 + grid.GetAdditionalPeanutGold();
            case 5: return 200 + grid.GetAdditionalPeanutGold(); 
            case 6: return 240 + grid.GetAdditionalPeanutGold();
        }
        return 0;
    }
}
