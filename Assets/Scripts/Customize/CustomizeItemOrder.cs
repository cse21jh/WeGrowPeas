using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 치장 아이템이 목록에 보이는 순서. 프로젝트에 하나만 두고(<see cref="ResourcePaths.CustomizeItemOrder"/>),
/// 종류마다 아이템 에셋을 보이는 순서대로 담는다. 아이템 데이터(<see cref="CustomizeItemData"/>)에는 순서를 적지 않는다.
///
/// 순서를 바꾸거나 중간에 끼워 넣어도 이 파일 하나만 바뀐다.
/// 목록에 없는 아이템(새로 만든 것 등)은 그 종류의 맨 뒤에 에셋 이름 순으로 붙는다.
/// 편집은 이 에셋 인스펙터에서 리스트를 끌어다 놓아 한다. 새 아이템을 만들면 그 종류 리스트에 직접 넣어 준다.
/// </summary>
public class CustomizeItemOrder : ScriptableObject
{
    [Serializable]
    public class CategoryList
    {
        public CustomizeCategory category;
        [Tooltip("위가 목록 앞쪽.")]
        public List<CustomizeItemData> items = new List<CustomizeItemData>();
    }

    [SerializeField] private List<CategoryList> categories = new List<CategoryList>();

    /// <summary>그 종류의 순서 목록. 없으면 null. (지워진 아이템이 null로 남아 있을 수 있다)</summary>
    public IReadOnlyList<CustomizeItemData> Get(CustomizeCategory category)
    {
        foreach (var c in categories)
            if (c != null && c.category == category) return c.items;
        return null;
    }

}
