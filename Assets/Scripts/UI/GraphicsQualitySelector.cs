using UnityEngine;

/// <summary>
/// 설정창의 그래픽 품질 행 ("그래픽  &lt; 보통 &gt;"). 행 모양과 배치는 <see cref="SettingStepperRow"/>가 맡는다.
/// </summary>
public class GraphicsQualitySelector : MonoBehaviour
{
    private SettingStepperRow row;

    /// <summary>
    /// lastRow: 지금 맨 아래에 있는 설정 행. 새 행은 그 바로 밑에 놓인다.
    /// labelReference: 글자 모양을 따를 행(배경음 볼륨 행).
    /// 만든 행을 돌려주며, 만들지 못하면 null.
    /// </summary>
    public static RectTransform AddTo(Transform panel, RectTransform lastRow, RectTransform labelReference)
    {
        var row = SettingStepperRow.AddTo(panel, lastRow, labelReference, nameof(GraphicsQualitySelector),
            () => Loc.T("settings.graphics"), ValueText, Step);
        if (row == null) return null;

        row.gameObject.AddComponent<GraphicsQualitySelector>().row = row;
        return (RectTransform)row.transform;
    }

    private static string ValueText() => GraphicsQuality.Current switch
    {
        GraphicsQualityLevel.Low => Loc.T("settings.graphics.low"),
        GraphicsQualityLevel.Medium => Loc.T("settings.graphics.medium"),
        _ => Loc.T("settings.graphics.high"),
    };

    private static void Step(int direction)
    {
        // 끝에서 다시 처음으로 돌지 않는다. 낮음 왼쪽으로 가면 높음이 되는 건 헷갈린다.
        int next = Mathf.Clamp((int)GraphicsQuality.Current + direction,
            (int)GraphicsQualityLevel.Low, (int)GraphicsQualityLevel.High);
        if (next == (int)GraphicsQuality.Current) return;
        GraphicsQuality.Set((GraphicsQualityLevel)next);
    }

    private void OnEnable() => GraphicsQuality.Changed += Refresh;
    private void OnDisable() => GraphicsQuality.Changed -= Refresh;
    private void Refresh() { if (row != null) row.Refresh(); }
}
