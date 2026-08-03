using System;

/// <summary>
/// 밸런스 표(CSV/시트)로 빼서 조정할 필드에 붙인다.
/// 이 어트리뷰트가 붙은 필드만 내보내기·불러오기 대상이 되므로,
/// 스프라이트·프리팹 참조 같은 밸런스와 무관한 필드가 섞이지 않는다.
///
/// 사용:
///   [Balance] public int interval = 5;
///   [Balance("세금 증가율")] public float beyondTableGrowth = 2f;
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class BalanceAttribute : Attribute
{
    /// <summary>표에 표시할 이름(설명). 비우면 필드 이름을 그대로 쓴다.</summary>
    public string Label { get; }

    public BalanceAttribute(string label = null)
    {
        Label = label;
    }
}

/// <summary>
/// 이 타입(과 상속받은 타입들)의 밸런스 표를 어느 카테고리/파일로 묶을지 지정한다.
/// 지정하지 않으면 타입 이름으로 파일이 하나씩 생긴다.
///
/// 예: ItemData에 [BalanceGroup("Shop", "Items")]를 달면
///     서브클래스 53종이 Shop/Items.csv 한 파일로 합쳐진다.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class BalanceGroupAttribute : Attribute
{
    /// <summary>CSV가 들어갈 하위 폴더 (예: "Shop", "Config").</summary>
    public string Category { get; }

    /// <summary>파일 이름(확장자 제외). 비우면 타입 이름을 쓴다.</summary>
    public string FileName { get; }

    public BalanceGroupAttribute(string category, string fileName = null)
    {
        Category = category;
        FileName = fileName;
    }
}

/// <summary>
/// 리스트 형태의 데이터를 "한 항목 = 한 행"으로 펼쳐 표로 만든다.
/// (예: DawnStageConfig.stages → 새벽 단계별 한 줄씩)
/// 리스트 요소 클래스 안의 [Balance] 필드가 열이 된다.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class BalanceRowsAttribute : Attribute
{
    /// <summary>각 행을 구분할 키 필드 이름 (예: "stage"). 불러오기 때 어떤 항목인지 찾는 데 쓴다.</summary>
    public string KeyField { get; }

    public BalanceRowsAttribute(string keyField)
    {
        KeyField = keyField;
    }
}
