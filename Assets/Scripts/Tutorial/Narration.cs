using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Narration : MonoBehaviour
{
    private RectTransform narrationBoxContent;

    [SerializeField] private GameObject textBoxPrefab;
    [SerializeField] private int maxVisible = 4;

    private readonly Queue<GameObject> spawnedText = new Queue<GameObject>();
    public int _nextIdx = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        narrationBoxContent = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    public void AddLine(string text)
    {
        Debug.Log("AddLine");

        if (textBoxPrefab == null || narrationBoxContent == null) return;

        if (spawnedText.Count >= maxVisible)
        {
            var oldest = spawnedText.Dequeue();
            if (oldest) Destroy(oldest);
        }

        var tb = Instantiate(textBoxPrefab, narrationBoxContent, false);

        var tmp = tb.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) tmp.text = text;

        spawnedText.Enqueue(tb);
    }

}
