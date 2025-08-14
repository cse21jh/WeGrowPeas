using UnityEngine;

public class DrawOrderController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    [SerializeField] private float drawOrderOffset = 0f;
    [SerializeField] private float YOffset = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y + YOffset, transform.position.z);
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -100f + drawOrderOffset);
    }
}
