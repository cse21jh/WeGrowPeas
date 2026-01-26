using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Intro/Cutscene Data", fileName = "IntroCutsceneData")]
public class IntroCutsceneData : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("위쪽에 표시할 이미지 (선택 사항)")]
        public Sprite image;

        [TextArea(3, 8)]
        [Tooltip("아래쪽 타이핑 효과로 나올 대사")]
        public string text;
    }

    public List<Entry> entries = new List<Entry>();

    public int Count => entries?.Count ?? 0;
    public Entry Get(int index) => entries != null && index >= 0 && index < entries.Count ? entries[index] : null;
}
