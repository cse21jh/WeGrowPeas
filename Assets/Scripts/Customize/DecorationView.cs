using UnityEngine;

/// <summary>씬에 놓인 장식품 하나. <see cref="DecorationLayer"/>가 만들고 지운다.</summary>
public class DecorationView : MonoBehaviour
{
    public DecorationPlacement Placement { get; private set; }
    public CustomizeItemData Item { get; private set; }
    public SpriteRenderer Renderer { get; private set; }

    /// <summary>차지하는 영역(2D, 그림의 사각형).</summary>
    public Rect Rect => DecorationLayer.RectAt(Item, Placement.Position);

    public void Init(DecorationPlacement placement, CustomizeItemData item, SpriteRenderer sr)
    {
        Placement = placement;
        Item = item;
        Renderer = sr;
    }

    public bool Contains(Vector2 worldPoint) => Item != null && Rect.Contains(worldPoint);
}
