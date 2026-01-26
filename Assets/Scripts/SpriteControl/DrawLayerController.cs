using UnityEngine;

public class DrawLayerController : MonoBehaviour
{
    [SerializeField] private string layerName = "Default";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SpriteRenderer[] sr = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer s in sr)
        {
            s.sortingLayerName = layerName;
        }
    }
}
