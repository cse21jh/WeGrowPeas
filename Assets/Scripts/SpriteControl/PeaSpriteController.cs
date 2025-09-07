using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PeaSpriteController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Animator faceAnim;
    [SerializeField] private float faceStartMaxDelay = 0.1f; // 얼굴 애니메이션 시작 지연 시간

    [SerializeField] private Sprite[] peaSprites;
    [SerializeField] private SpriteRenderer accessoryRenderer;

    [SerializeField] private GameObject WindEffect;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Animator[] anims = GetComponentsInChildren<Animator>();
        for(int i = 0; i < anims.Length; i++)
        {
            if(anims[i].gameObject != this.gameObject)
            {
                faceAnim = anims[i];
                break;
            }
        }

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
            case (int)CompleteTraitType.NaturalDeath:
                spriteRenderer.sprite = peaSprites[1]; // 자연사 저항
                accessoryRenderer.sprite = null;
                break;
            case (int)CompleteTraitType.WindResistance:
                spriteRenderer.sprite = peaSprites[7]; // 바람 저항
                accessoryRenderer.sprite = null;
                break;
            case (int)CompleteTraitType.FloodResistance:
                spriteRenderer.sprite = peaSprites[0]; // 홍수 저항
                accessoryRenderer.sprite = peaSprites[9];
                break;
            case (int)CompleteTraitType.PestResistance:
                spriteRenderer.sprite = peaSprites[0]; // 해충 저항
                accessoryRenderer.sprite = peaSprites[4];
                break;
            case (int)CompleteTraitType.ColdResistance:
                spriteRenderer.sprite = peaSprites[0]; // 추위 저항
                accessoryRenderer.sprite = peaSprites[5];
                break;
            case (int)CompleteTraitType.HeavyRainResistance:
                spriteRenderer.sprite = peaSprites[0]; // 폭우 저항
                accessoryRenderer.sprite = peaSprites[6];
                break;
            case (int)CompleteTraitType.None:
                spriteRenderer.sprite = peaSprites[0]; // 기본
                accessoryRenderer.sprite = null;
                break;
            default:
                Debug.LogWarning("Invalid index for PeaSpriteController: " + index);
                break;
        }

        if(index == (int)CompleteTraitType.WindResistance)
        {
            WindEffect.SetActive(true);
        }
        else
        {
            WindEffect.SetActive(false);
        }

    }


}
