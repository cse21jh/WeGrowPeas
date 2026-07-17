using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 특수 아이템 스타터 에셋(공용 11 + 식물별 6)을 Resources/Data/SpecialItem 아래에 생성.
/// 이미 있는 에셋은 덮어쓰지 않음. 식물별 이름은 임시(임시XXX) — 확정 시 인스펙터에서 교체.
/// </summary>
public static class SpecialItemDataCreator
{
    private const string Dir = "Assets/Resources/Data/SpecialItem";

    // (id, 이름, 설명)
    private static readonly (string id, string name, string desc)[] Common =
    {
        ("enchanter",   "마법부여자", "밤낮이 바뀔 때 발동하는 모든 아이템의 수치가 두 배가 됩니다."),
        ("promotion",   "프로모션",   "저항력이 10% 이하가 되면 해당 저항력이 90%로 변경됩니다."),
        ("double_try",  "이중 시도",  "최초 저항력이 35% 감소하는 대신, 웨이브 생존 시도를 2회 시행합니다."),
        ("world_travel","세계여행",   "낮에 식물을 멀리 이동시킬수록 웨이브를 버틴 후 더 비싸집니다. (한 칸마다 판매 배수 +0.1)"),
        ("circulation", "순환",       "식물을 판매할 때 주변 4칸 식물의 저항력을 5%p 회복시킵니다. (최대 90%)"),
        ("king_return", "왕의 귀환",  "기본 교배 가능 횟수가 2회 감소합니다. 이후 5000골드를 획득할 때마다 교배 가능 횟수가 1회 추가됩니다."),
        ("land_rich",   "땅부자",     "땅문서를 추가로 8회 구매할 수 있습니다. 구매할 때마다 무작위 세로줄에 고속 숙성 효과가 추가되고 가격이 증가합니다."),
        ("vegetarian",  "채식주의자", "완두콩/땅콩이 아닌 식물의 판매 가격이 500골드로 통일되고, 웨이브를 버틸 때마다 가치가 증가합니다."),
        ("gambler",     "도박꾼",     "식물이 저항력 40% 이하인 웨이브를 스스로 버틸 경우 가격의 20%에 해당하는 골드를 지급합니다."),
        ("colorful",    "알록달록",   "땅에 적용 중인 효과가 많을수록 그 위의 식물이 웨이브를 버틴 후 더 비싸집니다. (효과 1개당 판매 배수 +0.1)"),
        ("bottom_deal", "밑장빼기",   "교배 시 25% 확률로 교배 횟수를 소모하지 않습니다."),
    };

    // (id, 임시 이름, 설명, 식물, 해금 새벽 단계)
    private static readonly (string id, string name, string desc, string plant, int dawn)[] PlantSpecific =
    {
        ("pea_special_4",     "임시완두A", "저항력 평균 수치만큼 판매 가격이 추가로 증가합니다. (다른 효과와 곱적용)",                       "완두콩", 4),
        ("pea_special_8",     "임시완두B", "주변 4칸 이내의 서로 다른 식물/장치 1개마다 완두콩의 기본 가격이 50골드 증가합니다.",             "완두콩", 8),
        ("pea_special_12",    "임시완두C", "양성 변종 발생 시 해당 변종의 유전자가 현재 계절에 유리한 쪽으로 변경됩니다.",                   "완두콩", 12),
        ("peanut_special_4",  "임시땅콩A", "새로운 식물이 생겨날 때, 가격의 50%에 해당하는 골드를 획득합니다.",                              "땅콩",   4),
        ("peanut_special_8",  "임시땅콩B", "교배 시 악성 변종만, 자가번식 시 양성 변종만 등장합니다.",                                       "땅콩",   8),
        ("peanut_special_12", "임시땅콩C", "식물이 뿌리를 내리면 모든 저항력이 40%p 증가합니다.",                                            "땅콩",   12),
    };

    [MenuItem("Tools/SpecialItem/Create Starter Data Assets")]
    public static void Create()
    {
        EnsureFolder(Dir);
        int created = 0;

        foreach (var c in Common)
            if (CreateOne(c.id, c.name, c.desc, false, "", 0)) created++;
        foreach (var p in PlantSpecific)
            if (CreateOne(p.id, p.name, p.desc, true, p.plant, p.dawn)) created++;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SpecialItem] 스타터 에셋 {created}개 생성 (아이콘은 인스펙터에서 보완)");
    }

    private static bool CreateOne(string id, string name, string desc, bool plantSpecific, string plant, int dawn)
    {
        string path = $"{Dir}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<SpecialItemData>(path) != null) return false;

        var so = ScriptableObject.CreateInstance<SpecialItemData>();
        so.id = id;
        so.displayName = name;
        so.description = desc;
        so.plantSpecific = plantSpecific;
        so.plantName = plant;
        so.unlockDawnStage = dawn;
        AssetDatabase.CreateAsset(so, path);
        return true;
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
