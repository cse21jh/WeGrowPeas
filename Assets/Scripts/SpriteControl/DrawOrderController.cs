using UnityEngine;

public class DrawOrderController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    [SerializeField] private float drawOrderOffset = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -100f + drawOrderOffset);
    }
}
