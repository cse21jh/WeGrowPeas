using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>사용자가 바꿀 수 있는 키 동작. 저장 파일에는 이름으로 남으므로 이름을 바꾸지 않는다.</summary>
public enum GameAction
{
    Skip,
    WaveSkip,
    Breed,
    Shovel,
    Scouter,
    Phone
}

/// <summary>
/// 게임 조작 키를 한곳에서 읽는다. 값은 <see cref="DeviceSettings"/>에 저장되어 프로필과 무관하게 유지된다.
/// ESC는 키를 잘못 잡아도 설정창을 열 수 있도록 바꿀 수 없다.
/// </summary>
public static class GameInput
{
    public static readonly GameAction[] Actions = (GameAction[])Enum.GetValues(typeof(GameAction));

    private static readonly Dictionary<GameAction, KeyCode> Defaults = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.Skip, KeyCode.S },
        { GameAction.WaveSkip, KeyCode.D },
        { GameAction.Breed, KeyCode.Space },
        { GameAction.Shovel, KeyCode.A },
        { GameAction.Scouter, KeyCode.F },
        { GameAction.Phone, KeyCode.Tab },
    };

    private static Dictionary<GameAction, KeyCode> _keys;
    private static int _ignoreInputFrame = -1;

    /// <summary>키가 바뀌면 호출된다. 버튼의 키 안내는 여기서 다시 그린다.</summary>
    public static event Action BindingsChanged;

    /// <summary>설정창이 새 키를 기다리는 중이면 게임 입력을 막는다.</summary>
    public static bool IsCapturing { get; private set; }

    private static Dictionary<GameAction, KeyCode> Keys
    {
        get
        {
            if (_keys == null) Load();
            return _keys;
        }
    }

    public static KeyCode GetKey(GameAction action) => Keys[action];

    public static bool GetDown(GameAction action)
    {
        if (IsCapturing || Time.frameCount == _ignoreInputFrame) return false;
        return Input.GetKeyDown(Keys[action]);
    }

    public static void BeginCapture() => IsCapturing = true;

    /// <summary>입력을 받은 그 프레임에 같은 키가 게임 동작으로 이어지지 않게 막는다.</summary>
    public static void EndCapture()
    {
        IsCapturing = false;
        _ignoreInputFrame = Time.frameCount;
    }

    /// <summary>키를 지정한다. 다른 동작이 쓰던 키라면 두 동작의 키를 맞바꾼다.</summary>
    public static void SetKey(GameAction action, KeyCode key)
    {
        if (!IsBindable(key)) return;

        KeyCode previous = Keys[action];
        if (previous == key) return;

        foreach (GameAction other in Actions)
        {
            if (other != action && Keys[other] == key)
                Keys[other] = previous;
        }

        Keys[action] = key;
        Save();
    }

    public static void ResetToDefaults()
    {
        _keys = new Dictionary<GameAction, KeyCode>(Defaults);
        Save();
    }

    /// <summary>키보드 키만 허용한다. ESC는 설정창과 취소에 쓰고, 마우스는 게임 조작에 쓴다.</summary>
    public static bool IsBindable(KeyCode key)
    {
        return key != KeyCode.None && key != KeyCode.Escape && key < KeyCode.Mouse0;
    }

    public static string GetKeyName(GameAction action) => FormatKey(GetKey(action));

    public static string FormatKey(KeyCode key)
    {
        if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return ((int)key - (int)KeyCode.Alpha0).ToString();
        return key.ToString();
    }

    /// <summary>문구 안의 {key_동작} 자리를 현재 키 이름으로 바꾼다.</summary>
    public static string FormatHints(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf("{key_", StringComparison.Ordinal) < 0) return text;

        foreach (GameAction action in Actions)
            text = text.Replace("{key_" + action + "}", GetKeyName(action));
        return text;
    }

    private static void Load()
    {
        _keys = new Dictionary<GameAction, KeyCode>(Defaults);

        var saved = DeviceSettings.Data.keyBindings;
        if (saved == null) return;

        foreach (var entry in saved)
        {
            if (Enum.TryParse(entry.action, out GameAction action) && IsBindable((KeyCode)entry.key))
                _keys[action] = (KeyCode)entry.key;
        }
    }

    private static void Save()
    {
        var saved = new List<KeyBindingEntry>();
        foreach (GameAction action in Actions)
            saved.Add(new KeyBindingEntry { action = action.ToString(), key = (int)_keys[action] });

        DeviceSettings.Data.keyBindings = saved;
        DeviceSettings.Save();
        BindingsChanged?.Invoke();
    }

    // 도메인 리로드를 꺼도 플레이마다 설정을 새로 읽도록 정적 상태를 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _keys = null;
        _ignoreInputFrame = -1;
        IsCapturing = false;
        BindingsChanged = null;
    }
}
