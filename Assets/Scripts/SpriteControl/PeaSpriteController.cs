using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PeaSpriteController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Animator faceAnim;
    [SerializeField] private float faceStartMaxDelay = 0.1f; // �� �ִϸ��̼� ���� ���� �ð�

    [SerializeField] private Sprite[] peaSprites;
    [SerializeField] private SpriteRenderer accessoryRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        faceAnim = GetComponentInChildren<Animator>();
        if (faceAnim != null)
        {
            StartCoroutine(FaceStart());
        }
    }

    private IEnumerator FaceStart()
    {
        float rand = Random.Range(0f, faceStartMaxDelay);
        yield return new WaitForSeconds(rand);
        faceAnim.SetTrigger("Start");
    }


    public void SetPeaSprite(int index)
    {
        if(spriteRenderer == null)
        {
            spriteRenderer = this.GetComponent<SpriteRenderer>();
        }



        switch (index)
        {
            case 0:
                spriteRenderer.sprite = peaSprites[1]; // �ڿ��� ����
                accessoryRenderer.sprite = null;
                break;
            case 1:
                spriteRenderer.sprite = peaSprites[0]; // �ٶ� ����
                accessoryRenderer.sprite = peaSprites[2];
                break;
            case 2:
                spriteRenderer.sprite = peaSprites[0]; // ȫ�� ����
                accessoryRenderer.sprite = peaSprites[3];
                break;
            case 3:
                spriteRenderer.sprite = peaSprites[0]; // ���� ����
                accessoryRenderer.sprite = peaSprites[4];
                break;
            case 4:
                spriteRenderer.sprite = peaSprites[0]; // ���� ����
                accessoryRenderer.sprite = peaSprites[5];
                break;
            case 5:
                spriteRenderer.sprite = peaSprites[0]; // ���� ����
                accessoryRenderer.sprite = peaSprites[6];
                break;
            case 6:
                spriteRenderer.sprite = peaSprites[0]; // �⺻
                accessoryRenderer.sprite = null;
                break;
            default:
                Debug.LogWarning("Invalid index for PeaSpriteController: " + index);
                break;
        }


    }


}
