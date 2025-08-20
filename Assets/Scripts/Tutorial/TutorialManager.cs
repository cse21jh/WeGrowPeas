using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{

    [SerializeField] private TutorialGrid grid;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SoundManager.Instance.StopBgm();
        SoundManager.Instance.PlayBgm("Farm");

        Time.timeScale = 1;

        //ClickRouter.Instance.IsBlockedByUI = false;

        StartCoroutine(RunTutorial());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator RunTutorial()
    {
        yield return TutorialStep0();
        yield return TutorialStep1();
        yield return TutorialStep2();
        yield return TutorialStep3();
        yield return TutorialStep4();
    }

    private IEnumerator TutorialStep0()
    {
        Debug.Log("튜토리얼 0 실행");
        grid.InitTGrid();
        yield return null;
    }

    private IEnumerator TutorialStep1()
    {
        yield return null;
    }

    private IEnumerator TutorialStep2()
    {
        yield return null;
    }

    private IEnumerator TutorialStep3()
    {
        yield return null;
    }

    private IEnumerator TutorialStep4()
    {
        yield return null;
    }
}
