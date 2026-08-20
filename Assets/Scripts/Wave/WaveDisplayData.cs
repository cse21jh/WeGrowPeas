using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브의 표시 이름과 색을 한곳에 모은 에셋.
///
/// 예전에는 화면마다 8칸짜리 색 배열을 따로 채워야 해서, 한 곳만 빠뜨리면
/// 같은 웨이브가 화면마다 다른 색으로 보였다(실제로 엔딩 그래프와 정보 앱의 색이 서로 달랐다).
/// 이제 이 에셋 하나만 채우면 된다.
///
/// 아이콘은 여기 두지 않는다. 위젯·팝업·앱마다 규격이 달라 한 벌로 묶기 어렵다.
/// 아이콘은 각 화면의 인스펙터에서 따로 지정한다.
///
/// 읽을 때는 <see cref="WavePalette"/>를 쓴다. 에셋은
/// <c>Resources/Data/Wave/WaveDisplay.asset</c>에 하나만 둔다.
/// (Tools/Wave/Create Wave Display Data 로 생성)
/// </summary>
[CreateAssetMenu(fileName = "WaveDisplay", menuName = "Data/Wave Display Data")]
public class WaveDisplayData : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public WaveType type;

        [Tooltip("화면에 찍히는 이름 (예: 자연사)")]
        public string displayName;

        [Tooltip("그래프·비료 등 이 웨이브를 색으로 나타낼 때 쓰는 색")]
        public Color color = Color.white;
    }

    [Tooltip("WaveType마다 한 줄. None까지 포함해 9줄.")]
    public List<Entry> entries = new List<Entry>();

    public Entry Find(WaveType type)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].type == type)
                return entries[i];

        return null;
    }
}
