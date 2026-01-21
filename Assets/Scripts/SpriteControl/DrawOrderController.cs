using UnityEngine;

public class DrawOrderController : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;
    [SerializeField] protected float drawOrderOffset = 0f;
    [SerializeField] protected float YOffset = 0f;

    [SerializeField] private bool isRandomRotateOn = false;
    [SerializeField] private float randomRotateRange = 10f;

    virtual protected void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if(isRandomRotateOn)
        {
            float rand = Random.Range(-randomRotateRange, randomRotateRange);
            transform.Rotate(0f, 0f, rand);
        }
    }

    protected void Start()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y + YOffset, transform.position.z);
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -100f + drawOrderOffset);
    }
}
