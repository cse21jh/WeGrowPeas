using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 저주 .asset들의 <see cref="CurseScriptable.levels"/>(1/2/3단계 수치)를 Notion 표 값으로 일괄 주입.
/// 각 valueA/valueB/days의 의미는 저주별로 다르며, 해당 Curse 인스턴스 클래스 주석과 일치.
/// </summary>
public static class CurseConfigCreator
{
    [MenuItem("Tools/Curse/Populate Curse Levels")]
    public static void Populate()
    {
        var guids = AssetDatabase.FindAssets("t:CurseScriptable");
        int filled = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<CurseScriptable>(path);
            if (so == null) continue;

            var levels = LevelsFor(so.curseId);
            if (levels == null)
            {
                Debug.LogWarning($"[Curse] 알 수 없는 curseId={so.curseId} ({so.name}) — 건너뜀");
                continue;
            }

            so.levels = levels;
            EditorUtility.SetDirty(so);
            filled++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Curse] {filled}개 저주에 레벨 수치 주입 완료");
    }

    private static CurseLevel L(float a, float b, int days, string note)
        => new CurseLevel { valueA = a, valueB = b, days = days, note = note };

    private static List<CurseLevel> Lvls(CurseLevel l1, CurseLevel l2, CurseLevel l3)
        => new List<CurseLevel> { l1, l2, l3 };

    private static List<CurseLevel> LevelsFor(string id)
    {
        switch (id)
        {
            // ───── 지속형(seasonal) 201~208 ─────
            case "201": // 벌레 대발생: valueA = 등장 딜레이 감소(초). 벌레 2마리는 코드 고정.
                return Lvls(L(1, 0, 0, "2마리, 딜레이 -1초"), L(2, 0, 0, "2마리, 딜레이 -2초"), L(3, 0, 0, "2마리, 딜레이 -3초"));
            case "202": // 돌연변이: valueA = 교배 변종 확률 +%p
                return Lvls(L(5, 0, 0, "변종 +5%p"), L(10, 0, 0, "변종 +10%p"), L(15, 0, 0, "변종 +15%p"));
            case "203": // 방사능: valueA = 매일 모든 저항력 추가 감소 %p
                return Lvls(L(2, 0, 0, "-2%p/일"), L(4, 0, 0, "-4%p/일"), L(6, 0, 0, "-6%p/일"));
            case "204": // 꽃가루 실종: valueA = 매 턴 교배 불가로 바뀌는 비율(%)
                return Lvls(L(20, 0, 0, "20% 교배불가"), L(30, 0, 0, "30% 교배불가"), L(40, 0, 0, "40% 교배불가"));
            case "205": // 독점시장: valueA = 최소 가격 배율(%), valueB = 최대 가격 배율(%)
                return Lvls(L(80, 120, 0, "80~120%"), L(70, 130, 0, "70~130%"), L(60, 140, 0, "60~140%"));
            case "206": // 불면증: valueA = 자유시간 비율(%)
                return Lvls(L(80, 0, 0, "자유시간 80%"), L(60, 0, 0, "자유시간 60%"), L(40, 0, 0, "자유시간 40%"));
            case "207": // 씨 없는 수박: valueA = 교배 실패 확률(%)
                return Lvls(L(5, 0, 0, "실패 5%"), L(10, 0, 0, "실패 10%"), L(15, 0, 0, "실패 15%"));
            case "208": // 집중포화: valueA = 단일 웨이브 저항 추가 감소 %p, days = 유지 턴 수
                return Lvls(L(3, 0, 5, "-3%p, 5턴"), L(6, 0, 5, "-6%p, 5턴"), L(9, 0, 5, "-9%p, 5턴"));

            // ───── 단발형(temporal) 101~109 ─────
            case "101": // 반란: valueA = 우성 +%p / 열성 -%p
                return Lvls(L(10, 0, 0, "±10%p"), L(20, 0, 0, "±20%p"), L(30, 0, 0, "±30%p"));
            case "102": // 안개: valueA = 안개 생기는 땅 개수(총 32칸)
                return Lvls(L(12, 0, 0, "12칸"), L(16, 0, 0, "16칸"), L(20, 0, 0, "20칸"));
            case "103": // 도둑이야!: valueA = 뽑아가는 식물 수
                return Lvls(L(1, 0, 0, "1개"), L(2, 0, 0, "2개"), L(3, 0, 0, "3개"));
            case "104": // 기상이변: days = 웨이브 유형 확인 불가 일수
                return Lvls(L(0, 0, 1, "1일"), L(0, 0, 2, "2일"), L(0, 0, 3, "3일"));
            case "105": // 버섯 발생: valueA = 버섯 생기는 땅 개수(총 32칸)
                return Lvls(L(4, 0, 0, "4칸"), L(6, 0, 0, "6칸"), L(8, 0, 0, "8칸"));
            case "106": // 광란: valueA = 랜덤 교배 확률(%)
                return Lvls(L(10, 0, 0, "10%"), L(20, 0, 0, "20%"), L(30, 0, 0, "30%"));
            case "107": // 대격변: valueA = 위치 변경되는 이동가능 식물 비율(%)
                return Lvls(L(40, 0, 0, "40%"), L(60, 0, 0, "60%"), L(80, 0, 0, "80%"));
            case "108": // 이중 웨이브: days = 두 웨이브 동시 발생 일수
                return Lvls(L(0, 0, 1, "1일"), L(0, 0, 2, "2일"), L(0, 0, 3, "3일"));
            case "109": // 통신장애: valueA = 폰 확인 불가 시간(낮 시간의 %)
                return Lvls(L(20, 0, 0, "낮 20%"), L(40, 0, 0, "낮 40%"), L(60, 0, 0, "낮 60%"));

            default: return null;
        }
    }
}
