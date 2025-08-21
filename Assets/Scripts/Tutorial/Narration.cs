using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Narration : MonoBehaviour, IPointerClickHandler
{
    private RectTransform narrationBoxContent;

    [SerializeField] private GameObject textBoxPrefab;
    [SerializeField] private int maxVisible = 4;

    [TextArea]
    public List<string> demoLines = new List<string>{
        "오, 네가 우리를 관리해 줄 연구자구나?",
        "만나서 반가워! 우리는 인류의 먹거리를 책임지게 될 완두콩!",
        "1년 뒤에 돌아올 인간들을 위해 뛰어난 생존력을 가진 완두콩이 되는 게 우리의 목표야.",
        "아, 혹시 완두콩을 교배해 본 적이 있어?",
        "없구나?!",
    };

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

    public void OnPointerClick(PointerEventData eventData)
    {
        AddLine(demoLines[_nextIdx++]);
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
