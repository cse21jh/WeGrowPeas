using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 회상 기록의 영구 저장소. 런이 끝나면(엔딩/게임오버) 이번 런의 기록을 파일로 확정한다.
///
/// 저장 슬롯과 무관한 전역 컬렉션이다. 슬롯 0에서 남긴 기록은 슬롯 1로 새 게임을 해도 남는다.
/// 파일은 런 세이브와 같은 계열인 게임 폴더 옆(<see cref="RootPath"/>)에 둔다.
/// (에디터에서 Assets 아래에 두면 PNG가 전부 에셋으로 임포트되므로 한 단계 위로 뺐다)
///
///   Recall/recall_index.json   목록용 헤더 모음
///   Recall/run_(id).json       런 1건의 전체 기록
///   Recall/run_(id).png        그 런의 농장 사진
/// </summary>
public static class RecallStore
{
    /// <summary>
    /// 지금 쓰는 저장 형식 버전. 구조가 바뀌면 올리고 <see cref="MigrateRun"/>에 보정을 추가한다.
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// 읽어줄 수 있는 가장 오래된 버전. 필드가 늘어나는 정도의 변경은 JsonUtility가
    /// 기본값으로 채우므로 옛 기록도 그대로 읽힌다 — 버전이 다르다고 버리지 않는다.
    /// 반대로 이보다 새 버전(미래 빌드가 쓴 파일)은 해석할 수 없어 건너뛴다.
    /// </summary>
    public const int MinSupportedVersion = 1;

    /// <summary>보관할 최대 기록 수. 넘으면 오래된 것부터 지운다.</summary>
    public const int MaxEntries = 50;

    private const string IndexFileName = "recall_index.json";

    /// <summary>회상 파일이 모이는 폴더. 경로를 바꾸려면 여기만 고치면 된다.</summary>
    public static string RootPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recall"));

    public static string RunJsonPath(string id) => Path.Combine(RootPath, $"run_{id}.json");
    public static string RunImagePath(string id) => Path.Combine(RootPath, $"run_{id}.png");
    private static string IndexPath => Path.Combine(RootPath, IndexFileName);

    // ── 읽기 ──────────────────────────────────────────────────────────────────

    /// <summary>목록용 헤더를 최신순으로 돌려준다. 없으면 빈 목록.</summary>
    public static List<RecallIndexEntry> GetEntries()
    {
        var index = LoadIndex();

        index.entries.RemoveAll(e => e == null || string.IsNullOrEmpty(e.id));
        SyncWithFiles(index);

        index.entries.Sort((a, b) => b.savedAtUnix.CompareTo(a.savedAtUnix));
        return index.entries;
    }

    /// <summary>런 1건의 전체 기록을 읽는다. 없거나 읽을 수 없는 형식이면 null.</summary>
    public static RecallRunFile LoadRun(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        try
        {
            string path = RunJsonPath(id);
            if (!File.Exists(path)) return null;

            var run = JsonUtility.FromJson<RecallRunFile>(File.ReadAllText(path));
            if (run == null) return null;

            if (run.version > FormatVersion)
            {
                // 더 새 버전으로 저장된 파일. 내용을 신뢰할 수 없으니 손대지 않고 건너뛴다.
                Debug.LogWarning($"[Recall] 더 새로운 형식이라 읽지 않습니다: {id} (v{run.version} > v{FormatVersion})");
                return null;
            }

            if (run.version < MinSupportedVersion)
            {
                Debug.LogWarning($"[Recall] 지원이 끝난 형식입니다: {id} (v{run.version})");
                return null;
            }

            MigrateRun(run);
            return run;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Recall] 기록 로드 실패 ({id}): {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 옛 버전으로 저장된 기록을 지금 형식에 맞게 손본다.
    ///
    /// 필드가 늘기만 하는 변경은 JsonUtility가 기본값으로 채우므로 여기 손댈 필요가 없다.
    /// 기본값이 곧 오답인 경우(의미가 바뀐 필드, 쪼개진 필드 등)만 버전별로 보정한다.
    /// </summary>
    private static void MigrateRun(RecallRunFile run)
    {
        // v1: 최초 형식. 아직 보정할 것이 없다.
        //
        // 형식을 바꿀 때는 이런 식으로 쌓는다:
        //   if (run.version < 2) { ... v1 → v2 보정 ... }
        //   if (run.version < 3) { ... v2 → v3 보정 ... }
        // 파일 자체는 다시 쓰지 않는다. 다음에 읽을 때 또 보정하면 되고,
        // 읽기만으로 사용자 파일을 덮어쓰지 않는 편이 안전하다.

        if (run.header == null) run.header = new RecallIndexEntry();
        if (run.summary == null) run.summary = new RunSummary();
        if (run.graph == null) run.graph = new GraphSave();
        if (run.recall == null) run.recall = new RecallSave();
    }

    /// <summary>사진을 텍스처로 읽는다. 없으면 null.</summary>
    public static Texture2D LoadImage(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        try
        {
            string path = RunImagePath(id);
            if (!File.Exists(path)) return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            return tex.LoadImage(File.ReadAllBytes(path)) ? tex : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Recall] 사진 로드 실패 ({id}): {e.Message}");
            return null;
        }
    }

    // ── 쓰기 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 이번 런을 회상 기록으로 확정한다. 런이 끝나는 시점에 한 번만 부른다.
    /// 세이브 파일이 지워지기 전에 불려야 <see cref="RecallRecorder"/>의 타임라인이 살아 있다.
    /// </summary>
    /// <param name="screenshotPng">농장 사진. null이면 사진 없이 기록만 남는다.</param>
    /// <returns>남긴 기록의 id. 실패하면 null.</returns>
    public static string Commit(byte[] screenshotPng)
    {
        try
        {
            Directory.CreateDirectory(RootPath);

            string id = NewId();
            var run = BuildRunFile(id);

            File.WriteAllText(RunJsonPath(id), JsonUtility.ToJson(run, true));

            if (screenshotPng != null && screenshotPng.Length > 0)
                File.WriteAllBytes(RunImagePath(id), screenshotPng);

            var index = LoadIndex();
            index.entries.RemoveAll(e => e == null || string.IsNullOrEmpty(e.id) || e.id == id);
            index.entries.Add(run.header);
            Prune(index);
            SaveIndex(index);

            Debug.Log($"[Recall] 기록을 남겼습니다: {id} ({run.header.day}일)");
            return id;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Recall] 기록 저장 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>기록 1건을 지운다(json + png + 목록 항목).</summary>
    public static void Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        try
        {
            var index = LoadIndex();
            index.entries.RemoveAll(e => e != null && e.id == id);
            SaveIndex(index);
            DeleteFiles(id);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Recall] 기록 삭제 실패 ({id}): {e.Message}");
        }
    }

    // ── 내부 ──────────────────────────────────────────────────────────────────

    private static RecallRunFile BuildRunFile(string id)
    {
        var run = new RecallRunFile();

        run.header = new RecallIndexEntry
        {
            id = id,
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            day = GameRecordHolder.maxStageReached,
            clearDay = GameManager.Instance != null ? GameManager.Instance.EndStage : 0,
            plantName = GameManager.Instance != null ? GameManager.Instance.currentPlant : string.Empty,
            dawnStage = DawnSystem.SelectedDawnStage
        };

        run.summary = GameRecordHolder.Current;
        PlayerRecordForGraph.SaveTo(run.graph);
        RecallRecorder.SaveTo(run.recall);

        // 선택한 특성. 파일이 단독으로 읽혀야 하므로 에셋 참조 대신 이름/레벨로 남긴다.
        var am = AbilityManager.Instance;
        if (am != null)
        {
            foreach (var ability in am.CurrentPlantAbility)
            {
                if (ability == null) continue;
                run.plantAbilityNames.Add(ability.abilityName);
                run.plantAbilityLevels.Add(ability.level);
            }

            foreach (var ability in am.CurrentGeneralAbility)
            {
                if (ability == null) continue;
                run.generalAbilityNames.Add(ability.abilityName);
            }
        }

        return run;
    }

    /// <summary>같은 초에 두 번 저장돼도 겹치지 않도록 뒤에 번호를 붙인다.</summary>
    private static string NewId()
    {
        string baseId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string id = baseId;

        for (int i = 1; File.Exists(RunJsonPath(id)); i++)
            id = $"{baseId}_{i}";

        return id;
    }

    /// <summary>
    /// 목록 파일을 읽는다. 읽지 못해도 빈 목록을 돌려줄 뿐 기존 기록을 버리지 않는다 —
    /// 진짜 데이터는 run_*.json 쪽이고, 목록은 <see cref="SyncWithFiles"/>가 다시 채운다.
    /// </summary>
    private static RecallIndex LoadIndex()
    {
        try
        {
            if (File.Exists(IndexPath))
            {
                var index = JsonUtility.FromJson<RecallIndex>(File.ReadAllText(IndexPath));

                if (index != null && index.entries != null && index.version <= FormatVersion)
                    return index;

                if (index != null && index.version > FormatVersion)
                    Debug.LogWarning($"[Recall] 목록이 더 새로운 형식입니다 (v{index.version}). 파일에서 다시 만듭니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Recall] 목록 로드 실패: {e.Message}. 파일에서 다시 만듭니다.");
        }

        return new RecallIndex();
    }

    /// <summary>
    /// 목록을 폴더의 실제 파일과 맞춘다.
    ///
    /// 목록 파일은 캐시일 뿐이고 기준은 디스크의 run_*.json이다. 그래서
    ///   - 목록이 깨지거나 지워져도 기록이 사라지지 않고 (파일에서 다시 만든다)
    ///   - 사용자가 파일을 직접 지워도 목록에 유령 항목이 남지 않는다.
    /// </summary>
    private static void SyncWithFiles(RecallIndex index)
    {
        if (!Directory.Exists(RootPath)) return;

        var onDisk = new HashSet<string>();
        foreach (string path in Directory.GetFiles(RootPath, "run_*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.Length > 4) onDisk.Add(name.Substring(4)); // "run_" 떼기
        }

        bool changed = false;

        // 파일이 없어진 항목 정리
        changed |= index.entries.RemoveAll(e => e == null || !onDisk.Contains(e.id)) > 0;

        // 목록에 없는 파일 복구
        var known = new HashSet<string>();
        foreach (var e in index.entries) known.Add(e.id);

        foreach (string id in onDisk)
        {
            if (known.Contains(id)) continue;

            var run = LoadRun(id);
            if (run == null || run.header == null) continue; // 못 읽는 파일은 건드리지 않고 둔다

            run.header.id = id; // 파일명이 기준
            index.entries.Add(run.header);
            changed = true;

            Debug.Log($"[Recall] 목록에 없던 기록을 복구했습니다: {id}");
        }

        if (!changed && index.version == FormatVersion) return;

        try
        {
            SaveIndex(index);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Recall] 목록 갱신 실패: {e.Message}");
        }
    }

    private static void SaveIndex(RecallIndex index)
    {
        index.version = FormatVersion; // 쓰는 순간 지금 형식이 된다
        Directory.CreateDirectory(RootPath);
        File.WriteAllText(IndexPath, JsonUtility.ToJson(index, true));
    }

    /// <summary>상한을 넘으면 오래된 것부터 지운다(엔딩/게임오버 구분 없이 시간순).</summary>
    private static void Prune(RecallIndex index)
    {
        if (index.entries.Count <= MaxEntries) return;

        index.entries.Sort((a, b) => a.savedAtUnix.CompareTo(b.savedAtUnix));

        int removeCount = index.entries.Count - MaxEntries;
        for (int i = 0; i < removeCount; i++)
        {
            DeleteFiles(index.entries[i].id);
            Debug.Log($"[Recall] 오래된 기록을 지웠습니다: {index.entries[i].id}");
        }

        index.entries.RemoveRange(0, removeCount);
    }

    private static void DeleteFiles(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (File.Exists(RunJsonPath(id))) File.Delete(RunJsonPath(id));
        if (File.Exists(RunImagePath(id))) File.Delete(RunImagePath(id));
    }
}
