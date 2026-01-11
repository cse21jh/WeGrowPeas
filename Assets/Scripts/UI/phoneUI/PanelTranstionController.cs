using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PanelTranstionController : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.InOutSine;

    [SerializeField] private RectTransform[] panels;

    [SerializeField] private bool[] isPanelActive;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private void Start()
    {
        isPanelActive = new bool[panels.Length];
        for(int i = 0 ; i < panels.Length; i++)
        {
            isPanelActive[i] = panels[i].gameObject.activeSelf;
        }
    }

    private void Update()
    {
        for(int i = 0; i < panels.Length; i++)
        {
            if (Input.GetKeyDown(toggleKey + i))
            {
                TransitionToIndex(i);
            }
        }
    }

    private void TransitionIn(RectTransform panel)
    {
        panel.localScale = new Vector3(0f, 0f, 1f);
        panel.gameObject.SetActive(true);
        panel.DOScale(new Vector3(1f, 1f, 1f), transitionDuration).SetEase(transitionEase);
    }

    private void TransitionOut(RectTransform panel)
    {
        panel.localScale = new Vector3(1f, 1f, 1f);
        panel.DOScale(new Vector3(0f, 0f, 0f), transitionDuration).SetEase(transitionEase);
    }



    public void TransitionToIndex(int index)
    {
        for(int i = 0; i < panels.Length; i++)
        {
            if(i == index)
            {
                TransitionIn(panels[i]);
                //isPanelActive[i] = true;
            }
            else
            {
                TransitionOut(panels[i]);
                //isPanelActive[i] = false;
            }
        }
    }
}
