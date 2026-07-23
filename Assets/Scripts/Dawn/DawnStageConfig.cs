using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 새벽 모드(승천) 단계별 데이터. 단계가 올라갈수록 제약이 "누적"된다.
/// 수치/텍스트는 인스펙터에서 수정 가능(SO). 저주는 별도(여기선 curseLevel만 참고값).
/// 런타임 접근은 <see cref="DawnSystem"/>을 통해서 한다.
/// </summary>
[CreateAssetMenu(menuName = "Dawn/Dawn Stage Config", fileName = "DawnStageConfig")]
public class DawnStageConfig : ScriptableObject
{
    public List<DawnStageData> stages = new List<DawnStageData>();

    public DawnStageData Get(int stage) => stages.Find(s => s != null && s.stage == stage);
}

[Serializable]
public class DawnStageData
{
    [Tooltip("새벽 단계 번호 (1..N)")]
    public int stage;

    [TextArea]
    [Tooltip("이 단계에서 '추가'되는 제약(누적됨). UI에 표시됨")]
    public string constraintDescription;

    [Tooltip("유전자 배율(절대값). 표 기준 1.5~2.6")]
    public float geneticsMultiplier = 1f;

    [Tooltip("이 단계 클리어 시 해금되는 아이템(표시용, 없으면 빈칸)")]
    public string unlockItemName;

    [Tooltip("이 단계 클리어 시 해금되는 아이템 리스트")]
    public List<ItemData> unlockItems = new List<ItemData>();

    [Tooltip("이 단계 클리어 시 해금되는 특수 아이템 리스트")]
    public List<SpecialItemData> unlockSpecialItems = new List<SpecialItemData>();

    // ── 향후 제약 적용용 수치(지금은 데이터로만 보관, 누적은 DawnSystem에서 합/곱) ──
    [Header("제약 수치 (향후 적용용)")]
    [Tooltip("저주 단계(0=없음). 저주 로직은 추후 구현")]
    public int curseLevel = 0;
    [Tooltip("변종 발생 확률 증가(%p, 가산)")]
    public float mutationChanceAddPercent = 0f;
    [Tooltip("벌레 기본 가격 감소(가산)")]
    public int bugPriceReduction = 0;
    [Tooltip("벌레 등장 딜레이 감소(초, 가산)")]
    public float bugDelayReduction = 0f;
    [Tooltip("상점 가격 배수(곱산, 1=변화없음)")]
    public float shopPriceMultiplier = 1f;
    [Tooltip("매일 저항력 감소(%p, 가산)")]
    public float dailyResistanceDecayAddPercent = 0f;
    [Tooltip("세금 배수(곱산, 1=변화없음)")]
    public float taxMultiplier = 1f;
    [Tooltip("저항력 상한 감소(%p, 가산)")]
    public float resistanceCapReductionPercent = 0f;
    [Tooltip("식물 등장/이동 시 뿌리 확률(%, 가산)")]
    public float rootChancePercent = 0f;
}
