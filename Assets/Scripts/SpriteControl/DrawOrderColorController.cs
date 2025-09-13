using UnityEngine;

public class DrawOrderColorController : DrawOrderController
{
    [SerializeField] private Color multiplyColor = Color.white;

    [SerializeField] private float maxY = 1f;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float mulY = 0f;
    [SerializeField] private float colorOffset = 1f;

    override protected void Awake()
    {
        base.Awake();

        mulY = colorOffset + (transform.position.y - minY) / (maxY - minY);

        multiplyColor = new Color(mulY * 0.8f, mulY * 0.9f, mulY * 0.5f, 1f);
        spriteRenderer.color = multiplyColor;
    }
}
