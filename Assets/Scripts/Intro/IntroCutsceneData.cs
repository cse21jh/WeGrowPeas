using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Intro/Cutscene Data", fileName = "IntroCutsceneData")]
[LocGroup(LocPrefix)]
public class IntroCutsceneData : ScriptableObject, ILocListSource
{
    private const string LocPrefix = "intro";

    [System.Serializable]
    public class Entry
    {
        [HideInInspector, Tooltip("번역 키에 쓰는 고정 id. 추출 도구가 채운다.")]
        public string locId;

        [Tooltip("위쪽에 표시할 이미지 (선택 사항)")]
        public Sprite image;

        [TextArea(3, 8)]
        [Tooltip("아래쪽 타이핑 효과로 나올 대사")]
        public string text;
    }

    public List<Entry> entries = new List<Entry>();

    public int Count => entries?.Count ?? 0;
    public Entry Get(int index) => entries != null && index >= 0 && index < entries.Count ? entries[index] : null;

    /// <summary>화면에 보여줄 대사.</summary>
    public string GetText(Entry entry) => Loc.Or(LocKey.For(LocPrefix, name, entry.locId), entry.text);

    public IEnumerable<LocListText> GetLocTexts()
        => entries.Where(e => e != null).Select(e => new LocListText(e.locId, e.text));

    public bool EnsureLocIds() => LocKey.EnsureUniqueIds(entries, e => e.locId, (e, id) => e.locId = id);
}
