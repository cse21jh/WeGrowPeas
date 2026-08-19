using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 기획 문서(노션 "상점 품목")의 태그를 아이템 에셋에 적용한다.
///
/// 아래 표가 문서의 사본이므로, 문서에서 태그가 바뀌면 표만 고치고 메뉴를 다시 실행하면 된다.
/// 표에 없는 아이템은 건드리지 않고, 이름이 안 맞는 항목은 로그로 알려 준다.
/// (<see cref="CodexDataCreator"/>와 같은 방식)
/// </summary>
public static class ItemTagApplier
{
    // 아이템 이름(ItemData.DisplayName) → 태그. 태그가 없는 아이템은 빈 배열.
    private static readonly Dictionary<string, ItemTag[]> Table = new Dictionary<string, ItemTag[]>
    {
        // ── 고정 물품 ─────────────────────────────────────────────────────
        ["교배 키트"] = new ItemTag[0],
        ["땅문서"] = new ItemTag[0],
        ["식물 모종"] = new[] { ItemTag.Install },
        ["황금 시계"] = new ItemTag[0],

        // ── 완두콩 전용 ───────────────────────────────────────────────────
        ["고품질 식물"] = new[] { ItemTag.Install },
        ["고속 숙성"] = new[] { ItemTag.Profit },
        ["완두커피"] = new[] { ItemTag.Profit },
        ["슈퍼 변종"] = new ItemTag[0],

        // ── 땅콩 전용 ─────────────────────────────────────────────────────
        ["활성형 껍질"] = new ItemTag[0],
        ["왕위 계승"] = new[] { ItemTag.Profit },
        ["땅과 콩"] = new[] { ItemTag.Profit },

        // ── 빌드 상관없이 도움되는 물품 ───────────────────────────────────
        ["페트병"] = new[] { ItemTag.Select, ItemTag.Trigger },
        ["벌레 방해용 선풍기"] = new ItemTag[0],
        ["벌레 스프레이"] = new[] { ItemTag.Timed },
        ["유전자 추출기"] = new[] { ItemTag.Select },
        ["타임 머신"] = new[] { ItemTag.Trigger },
        ["돈나무"] = new[] { ItemTag.Install, ItemTag.Profit },
        ["신용카드"] = new ItemTag[0],
        ["상점 연락처"] = new ItemTag[0],
        ["급속 냉각기"] = new[] { ItemTag.Select },
        ["확률 증가 이벤트"] = new[] { ItemTag.Timed },
        ["쌍둥이"] = new ItemTag[0],
        ["냉각 방패"] = new[] { ItemTag.Trigger },
        ["아드레날린"] = new[] { ItemTag.Timed },

        // ── 골드 관련 ─────────────────────────────────────────────────────
        ["풍미 증진"] = new[] { ItemTag.Profit },
        ["벌레 가공 기계"] = new[] { ItemTag.Profit },
        ["벌레 유도장치"] = new ItemTag[0],
        ["스프링클러"] = new[] { ItemTag.Install, ItemTag.Profit },
        ["스프링클러 용액 개조"] = new[] { ItemTag.Upgrade },
        ["스프링클러 성능 향상"] = new[] { ItemTag.Upgrade },
        ["시간은 금이다"] = new[] { ItemTag.Profit },

        // ── 벌레 대응 ─────────────────────────────────────────────────────
        ["무당벌레 유도장치"] = new ItemTag[0],
        ["무당벌레 채집통"] = new[] { ItemTag.Upgrade },
        ["황금 무당벌레"] = new[] { ItemTag.Upgrade, ItemTag.Profit },
        ["건강한 무당벌레"] = new[] { ItemTag.Upgrade },
        ["네펜데스"] = new[] { ItemTag.Install },
        ["네펜데스 소화액 개량"] = new[] { ItemTag.Upgrade, ItemTag.Profit },
        ["네펜데스 페로몬 강화"] = new[] { ItemTag.Upgrade },
        ["네펜데스 페로몬 생성"] = new[] { ItemTag.Upgrade },

        // ── 메인 생존 ─────────────────────────────────────────────────────
        ["약자생존"] = new ItemTag[0],
        ["불량식품"] = new[] { ItemTag.Profit },
        ["강자생존"] = new ItemTag[0],
        ["황금 유전자"] = new ItemTag[0],

        // ── 서브 생존 ─────────────────────────────────────────────────────
        ["전용 비료"] = new[] { ItemTag.Install },
        ["팻말"] = new[] { ItemTag.Timed },
        ["페트병 납품량 증가"] = new[] { ItemTag.Upgrade },
        ["페트병 원가 감소"] = new[] { ItemTag.Upgrade },
        ["페트병 재질 강화"] = new[] { ItemTag.Upgrade },
        ["고추"] = new[] { ItemTag.Install },
        ["매운 고추"] = new[] { ItemTag.Upgrade },
        ["치료형 캡사이신"] = new[] { ItemTag.Upgrade },

        // ── 임시 ──────────────────────────────────────────────────────────
        ["저항력 흡수 비료"] = new[] { ItemTag.Install },
        ["황금 비료"] = new[] { ItemTag.Install },
    };

    [MenuItem("Tools/Shop/Apply Item Tags")]
    public static void Apply()
    {
        var items = Resources.LoadAll<ItemData>("Data/Item Data");
        var matched = new HashSet<string>();

        int changed = 0;
        var unmatchedAssets = new List<string>();

        foreach (var item in items)
        {
            if (item == null) continue;

            string key = string.IsNullOrEmpty(item.DisplayName) ? item.name : item.DisplayName;

            if (!Table.TryGetValue(key, out var tags))
            {
                unmatchedAssets.Add($"{item.name} (\"{key}\")");
                continue;
            }

            matched.Add(key);

            var next = tags.ToList();
            if (item.Tags != null && item.Tags.SequenceEqual(next)) continue;

            Undo.RecordObject(item, "Apply Item Tags");
            item.Tags = next;
            EditorUtility.SetDirty(item);
            changed++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[Shop] 아이템 태그 적용: {changed}개 변경 / 에셋 {items.Length}개 중 {matched.Count}개 매칭");

        if (unmatchedAssets.Count > 0)
            Debug.LogWarning("[Shop] 표에 없는 아이템 에셋 (태그 미적용):\n  " + string.Join("\n  ", unmatchedAssets));

        var missingInProject = Table.Keys.Where(k => !matched.Contains(k)).ToList();
        if (missingInProject.Count > 0)
            Debug.LogWarning("[Shop] 표에는 있는데 에셋이 없는 아이템:\n  " + string.Join("\n  ", missingInProject));
    }
}
