using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PanelTranstionController : MonoBehaviour
{
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.InOutSine;

    [SerializeField] private RectTransform[] panels;

    [SerializeField] private bool[] isPanelActive;
    [SerializeField] private int lastActiveIndex = -1;
    [SerializeField] private int currentActiveIndex = -1;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private void Start()
    {
        isPanelActive = new bool[panels.Length];
        for(int i = 0 ; i < panels.Length; i++)
        {
            isPanelActive[i] = panels[i].gameObject.activeSelf;
        }
        lastActiveIndex = 0;
        currentActiveIndex = 0;
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
        panel.gameObject.SetActive(true);
        panel.DOKill();

        panel.localScale = new Vector3(0f, 0f, 1f);
        panel.gameObject.SetActive(true);
        panel.DOScale(new Vector3(1f, 1f, 1f), transitionDuration).SetEase(transitionEase);
    }

    private void TransitionOut(RectTransform panel)
    {
        if(panel.gameObject.activeSelf == false)
            return;
        panel.DOKill();

        panel.localScale = new Vector3(1f, 1f, 1f);
        panel.DOScale(new Vector3(0f, 0f, 0f), transitionDuration).SetEase(transitionEase).OnComplete(() =>
        {
            panel.gameObject.SetActive(false);
        });
    }



    public void TransitionToIndex(int index)
    {
        lastActiveIndex = currentActiveIndex;
        currentActiveIndex = index;

        PhoneManager.Instance.PhoneTouchEffect();
        if (index == 0)
        {
            PhoneManager.Instance.messengerApp.CheckCoroutineByTab(false);
            for (int i = 1; i < panels.Length; i++)
            {
                TransitionOut(panels[i]);
            }
            if(lastActiveIndex != 0)
                TransitionIn(panels[0]);
        }
        else
        {
            TransitionOut(panels[0]);
            TransitionIn(panels[index]);
            if (index == 4)
                PhoneManager.Instance.messengerApp.CheckCoroutineByTab(true);
        }
    }
}
