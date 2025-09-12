using UnityEngine;

public class DrawOrderController : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;
    [SerializeField] protected float drawOrderOffset = 0f;
    [SerializeField] protected float YOffset = 0f;

    virtual protected void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y + YOffset, transform.position.z);
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -100f + drawOrderOffset);
    }
}
