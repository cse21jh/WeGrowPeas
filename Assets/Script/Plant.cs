using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CompleteTraitType // 기존 형질
{
    NaturalDeath,
    WindResistance,
    FloodResistance,
    PestResistance,
    ColdResistance,
    HeavyRainResistance,
    None
}

public enum IncompleteTraitType // 불완전 형질
{

}

public enum WaveType
{
    Aging,
    Wind,
    Flood,
    Pest,
    Cold,
    HeavyRain,
}

public abstract class Plant : MonoBehaviour
{
    public Vector2Int gridPosition;
    public int gridNumber;

    [SerializeField]
    protected Dictionary<CompleteTraitType, float> completeResistances = new Dictionary<CompleteTraitType, float>(); // 기존 우성 법칙 따르는 형질의 저항력

    [SerializeField]
    protected Dictionary<IncompleteTraitType, float> IncompleteResistances = new Dictionary<IncompleteTraitType, float>(); // 불완전 우성 따르는 형질 저항력

    public virtual void Initialize(int gridNumber, Plant parent1, Plant parent2) // 번식 event 발생 시킬 때 instanciate하고 호출
    {
        InitializeGridPosition(gridNumber);
        InitializeCompleteTrait(parent1.completeResistances, parent2.completeResistances);
        InitializeIncompleteTrait(parent1.IncompleteResistances, parent2.IncompleteResistances);

    }

    protected void InitializeGridPosition(int gridNum)
    {
        // grid번호 따라 포지션 계산해서 grid Position 초기화
    }

    public virtual void InitializeCompleteTrait(Dictionary<CompleteTraitType, float> parent1, Dictionary<CompleteTraitType, float> parent2)
    {
        // 유전 법칙 따라 저항력 계산 + 식물 종류 따라 다른 계산 식 필요
    }

    public virtual void InitializeIncompleteTrait(Dictionary<IncompleteTraitType, float> parent1, Dictionary<IncompleteTraitType, float> parent2)
    {
        // 불완전 유전 법칙 따라 저항력 계산 + 식물 종류 따라 다른 계산 식 필요
    }

    public void CanResist(WaveType wave)
    {
        // 웨이브 형식 따라 특성 보고 버틸 확률 계산

        int randomNumber = Random.Range(0, 100);
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
        //wave 따른 버틸 확률 반환해주...게 하고 싶은데 enum만 추가해도 알아서 추가되도록 만들고 싶은데...
        return 0;
    }

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
