using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

/*public enum CompleteTraitType // 기존 형질
{
    NaturalDeath,
    WindResistance,
    FloodResistance,
    PestResistance,
    ColdResistance,
    HeavyRainResistance,
    None
}*/

public enum IncompleteTraitType // 불완전 형질
{
    None
}



// 형질이나 웨이브 추가 시 GetResistantValue 및 번식 시 Initialize Trait 에서 저항력 계산 추가 필요.

public abstract class Plant : MonoBehaviour
{
    public string speciesname;
    protected List<GeneticTrait> traits = new List<GeneticTrait>();

    public int gridIndex { get; private set; }
    private Grid grid;

    //이동을 위한 변수
    private float holdTime = 0f;
    private bool isHolding = false;
    private bool isDragging = false;
    private const float HoldDuration = 1.5f;

    //옮기기 게이지
    [SerializeField] private Image holdGaugeImage;
    [SerializeField] private GameObject holdGaugeCanvas;

    public virtual void Init(int gridIndex, Grid grid)
    {
        this.gridIndex = gridIndex;
        this.grid = grid;
    }

    public virtual void SetTrait(List<GeneticTrait> newTraits)
    {
        traits = newTraits;
    }

    public virtual List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }
    protected virtual void OnMouseEnter()
    {
        if (ClickRouter.Instance.IsBlockedByUI) return;

        UIPlantStat.Instance.ShowInfo(speciesname, traits);
    }

    protected virtual void OnMouseExit()
    {
        UIPlantStat.Instance.HideInfo();
    }
    
    //식물 이동
    private void OnMouseDown()
    {
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
            grid.RequestBreedSelect(this.gameObject);
        }

            Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        isDragging = false;
        isHolding = false;
        holdTime = 0f;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvas.SetActive(false);
    }
    protected virtual void Update()
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
            FollowMouse();
        }
    }
    private void StartDragging()
    {
        Debug.Log("식물 들기 성공");
        isDragging = true;

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, pos.z - 0.1f);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void FollowMouse()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        transform.position = worldPos;
    }

    public void SetGridIndex(int idx)
    {
        gridIndex = idx;
    }

    public bool CanResist(WaveType wave) // if can't resist, Call Die()
    {
        int randomNumber = UnityEngine.Random.Range(0, 100);
        if (randomNumber <= (int)(GetResistanceValue(wave) * 100))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    protected float GetResistanceValue(WaveType wave)
    {
        CompleteTraitType traitType = CompleteTraitType.None;
        float defaultResistance = 0.0f;
        switch(wave)
        {
            case WaveType.Wind: traitType = CompleteTraitType.WindResistance; break;
            case WaveType.Flood: traitType = CompleteTraitType.FloodResistance; break;
            case WaveType.Pest: traitType = CompleteTraitType.PestResistance; break;
            case WaveType.Cold: traitType = CompleteTraitType.ColdResistance; break;
            case WaveType.HeavyRain: traitType = CompleteTraitType.HeavyRainResistance; break;
            case WaveType.Aging: traitType = CompleteTraitType.NaturalDeath; break;
                // 특성 추가되면 추가
        }

        foreach(GeneticTrait g in traits)
        {
            if(g.traitType == traitType)
                return g.resistance;
        }
        
        return defaultResistance /*+ GameManager.Instance.grid.GetAdditionalResistance(traitType)*/;
    }

    public void UpdateResistance(CompleteTraitType traitType, float value)
    {
        for(int i=0; i < traits.Count; i++)
        {
            if(traits[i].traitType == traitType)
            {
                traits[i] = new GeneticTrait(traitType, traits[i].resistance + value, traits[i].genetics);
            
                return;
            }
        }
        return;
    }

    /*public Vector2Int gridPosition;
    public int gridNumber;

    [SerializeField]
    protected Dictionary<CompleteTraitType, float> completeResistances = new Dictionary<CompleteTraitType, float>(); // 기존 우성 법칙 따르는 형질의 저항력
    
    [SerializeField]
    protected Dictionary<CompleteTraitType, int> completeGenetics= new Dictionary<CompleteTraitType, int>(); // int 값은 우성 개수

    [SerializeField]
    protected Dictionary<IncompleteTraitType, float> incompleteResistances = new Dictionary<IncompleteTraitType, float>(); // 불완전 우성 따르는 형질 저항력

    public virtual void Initialize(int gridNumber, Plant parent1, Plant parent2) // 번식 event 발생 시킬 때 instanciate하고 호출
    {
        InitializeGridPosition(gridNumber);
        InitializeCompleteTrait(parent1.completeGenetics, parent2.completeGenetics);
        InitializeIncompleteTrait(parent1.incompleteResistances, parent2.incompleteResistances);

    }

    protected void InitializeGridPosition(int gridNum)
    {
        // grid번호 따라 포지션 계산해서 grid Position 초기화
    }

    public virtual void InitializeCompleteTrait(Dictionary<CompleteTraitType, int> parent1, Dictionary<CompleteTraitType, int> parent2) // 우성 열성만 여기서 계산. 실제 저항력은 하위 객체에서 상속 받아서 저장
    {
        foreach (CompleteTraitType trait in Enum.GetValues(typeof(CompleteTraitType)))
        {
            if (trait == CompleteTraitType.None)
                break;

            parent1.TryGetValue(trait, out int p1Count);
            parent2.TryGetValue(trait, out int p2Count);

            int allele1;
            if (p1Count == 2) // RR
            {
                allele1 = 1;
            }
            else if (p1Count == 0) // rr
            {
                allele1 = 0;
            }
            else // Rr
            {
                allele1 = UnityEngine.Random.Range(0, 2);
            }

            int allele2;
            if (p2Count == 2) // RR
            {
                allele2 = 1;
            }
            else if (p2Count == 0) // rr
            {
                allele2 = 0;
            }
            else // Rr
            {
                allele2 = UnityEngine.Random.Range(0, 2);
            }

            completeGenetics[trait] = allele1 + allele2;
        }

        // 유전 법칙 따라 저항력 계산은 식물마다 다르니 하위 객체에서 상속받아 계산 
    }

    public virtual void InitializeIncompleteTrait(Dictionary<IncompleteTraitType, float> parent1, Dictionary<IncompleteTraitType, float> parent2)
    {
        // 불완전 유전 법칙 따라 저항력 계산 + 식물 종류 따라 다른 계산 식 필요
    }

    */

    public virtual void Die()
    {
        UIPlantStat.Instance.HideInfo();
        grid.ClearGridIndex(gridIndex);
        Destroy(this.gameObject);
    }

    public virtual void DieWithAnimation()
    {

    }

    public virtual void MakeSelectedSprite()
    {

    }

    public virtual void MakeDefaultSprite()
    {

    }
    void Start()
    {
        
    }
}
