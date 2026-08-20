using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 웨이브 표시 데이터(<see cref="WaveDisplayData"/>) 에셋을 만들고 기본값을 채운다.
/// 이미 있으면 덮어쓰지 않고, 빠진 WaveType 줄만 채워 넣는다.
/// </summary>
public static class WaveDisplayDataCreator
{
    private const string Dir = "Assets/Resources/Data/Wave";
    private const string Path = Dir + "/WaveDisplay.asset";

    // (WaveType, 표시 이름, 색). 색은 엔딩 그래프에서 쓰던 값 기준.
    // 바람만 원래 값(#D6E6EB)이 흰색에 가까워 작은 아이콘에서 묻히길래 조금 진하게 잡았다.
    private static readonly (WaveType type, string name, string hex)[] Defaults =
    {
        (WaveType.Aging,     "자연사", "#FCCF4E"),
        (WaveType.Pest,      "해충",   "#B6B53A"),
        (WaveType.Wind,      "바람",   "#A9C9D6"),
        (WaveType.Flood,     "홍수",   "#469696"),
        (WaveType.HeavyRain, "폭우",   "#746D80"),
        (WaveType.Cold,      "추위",   "#629AB7"),
        (WaveType.Drought,   "가뭄",   "#BE9978"),
        (WaveType.Heat,      "더위",   "#FF6037"),
        (WaveType.None,      "없음",   "#FFFFFF"),
    };

    [MenuItem("Tools/Wave/Create Wave Display Data")]
    public static void Create()
    {
        EnsureFolder(Dir);

        var data = AssetDatabase.LoadAssetAtPath<WaveDisplayData>(Path);
        bool created = false;

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WaveDisplayData>();
            AssetDatabase.CreateAsset(data, Path);
            created = true;
        }

        int added = 0;
        foreach (var (type, name, hex) in Defaults)
        {
            if (data.Find(type) != null) continue; // 이미 있으면 손대지 않는다

            data.entries.Add(new WaveDisplayData.Entry
            {
                type = type,
                displayName = name,
                color = ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white
            });
            added++;
        }

        // WaveType 순서대로 정렬해 인스펙터에서 보기 쉽게
        data.entries.Sort((a, b) => ((int)a.type).CompareTo((int)b.type));

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WavePalette.Reload();

        Debug.Log($"[Wave] 표시 데이터 {(created ? "생성" : "갱신")}: {Path} (줄 {added}개 추가)");
        Selection.activeObject = data;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = System.IO.Path.GetFileName(path);

        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
