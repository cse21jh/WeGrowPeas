using System.Collections;
using System.Security.Cryptography;
using UnityEngine;

public class LetterController : MonoBehaviour
{
    /*
    public AnimationCurve curveX;
    public AnimationCurve curveY;
    public AnimationCurve curveZ;

    public float speed = 1f; // Ŀ�갡 ����Ǵ� �ӵ�
    public float amplitude = 1f; // �������� ����

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
        // 화면 덮기/열기 연출은 SceneLoader가 담당한다.

        //���⿡ ���������� �Ѿ�� �ڵ� �ۼ�
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
