using UnityEngine;
using UnityEngine.EventSystems;

public class WhiteCircle : MonoBehaviour, IPointerClickHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        TutorialManager.Instance?.OnObjectClicked(gameObject);
    }
}
