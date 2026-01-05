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
    [SerializeField] private GameObject SweatEffect;

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
            case (int)TraitType.NaturalDeath:
                spriteRenderer.sprite = peaSprites[1]; // 자연사 저항
                accessoryRenderer.sprite = null;
                break;
            case (int)TraitType.Pest:
                spriteRenderer.sprite = peaSprites[0]; // 해충 저항
                accessoryRenderer.sprite = peaSprites[4];
                break;
            case (int)TraitType.Wind:
                spriteRenderer.sprite = peaSprites[7]; // 바람 저항
                accessoryRenderer.sprite = null;
                break;
            case (int)TraitType.Flood:
                spriteRenderer.sprite = peaSprites[0]; // 홍수 저항
                accessoryRenderer.sprite = peaSprites[9];
                break;
            case (int)TraitType.HeavyRain:
                spriteRenderer.sprite = peaSprites[0]; // 폭우 저항 유전자 2개
                accessoryRenderer.sprite = peaSprites[6];
                break;
            case (int)TraitType.Cold:
                spriteRenderer.sprite = peaSprites[0]; // 추위 저항 유전자 2개
                accessoryRenderer.sprite = peaSprites[5];
                break;
            case (int)TraitType.Drought:
                spriteRenderer.sprite = peaSprites[2]; // 가뭄 저항 유전자 2개 (폭우 0개) 
                accessoryRenderer.sprite = null; 
                break;
            case (int)TraitType.Heat:
                spriteRenderer.sprite = peaSprites[0]; // 더위 저항 유전자 2개 (추위 0개)
                accessoryRenderer.sprite = peaSprites[3];
                //땀 이펙트
                SweatEffect.SetActive(true);
                break;
            case (int)TraitType.None + 1:
                spriteRenderer.sprite = peaSprites[2]; // 폭우 가뭄 반반
                accessoryRenderer.sprite = peaSprites[6];
                break;
            case (int)TraitType.None + 2:
                spriteRenderer.sprite = peaSprites[0]; // 추위 더위 반반
                accessoryRenderer.sprite = peaSprites[5];
                //땀 이펙트
                SweatEffect.SetActive(true);
                break;
            case (int)TraitType.None:
                spriteRenderer.sprite = peaSprites[0]; // 기본
                accessoryRenderer.sprite = null;
                break;
            default:
                Debug.LogWarning("Invalid index for PeaSpriteController: " + index);
                break;
        }

        if(index == (int)TraitType.Wind)
        {
            WindEffect.SetActive(true);
        }
        else
        {
            WindEffect.SetActive(false);
        }


        if(index == (int)TraitType.Heat || index == (int)TraitType.None + 2)
        {
            SweatEffect.SetActive(true);
            Debug.Log("Sweat Effect Activated");
        }
        else
        {
            SweatEffect.SetActive(false);
        }
    }


}
