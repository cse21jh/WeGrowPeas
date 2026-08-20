using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로딩창 TMI 문구 접근자.
/// 우선순위: Resources의 <see cref="TmiConfig"/> 에셋 → (없으면) 코드 내장 기본값.
/// (WaveSchedule·TaxSchedule과 동일한 구조)
/// </summary>
public static class TmiPool
{
    public const string ResourcePath = "Data/TmiConfig";

    // 에셋이 없을 때 쓰는 폴백 겸, 에디터가 에셋을 만들 때의 시드.
    public static readonly string[] DefaultTips =
    {
        "완두콩은 유전자가 좋을수록 비싸게 팔립니다.",
        "웨이브를 버틸 때마다 식물의 가격이 올라갑니다.",
        "저항력은 매일 조금씩 떨어지니 방치하면 위험합니다.",
        "벌레는 클릭해서 잡을 수 있고, 잡으면 골드를 줍니다.",
        "네펜데스는 벌레를 잡아먹지만 팔 수는 없습니다.",
        "고추 주변의 우성 형질 식물은 저항력이 올라갑니다.",
        "페트병은 식물의 죽음을 한 번 막아줍니다.",
        "세금은 5일마다 내야 하며, 밀리면 압류가 시작됩니다.",
        "국세청 앱에서 미리 세금을 낼 수 있습니다.",
        "비료를 뿌린 줄은 해당 웨이브 저항력이 떨어지지 않습니다.",
        "땅콩은 가끔 스스로 번식합니다.",
        "교배로 태어난 식물은 아주 낮은 확률로 변종이 됩니다.",
        "황금 흙에 심은 식물은 모든 저항력이 90%가 됩니다.",
        "상점은 매일 새로운 물건으로 바뀝니다.",
        "10일마다 특수 아이템 선물이 도착합니다.",
        "새벽 모드는 40일을 클리어하면 열립니다.",
    };

    private static bool _loaded;
    private static List<string> _tips;
    private static int _lastIndex = -1;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var cfg = Resources.Load<TmiConfig>(ResourcePath);
        if (cfg != null && cfg.tips != null && cfg.tips.Count > 0)
        {
            _tips = new List<string>(cfg.tips);
        }
        else
        {
            _tips = new List<string>(DefaultTips);
            if (cfg == null)
                Debug.LogWarning($"[Tmi] Resources/{ResourcePath} 에셋이 없어 기본 문구를 사용합니다. " +
                                 "TmiConfig 에셋을 만들어 그 경로에 두세요.");
        }
    }

    public static void Reload()
    {
        _loaded = false;
        _lastIndex = -1;
        EnsureLoaded();
    }

    public static int Count { get { EnsureLoaded(); return _tips.Count; } }

    /// <summary>무작위 문구 하나. 직전에 보여준 것과는 겹치지 않게 고른다.</summary>
    public static string GetRandom()
    {
        EnsureLoaded();
        if (_tips.Count == 0) return string.Empty;
        if (_tips.Count == 1) return _tips[0];

        int idx;
        do { idx = Random.Range(0, _tips.Count); }
        while (idx == _lastIndex);

        _lastIndex = idx;
        return _tips[idx];
    }
}
