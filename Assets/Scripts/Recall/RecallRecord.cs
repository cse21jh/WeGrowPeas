using System;
using System.Collections.Generic;

/// <summary>
/// 회상 목록 화면에 필요한 최소 정보. 목록을 그릴 때 런 파일 전체를 열지 않아도 되도록
/// <c>recall_index.json</c>에 따로 모아 둔다.
///
/// 엔딩과 게임오버를 구분해 담지 않는다. 런은 어떻게 끝났든 한 건의 기록이고,
/// "얼마나 버텼는가"는 <see cref="day"/> 하나로 드러난다.
/// </summary>
[Serializable]
public class RecallIndexEntry
{
    public string id;

    /// <summary>기록된 시각 (Unix 초, UTC). 목록 정렬 기준.</summary>
    public long savedAtUnix;

    /// <summary>버틴 일수. 결과 화면(GameRecordHolder.maxStageReached)과 같은 값.</summary>
    public int day;

    /// <summary>
    /// 그 런의 엔딩 일차. 화면에서 클리어 여부를 보이고 싶으면 <c>day >= clearDay</c>로 따진다.
    /// 기획이 바뀌어 엔딩 일차가 달라져도 과거 기록이 그 시절 기준으로 남는다.
    /// </summary>
    public int clearDay;

    public string plantName;
    public int dawnStage;
}

/// <summary>회상 목록 파일(<c>recall_index.json</c>)의 내용.</summary>
[Serializable]
public class RecallIndex
{
    public int version = RecallStore.FormatVersion;
    public List<RecallIndexEntry> entries = new();
}

/// <summary>
/// 회상 1건(런 하나)의 전체 기록(<c>run_&lt;id&gt;.json</c>).
/// 결과 화면이 쓰는 요약·그래프와 타임라인 스냅샷을 한 파일에 담는다.
/// </summary>
[Serializable]
public class RecallRunFile
{
    public int version = RecallStore.FormatVersion;

    public RecallIndexEntry header = new();
    public RunSummary summary = new();
    public GraphSave graph = new();
    public RecallSave recall = new();

    // 선택한 특성 (plantAbilityNames[i]의 레벨이 plantAbilityLevels[i] — 인덱스 매칭)
    public List<string> plantAbilityNames = new();
    public List<int> plantAbilityLevels = new();
    public List<string> generalAbilityNames = new();
}
