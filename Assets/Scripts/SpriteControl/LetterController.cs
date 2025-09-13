using System.Collections;
using System.Security.Cryptography;
using UnityEngine;

public class LetterController : MonoBehaviour
{
    /*
    public AnimationCurve curveX;
    public AnimationCurve curveY;
    public AnimationCurve curveZ;

    public float speed = 1f; // 커브가 재생되는 속도
    public float amplitude = 1f; // 움직임의 세기

    private Vector3 startPos;
    private float time;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void FixedUpdate()
    {
        time += Time.deltaTime * speed;

        float offsetX = curveX.Evaluate(time) * amplitude;
        float offsetY = curveY.Evaluate(time) * amplitude;
        float offsetZ = curveZ.Evaluate(time) * amplitude;

        transform.localPosition = startPos + new Vector3(offsetX, offsetY, offsetZ);
    }
    */


    private void OnMouseDown()
    {
        TransitionController.instance.Transition_Out();

        //여기에 엔딩씬으로 넘어가는 코드 작성
        StartCoroutine(EndScene(1.1f));

        Debug.Log("Letter Clicked!");
    }

    private IEnumerator EndScene(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneLoader.Instance.LoadGameOverScene();
    }

    public void StartEndLetter()
    {
        GetComponent<Animator>().SetTrigger("StartEndLetter");
    }
}
