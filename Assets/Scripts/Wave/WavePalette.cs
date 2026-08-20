using UnityEngine;

/// <summary>
/// 웨이브 표시 이름·색을 꺼내 쓰는 창구. 어느 화면에서든 같은 값이 나오도록 여기 한 곳만 본다.
///
/// 값은 <see cref="WaveDisplayData"/> 에셋에 있고, 에셋이 없거나 항목이 비어 있으면
/// 아래 기본값으로 떨어진다. 그래서 에셋을 아직 만들지 않았어도 화면이 깨지지는 않는다.
/// </summary>
public static class WavePalette
{
    public const string ResourcePath = "Data/Wave/WaveDisplay";

    /// <summary>에셋이 없을 때 쓰는 이름. <see cref="WaveType"/> 순서.</summary>
    private static readonly string[] FallbackNames =
    {
        "자연사", "해충", "바람", "홍수", "폭우", "추위", "가뭄", "더위", "없음"
    };

    /// <summary>에셋이 없을 때 쓰는 색. 엔딩 그래프에서 쓰던 값이 기준이다.</summary>
    private static readonly string[] FallbackColorHex =
    {
        "#FCCF4E", // 자연사
        "#B6B53A", // 해충
        "#A9C9D6", // 바람
        "#469696", // 홍수
        "#746D80", // 폭우
        "#629AB7", // 추위
        "#BE9978", // 가뭄
        "#FF6037", // 더위
        "#FFFFFF", // 없음
    };

    private static WaveDisplayData _data;
    private static bool _searched;

    private static WaveDisplayData Data
    {
        get
        {
            if (_data == null && !_searched)
            {
                _searched = true; // 없으면 매 호출마다 Resources를 뒤지지 않도록
                _data = Resources.Load<WaveDisplayData>(ResourcePath);

                if (_data == null)
                    Debug.LogWarning($"[Wave] {ResourcePath} 에셋이 없어 기본 색·이름을 씁니다. " +
                                     "Tools/Wave/Create Wave Display Data 로 만들 수 있습니다.");
            }

            return _data;
        }
    }

    public static string GetName(WaveType type)
    {
        var entry = Data != null ? Data.Find(type) : null;
        if (entry != null && !string.IsNullOrEmpty(entry.displayName)) return entry.displayName;

        int i = (int)type;
        return (i >= 0 && i < FallbackNames.Length) ? FallbackNames[i] : type.ToString();
    }

    public static Color GetColor(WaveType type)
    {
        var entry = Data != null ? Data.Find(type) : null;
        if (entry != null) return entry.color;

        int i = (int)type;
        string hex = (i >= 0 && i < FallbackColorHex.Length) ? FallbackColorHex[i] : "#FFFFFF";
        return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
    }

    /// <summary>에셋을 다시 읽는다. (에디터에서 에셋을 새로 만든 직후 등)</summary>
    public static void Reload()
    {
        _data = null;
        _searched = false;
    }
}
