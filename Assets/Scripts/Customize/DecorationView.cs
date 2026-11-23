using UnityEngine;

/// <summary>씬에 놓인 장식품 하나. <see cref="DecorationLayer"/>가 만들고 지운다(배경 소품은 원래 오브젝트에 붙는다).</summary>
public class DecorationView : MonoBehaviour
{
    public DecorationPlacement Placement { get; private set; }
    public CustomizeItemData Item { get; private set; }
    public SpriteRenderer Renderer { get; private set; }

    /// <summary>
    /// 지금 보이는 위치·크기·기울기·반전. 보통은 저장된 배치(<see cref="Placement"/>)와 같고,
    /// 치장 화면에서 끌어 옮기거나 돌리는 동안에는 손을 뗄 때까지 임시 값이다(<see cref="DecorationLayer.ApplyPose"/>).
    /// </summary>
    public DecorationPose Pose { get; private set; }

    /// <summary>차지하는 영역(2D, 지금 보이는 그림을 감싸는 사각형).</summary>
    public Rect Rect => DecorationLayer.RectAt(Item, Pose);

    public void Init(DecorationPlacement placement, CustomizeItemData item, SpriteRenderer sr)
    {
        Placement = placement;
        Item = item;
        Renderer = sr;
        Pose = placement.Pose;
    }

    public void SetPose(DecorationPose pose) => Pose = pose;

    public bool Contains(Vector2 worldPoint) => Item != null && Rect.Contains(worldPoint);
}
