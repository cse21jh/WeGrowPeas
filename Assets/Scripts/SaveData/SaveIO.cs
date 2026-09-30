using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 세이브 파일 입출력 공통 창구. 런 세이브·프로필·도감·회상이 모두 이걸 거친다.
///
/// 쓰기는 "임시 파일에 다 쓴 뒤 교체"로 한다. 쓰는 도중 게임이 꺼져도
/// 원본은 멀쩡히 남고, 교체 직전 원본은 .bak으로 한 벌 보관한다.
/// 읽기는 원본이 깨졌으면 .bak으로 한 번 더 시도한다.
/// </summary>
public static class SaveIO
{
    private const string TempSuffix = ".tmp";
    private const string BackupSuffix = ".bak";

    public static bool Exists(string path) => !string.IsNullOrEmpty(path) && File.Exists(path);

    /// <summary>JSON 파일을 읽는다. 없거나 원본·백업 모두 읽을 수 없으면 false.</summary>
    public static bool TryRead<T>(string path, out T data) where T : class
    {
        data = null;
        if (string.IsNullOrEmpty(path)) return false;

        if (TryParse(path, out data)) return true;

        string backup = path + BackupSuffix;
        if (TryParse(backup, out data))
        {
            Debug.LogWarning($"[SaveIO] 원본을 읽지 못해 백업에서 복구했습니다: {path}");
            return true;
        }
        return false;
    }

    /// <summary>JSON으로 안전하게 쓴다. 실패하면 false (기존 파일은 그대로 남는다).</summary>
    public static bool Write<T>(string path, T data, bool pretty = true)
    {
        if (string.IsNullOrEmpty(path) || data == null) return false;

        try
        {
            return WriteText(path, JsonUtility.ToJson(data, pretty));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveIO] 저장 실패: {path}\n{e}");
            return false;
        }
    }

    /// <summary>문자열을 안전하게 쓴다. 실패하면 false.</summary>
    public static bool WriteText(string path, string text)
    {
        string temp = path + TempSuffix;
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(temp, text);

            if (File.Exists(path))
                File.Replace(temp, path, path + BackupSuffix);
            else
                File.Move(temp, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveIO] 저장 실패: {path}\n{e}");
            try { if (File.Exists(temp)) File.Delete(temp); } catch { /* 정리 실패는 무시 */ }
            return false;
        }
    }

    /// <summary>파일과 그 임시·백업 파일까지 지운다.</summary>
    public static void Delete(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        TryDeleteFile(path);
        TryDeleteFile(path + TempSuffix);
        TryDeleteFile(path + BackupSuffix);
    }

    private static bool TryParse<T>(string path, out T data) where T : class
    {
        data = null;
        try
        {
            if (!File.Exists(path)) return false;
            data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveIO] 읽기 실패: {path} ({e.Message})");
            data = null;
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception e) { Debug.LogWarning($"[SaveIO] 삭제 실패: {path} ({e.Message})"); }
    }
}
