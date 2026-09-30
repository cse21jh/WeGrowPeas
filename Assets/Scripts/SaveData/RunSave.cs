using UnityEngine;

/// <summary>
/// 한 판(런) 세이브 파일(<see cref="SaveData"/>)의 읽기·쓰기·버전 보정.
/// 파일을 직접 읽지 말고 여기를 거친다.
/// </summary>
public static class RunSave
{
    /// <summary>
    /// 지금 쓰는 저장 형식 버전. 구조가 바뀌면 올리고 <see cref="Migrate"/>에 보정을 추가한다.
    /// 필드가 늘어나는 정도는 JsonUtility가 기본값으로 채우므로 올릴 필요 없다.
    /// </summary>
    public const int CurrentVersion = 1;

    public static bool Exists(string path) => SaveIO.Exists(path);

    /// <summary>세이브를 읽고 현재 버전으로 보정한다. 없거나 읽을 수 없으면 false.</summary>
    public static bool TryLoad(string path, out SaveData data)
    {
        if (!SaveIO.TryRead(path, out data)) return false;

        if (data.version > CurrentVersion)
        {
            Debug.LogError($"[RunSave] 더 새로운 빌드의 세이브입니다 (v{data.version} > v{CurrentVersion}): {path}");
            data = null;
            return false;
        }

        Migrate(data);
        return true;
    }

    public static bool Save(string path, SaveData data)
    {
        data.version = CurrentVersion;
        return SaveIO.Write(path, data);
    }

    public static void Delete(string path) => SaveIO.Delete(path);

    /// <summary>
    /// 이어하기로 불러올 때의 시작 방식. 파일에 새 게임/게임오버 상태가 잘못 기록돼 있어도
    /// 이어하기로 보정한다.
    /// </summary>
    public static GameStartType ResolveContinueStartType(SaveData data)
    {
        GameStartType type = data != null ? data.progress.gst : GameStartType.None;
        if (type == GameStartType.NewGame || type == GameStartType.GameOver || type == GameStartType.None)
            return GameStartType.ContinueGame;
        return type;
    }

    /// <summary>옛 버전 데이터를 현재 형식으로 맞춘다. 버전을 올릴 때마다 단계를 추가한다.</summary>
    private static void Migrate(SaveData data)
    {
        // v0 → v1: 버전 필드만 추가됨. 데이터 보정 없음.
        if (data.version < 1) data.version = 1;
    }
}
