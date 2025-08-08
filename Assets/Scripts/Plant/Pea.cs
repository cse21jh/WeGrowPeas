using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class Pea : Plant
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
    [SerializeField] private GameObject holdGaugeCanvas;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "완두콩";
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
            stem.SetTraits(newTraits);
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
                holdGaugeCanvas.SetActive(false);
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
            holdGaugeCanvas.SetActive(false);
        }
    }

    public override List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }
    /*public override void Initialize(int gridNumber, Plant parent1, Plant parent2)
    {
        base.Initialize(gridNumber, parent1, parent2);
    }

    public override void InitializeCompleteTrait(Dictionary<CompleteTraitType, int> parent1, Dictionary<CompleteTraitType, int> parent2)
    {
        base.InitializeCompleteTrait(parent1, parent2);

        foreach (CompleteTraitType trait in Enum.GetValues(typeof(CompleteTraitType)))
        {
            if (trait == CompleteTraitType.None)
                break;
            if (completeGenetics[trait] == 0)
                completeResistances[trait] = 0.9f;
            else
                completeResistances[trait] = 0.5f;
        }

        // 저항력 계산 및 삽입 필요. 지금은 러프하게 열성 만족하면 0.9 저항력 가지도록
    }

    public override void InitializeIncompleteTrait(Dictionary<IncompleteTraitType, float> parent1, Dictionary<IncompleteTraitType, float> parent2)
    {
        base.InitializeIncompleteTrait(parent1, parent2);
        // 저항력 계산 및 삽입 필요
    }*/

    public override void Die()
    {
        base.Die();
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

    protected override void OnMouseEnter()
    {
        if (ClickRouter.Instance.IsBlockedByUI) return;

        UIPlantStat.Instance.ShowInfo(speciesname, traits);
    }

    protected override void OnMouseExit()
    {
        UIPlantStat.Instance.HideInfo();
    }

    private void OnMouseDown()
    {
        if (!grid.GetIsBreeding())
            return;
        holdTime = 0f;
        isHolding = true;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvas.SetActive(true);
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            grid.TryPlacePlant(this, Input.mousePosition);
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
        holdGaugeCanvas.SetActive(false);
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
            case 0: return 0.5f;
            case 1: return 0.5f;
            case 2: return 0.8f;
        }
        return 0.0f;
    }

    public override void ContactBug(Bug bug)
    {
        Die();
    }
}
