using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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

public enum WaveType
{
    Aging,
    Wind,
    Flood,
    Pest,
    Cold,
    HeavyRain,
    None
}

// 형질이나 웨이브 추가 시 GetResistantValue 및 번식 시 Initialize Trait 에서 저항력 계산 추가 필요.

public abstract class Plant : MonoBehaviour
{
    protected List<GeneticTrait> traits = new List<GeneticTrait>();

    public virtual void Init(List<GeneticTrait> newTraits)
    {
        traits = newTraits;
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

    public void CanResist(WaveType wave) // if can't resist, Call Die()
    {
        int randomNumber = UnityEngine.Random.Range(0, 100);
        if(randomNumber <= (int) (GetResistanceValue(wave) * 100))
        {
            return;
        }
        else
        {
            Die();
        }
    }

    protected float GetResistanceValue(WaveType wave)
    {
        float resistance = 0f;
        switch (wave)
        {
            case WaveType.Wind: resistance = completeResistances[CompleteTraitType.WindResistance]; break;
            case WaveType.Flood: resistance = completeResistances[CompleteTraitType.FloodResistance]; break;
            case WaveType.Pest: resistance = completeResistances[CompleteTraitType.PestResistance]; break;
            case WaveType.Cold: resistance = completeResistances[CompleteTraitType.ColdResistance]; break;
            case WaveType.HeavyRain: resistance = completeResistances[CompleteTraitType.HeavyRainResistance]; break;
            case WaveType.Aging: resistance = completeResistances[CompleteTraitType.NaturalDeath]; break;
            // 특성 추가되면 추가
        }
        return resistance;

    }*/

    public void Die()
    {
        // 웨이브를 버티지 못하거나, 칸에 있는 애 지울 때?
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
