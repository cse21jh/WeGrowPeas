using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회상 기록에 담긴 id를 화면에 띄울 아이콘·이름·설명으로 바꾼다.
///
/// 스냅샷은 용량을 아끼려고 id만 담으므로(<see cref="DaySnapshot"/>) 표시할 때 여기서 되찾는다.
/// Resources 로드는 한 번만 하고 캐시한다. 씬 의존이 없어 시작화면에서도 쓸 수 있다.
/// </summary>
public static class RecallLookup
{
    /// <summary>표시에 필요한 최소 정보. 못 찾으면 <see cref="found"/>가 false고 이름만 채워진다.</summary>
    public struct Entry
    {
        public bool found;
        public Sprite icon;
        public string name;
        public string description;
    }

    private static Dictionary<string, Entry> _plants;
    private static Dictionary<string, Entry> _items;
    private static Dictionary<string, Entry> _curses;
    private static Dictionary<string, Entry> _abilities;
    private static string _language;

    // 조회 결과에 표시 이름이 들어 있어 언어가 바뀌면 다시 만들어야 한다.
    private static void EnsureLanguage()
    {
        if (_language == Loc.Language) return;
        _language = Loc.Language;
        _plants = null;
        _items = null;
        _curses = null;
        _abilities = null;
    }

    public static string WaveName(WaveType type) => WavePalette.GetName(type);

    /// <summary>기록에 저장된 웨이브 값의 표시 이름. 알 수 없는 값은 저장된 그대로 보여 준다.</summary>
    public static string SavedWaveName(string saved)
        => WavePalette.TryParseSaved(saved, out var type) ? WaveName(type) : saved;

    /// <summary>
    /// 식물 종(Plant.speciesname)으로 표시 정보를 찾는다.
    ///
    /// 도감 데이터(<see cref="PlantCodexData"/>)를 먼저 보고, 아이콘이 비어 있으면
    /// <see cref="PlantInfoData"/>의 것을 쓴다. 도감 쪽 아이콘이 아직 안 채워져 있어서다.
    /// </summary>
    public static Entry Plant(string speciesName)
    {
        EnsureLanguage();
        if (string.IsNullOrEmpty(speciesName)) return default;

        if (_plants == null)
        {
            _plants = new Dictionary<string, Entry>();

            foreach (var p in Resources.LoadAll<PlantCodexData>("Data/Codex/Plant"))
            {
                if (p == null || string.IsNullOrEmpty(p.plantId)) continue;
                _plants[p.plantId] = new Entry
                {
                    found = true,
                    icon = p.icon,
                    name = p.LocalizedName,
                    description = p.LocalizedDescription
                };
            }

            // 아이콘 보충. plantName이 speciesname과 같은 값을 쓴다.
            foreach (var info in Resources.LoadAll<PlantInfoData>("Data/AbilityData/PlantAbilityData/Plant"))
            {
                if (info == null || string.IsNullOrEmpty(info.plantName) || info.icon == null) continue;

                if (_plants.TryGetValue(info.plantName, out var existing))
                {
                    if (existing.icon != null) continue; // 도감 아이콘이 있으면 그쪽이 우선

                    existing.icon = info.icon;
                    _plants[info.plantName] = existing;
                }
                else
                {
                    _plants[info.plantName] = new Entry
                    {
                        found = true,
                        icon = info.icon,
                        name = info.LocalizedName,
                        description = info.LocalizedDescription
                    };
                }
            }
        }

        return _plants.TryGetValue(speciesName, out var e) ? e : NotFound(speciesName);
    }

    /// <summary>
    /// 상점 구매 이력의 키(<see cref="ItemData.PurchaseKey"/>)로 아이템 정보를 찾는다.
    /// 옛 기록의 키는 <see cref="RecallStore"/>가 읽을 때 이미 바꿔 둔다.
    /// </summary>
    public static Entry Item(string itemKey)
    {
        EnsureLanguage();
        if (string.IsNullOrEmpty(itemKey)) return default;

        if (_items == null)
        {
            _items = new Dictionary<string, Entry>();
            foreach (var it in Resources.LoadAll<ItemData>("Data/Item Data"))
            {
                if (it == null) continue;

                _items[it.PurchaseKey] = new Entry
                {
                    found = true,
                    icon = it.Icon,
                    name = it.LocalizedName,
                    description = it.LocalizedDescription
                };
            }
        }

        return _items.TryGetValue(itemKey, out var e) ? e : NotFound(itemKey);
    }

    /// <summary>특수 아이템 id로 정보를 찾는다.</summary>
    public static Entry SpecialItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return default;

        var data = SpecialItemSystem.GetData(id);
        if (data == null) return NotFound(id);

        return new Entry
        {
            found = true,
            icon = data.icon,
            name = data.LocalizedName,
            description = data.LocalizedDescription
        };
    }

    /// <summary>저주 id로 정보를 찾는다.</summary>
    public static Entry Curse(string id)
    {
        EnsureLanguage();
        if (string.IsNullOrEmpty(id)) return default;

        if (_curses == null)
        {
            _curses = new Dictionary<string, Entry>();
            foreach (var c in Resources.LoadAll<CurseScriptable>("Data/Codex/Curse"))
            {
                if (c == null || string.IsNullOrEmpty(c.curseId)) continue;
                _curses[c.curseId] = new Entry
                {
                    found = true,
                    icon = c.icon,
                    name = c.LocalizedTitle,
                    description = c.LocalizedDescription
                };
            }
        }

        return _curses.TryGetValue(id, out var e) ? e : NotFound(id);
    }

    /// <summary>특성의 에셋 이름으로 정보를 찾는다. 옛 기록의 값은 <see cref="RecallStore"/>가 읽을 때 바꿔 둔다.</summary>
    public static Entry Ability(string id)
    {
        EnsureLanguage();
        if (string.IsNullOrEmpty(id)) return default;

        if (_abilities == null)
        {
            _abilities = new Dictionary<string, Entry>();

            foreach (var a in Resources.LoadAll<PlantAbilityData>("Data/AbilityData/PlantAbilityData"))
                if (a != null) AddAbility(a.name, a.icon, a.LocalizedName, a.LocalizedDescription);

            foreach (var a in Resources.LoadAll<GeneralAbilityData>("Data/AbilityData/GeneralAbilityData"))
                if (a != null) AddAbility(a.name, a.icon, a.LocalizedName, a.LocalizedDescription);
        }

        return _abilities.TryGetValue(id, out var e) ? e : NotFound(id);
    }

    private static void AddAbility(string id, Sprite icon, string name, string description)
        => _abilities[id] = new Entry { found = true, icon = icon, name = name, description = description };

    /// <summary>데이터를 못 찾아도 이름은 남긴다 — 삭제된 아이템의 옛 기록도 읽히도록.</summary>
    private static Entry NotFound(string id) => new Entry { found = false, name = id, description = "" };
}
