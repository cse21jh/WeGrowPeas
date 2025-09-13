using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Narration : MonoBehaviour
{
    private RectTransform narrationBoxContent;

    [SerializeField] private GameObject textBoxPrefab;
    private int maxVisible = 3;

    private readonly Queue<GameObject> spawnedText = new Queue<GameObject>();

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
        //Debug.Log("AddLine");

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

    public void Flush()
    {
        for(int i = narrationBoxContent.childCount - 1; i >= 0; i--)
        {
            var child = narrationBoxContent.GetChild(i);
            if(child != null) Destroy(child.gameObject);
        }

        spawnedText.Clear();
    }

}
