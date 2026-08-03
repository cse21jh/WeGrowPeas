using UnityEngine;

/// <summary>
/// 인게임 디버그 패널 전체 on/off 스위치.
/// 여기서 <see cref="Enabled"/>가 false면 F8~F11 패널이 전부 뜨지 않는다.
///
/// - 에디터/개발 빌드에서만 컴파일된다(릴리즈 빌드에는 코드가 포함되지 않음).
/// - 기본값은 에디터에서 켜짐, 개발 빌드에서 꺼짐.
/// - 게임 중에는 F12로 전체 토글할 수 있다.
/// </summary>
public static class DebugPanels
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const string PrefKey = "Debug_PanelsEnabled";

    private static bool _loaded;
    private static bool _enabled;

    /// <summary>디버그 패널을 표시할지 여부. 설정은 PlayerPrefs에 유지된다.</summary>
    public static bool Enabled
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
#if UNITY_EDITOR
                _enabled = PlayerPrefs.GetInt(PrefKey, 1) != 0; // 에디터: 기본 켜짐
#else
                _enabled = PlayerPrefs.GetInt(PrefKey, 0) != 0; // 개발 빌드: 기본 꺼짐
#endif
            }
            return _enabled;
        }
        set
        {
            _loaded = true;
            _enabled = value;
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Debug] 디버그 패널 {(value ? "켜짐" : "꺼짐")} (F12로 토글)");
        }
    }

    public static void Toggle() => Enabled = !Enabled;

    /// <summary>F12 전체 토글. 각 패널이 Update에서 호출한다.</summary>
    public static void HandleToggleKey()
    {
        if (Input.GetKeyDown(KeyCode.F12)) Toggle();
    }
#else
    // 릴리즈 빌드: 항상 꺼짐 (호출부가 컴파일되도록 최소 API만 유지)
    public static bool Enabled => false;
    public static void Toggle() { }
    public static void HandleToggleKey() { }
#endif
}
