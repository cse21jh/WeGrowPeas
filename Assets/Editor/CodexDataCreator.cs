using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 도감용 BugCodexData / PlantCodexData 스타터 에셋을 Resources/Data/Codex 아래에 생성.
/// (UI는 Resources.LoadAll로 로드. 아이템/저주는 각자의 기존 에셋 사용.)
/// 이미 있는 에셋은 덮어쓰지 않음 — 아이콘/설명은 인스펙터에서 채우면 됨.
/// </summary>
public static class CodexDataCreator
{
    private const string BugDir = "Assets/Resources/Data/Codex/Bug";
    private const string PlantDir = "Assets/Resources/Data/Codex/Plant";

    // (bugId=클래스명, 표시명, 설명)
    private static readonly (string id, string name, string desc)[] Bugs =
    {
        ("DefaultBug",        "일반 벌레",       "가장 기본적인 벌레. 식물을 향해 다가온다."),
        ("StraightMovingBug", "직진 벌레",       "방향을 바꾸지 않고 직진한다."),
        ("RandomMovingBug",   "무작위 벌레",     "예측 불가능하게 이동한다."),
        ("UnstoppableBug",    "돌진 벌레",       "멈추지 않고 밀고 들어온다."),
        ("ReviveBug",         "부활 벌레",       "잡아도 다시 살아난다."),
        ("Ladybug",           "무당벌레",        "이로운 벌레. 잡으면 보상을 준다."),
    };

    // (plantId=speciesname, 표시명, 설명, 특성)
    private static readonly (string id, string name, string desc, string trait)[] Plants =
    {
        ("완두콩",   "완두콩",   "기본 저항력이 강한 편이다.",                       ""),
        ("땅콩",     "땅콩",     "자가번식이 가능하다. 생긴 것에 비해 칼로리가 높다.", ""),
        ("피스타치오", "피스타치오", "아직 구현되지 않은 식물.",                        ""),

        // 상점에서 사서 밭에 심는 것들. 교배로는 나오지 않는다.
        ("고추",     "고추",     "웨이브에 죽지 않는다. 500골드에 팔린다.",
                                 "주변 칸 식물의 우성 형질 저항력을 올려 준다."),
        ("네펜데스", "네펜데스", "웨이브에 죽지 않지만 팔아도 값이 붙지 않는다.",
                                 "페로몬으로 주변의 벌레를 끌어당긴다."),
        ("돈나무",   "돈나무",   "5턴을 버티면 5000골드를 남기고 사라진다.",
                                 "옆에 붙은 식물이 하나도 없으면 그 자리에서 시든다."),
        ("스프링클러", "스프링클러", "식물은 아니지만 밭 한 칸을 차지하는 장치. 웨이브에 망가지지 않는다.",
                                 "주변 칸에 물을 주어 비료 효과를 함께 올린다."),
    };

    [MenuItem("Tools/Codex/Create Starter Data Assets")]
    public static void Create()
    {
        EnsureFolder(BugDir);
        EnsureFolder(PlantDir);

        int created = 0;

        foreach (var b in Bugs)
        {
            string path = $"{BugDir}/{b.id}.asset";
            if (AssetDatabase.LoadAssetAtPath<BugCodexData>(path) != null) continue;
            var so = ScriptableObject.CreateInstance<BugCodexData>();
            so.bugId = b.id;
            so.displayName = b.name;
            so.description = b.desc;
            AssetDatabase.CreateAsset(so, path);
            created++;
        }

        foreach (var p in Plants)
        {
            string path = $"{PlantDir}/{p.id}.asset";
            if (AssetDatabase.LoadAssetAtPath<PlantCodexData>(path) != null) continue;
            var so = ScriptableObject.CreateInstance<PlantCodexData>();
            so.plantId = p.id;
            so.displayName = p.name;
            so.description = p.desc;
            so.traitInfo = p.trait;
            AssetDatabase.CreateAsset(so, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Codex] 스타터 데이터 에셋 {created}개 생성 (아이콘/설명은 인스펙터에서 보완)");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
